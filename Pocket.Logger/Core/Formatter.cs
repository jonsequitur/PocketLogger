using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

#nullable enable

namespace Pocket;
#if !SourceProject
[System.Diagnostics.DebuggerStepThrough]
#endif
internal class Formatter
{
    private static readonly ConcurrentDictionary<string, Formatter> cache;
    private static bool stopCaching;
    private static int cacheCount;

    private static readonly Regex tokenRegex;

    static Formatter()
    {
        cache = new ConcurrentDictionary<string, Formatter>();
        tokenRegex = new Regex(
            @"{(?<key>[^{}:]*)(?:\:(?<format>.+))?}",
            RegexOptions.IgnoreCase |
            RegexOptions.Multiline |
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled
        );
    }

    private readonly string template;

    private readonly List<Action<StringBuilder, object>> argumentFormatters = new();

    private readonly List<string> tokens = new();

    public Formatter(string template)
    {
        this.template = template;
        var matches = tokenRegex.Matches(template);

        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var argName = match.Groups["key"].Captures[0].Value;
            tokens.Add(argName);

            var replacementTarget = match.Value;

            var formatStr = match.Groups["format"].Success
                                ? match.Groups["format"].Captures[0].Value
                                : null;

            void Format(StringBuilder sb, object value)
            {
                string? formattedParam = null;

                if (!string.IsNullOrEmpty(formatStr))
                {
                    if (value is IFormattable formattableParamValue)
                    {
                        formattedParam = formattableParamValue.ToString(formatStr, CultureInfo.CurrentCulture);
                    }
                }

                if (formattedParam is null)
                {
                    formattedParam = value.ToLogString();
                }

                sb.Replace(replacementTarget, formattedParam);
            }

            argumentFormatters.Add(Format);
        }
    }

    public IReadOnlyList<string> Tokens => tokens;

    public FormatterResult Format(
        IReadOnlyList<object>? args,
        IList<(string Name, object Value)>? knownProperties)
    {
        if (args is null)
        {
            args = new object[argumentFormatters.Count];
        }

        var stringBuilder = new StringBuilder(template);
        var result = new FormatterResult(stringBuilder);

        for (var i = 0; i < Math.Min(argumentFormatters.Count, args.Count); i++)
        {
            var argument = args[i];
            argumentFormatters[i](stringBuilder, argument);
            result.Add(Tokens[i], argument);
        }

        if (args.Count > argumentFormatters.Count || knownProperties?.Count > 0)
        {
            stringBuilder.Append(" +[ ");
            var first = true;

            if (args.Count > 0)
            {
                for (var i = argumentFormatters.Count; i < args.Count; i++)
                {
                    var argument = args[i];
                    TryAppendComma();

                    stringBuilder.Append(argument.ToLogString());
                    result.Add($"arg{i}", argument);
                }
            }

            if (knownProperties?.Count > 0)
            {
                for (var i = 0; i < knownProperties.Count; i++)
                {
                    var property = knownProperties[i];
                    TryAppendComma();
                    stringBuilder.Append($"({property.Name}, {property.Value.ToLogString()})");
                }
            }

            stringBuilder.Append(" ]");

            void TryAppendComma()
            {
                if (!first)
                {
                    stringBuilder.Append(", ");
                }
                first = false;
            }
        }

        return result;
    }

    public FormatterResult Format(params object[] args) => Format(args, null);

    public static int CacheCount => cacheCount;

    public static int CacheLimit { get; set; } = 300;

    public static Formatter Parse(string template)
    {
        return stopCaching
                   ? new Formatter(template)
                   : cache.GetOrAdd(template, CreateFormatter);

        Formatter CreateFormatter(string t)
        {
            if (Interlocked.Increment(ref cacheCount) >= CacheLimit)
            {
                stopCaching = true;
            }

            return new Formatter(t);
        }
    }

    internal class FormatterResult : IReadOnlyList<(string Name, object Value)>
    {
        private readonly StringBuilder formattedMessage;

        public FormatterResult(StringBuilder formattedMessage)
        {
            this.formattedMessage = formattedMessage;
        }

        private readonly List<(string Name, object Value)> properties = new();

        public void Add(string key, object value) => properties.Add((key, value));

        public IEnumerator<(string Name, object Value)> GetEnumerator() => properties.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => properties.Count;

        public (string Name, object Value) this[int index] => properties[index];

        public override string ToString() => formattedMessage?.ToString() ?? "";
    }
}

internal static partial class Format
{
    public static (string Message, (string Name, object Value)[] Properties) Evaluate(
        this in (
            string MessageTemplate,
            object[] Args,
            List<(string Name, object Value)> Properties,
            byte LogLevel,
            DateTime TimestampUtc,
            Exception Exception,
            string OperationName,
            string Category,
            (string Id,
            bool IsStart,
            bool IsEnd,
            bool? IsSuccessful,
            TimeSpan? Duration) Operation) e)
    {
        (string message, (string Name, object Value)[] Properties)? evaluated = null;

        var message = e.MessageTemplate;
        var properties = new List<(string Name, object Value)>();

        if (e.Args?.Length != 0 || e.Properties.Count > 0)
        {
            var formatter = Formatter.Parse(e.MessageTemplate);

            var formatterResult = formatter.Format(args: e.Args, knownProperties: e.Properties);

            message = formatterResult.ToString();

            properties.AddRange(formatterResult);
        }

        evaluated = (message, properties.Concat(e.Properties).ToArray());

        return evaluated.Value;
    }

    public static string ToLogString(
       this in (
           string MessageTemplate,
           object[] Args,
           List<(string Name, object Value)> Properties,
           byte LogLevel,
           DateTime TimestampUtc,
           Exception Exception,
           string OperationName,
           string Category,
           (string Id,
           bool IsStart,
           bool IsEnd,
           bool? IsSuccessful,
           TimeSpan? Duration) Operation) e)
    {
        var (message, _) = e.Evaluate();

        var logLevelString =
            LogLevelString((LogLevel)e.LogLevel,
                           e.Operation.IsStart,
                           e.Operation.IsEnd,
                           e.Operation.IsSuccessful,
                           e.Operation.Duration);

        return
            $"{e.TimestampUtc:o} {e.Operation.Id.IfNotEmpty()}{e.Category.IfNotEmpty()}{e.OperationName.IfNotEmpty()} {logLevelString} {message} {e.Exception}";
    }

    static partial void CustomizeLogString(object? value, ref string? output);

    internal static string ToLogString(this object? value)
    {
        string? output = null;

        CustomizeLogString(value, ref output);

        if (output is not null)
        {
            return output;
        }

        if (value is ICollection enumerable)
        {
            return $"[ {string.Join(", ", enumerable.Cast<object>())} ]";
        }

        return value switch
        {
            null => "[null]",
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string IfNotEmpty(
        this string value,
        string prefix = "[",
        string suffix = "] ") =>
        string.IsNullOrEmpty(value)
            ? ""
            : $"{prefix}{value}{suffix}";

    private static string LogLevelString(
        LogLevel logLevel,
        bool isStartOfOperation,
        bool isEndOfOperation,
        bool? isOperationSuccessful,
        TimeSpan? duration)
    {
        string symbol()
        {
            if (isStartOfOperation)
            {
                return "▶";
            }

            if (isEndOfOperation)
            {
                if (isOperationSuccessful == true)
                {
                    return "⏹ -> ✔";
                }

                if (isOperationSuccessful == false)
                {
                    return "⏹ -> ❌";
                }

                return "⏹";
            }

            switch (logLevel)
            {
                case LogLevel.Telemetry:
                    return "📊";
                case LogLevel.Trace:
                case LogLevel.Debug:
                    return "⏱";
                case LogLevel.Information:
                    return "ℹ";
                case LogLevel.Warning:
                    return "⚠";
                case LogLevel.Error:
                    return "❌";
                case LogLevel.Critical:
                    return "💩";
                default:
                    return "ℹ";
            }
        }

        return duration is null ||
               isStartOfOperation
                   ? symbol()
                   : $"{symbol()} ({duration?.TotalMilliseconds}ms)";
    }
}
