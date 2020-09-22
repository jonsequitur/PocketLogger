using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Pocket
{
#if !SourceProject
    [System.Diagnostics.DebuggerStepThrough]
#endif
    internal class Formatter
    {
        private static bool stopCaching = false;

        private static int cacheCount = 0;

        private static readonly Regex tokenRegex = new Regex(
            @"{(?<key>[^{}:]*)(?:\:(?<format>.+))?}",
            RegexOptions.IgnoreCase |
            RegexOptions.Multiline |
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled
        );

        private readonly string template;

        private readonly List<Action<StringBuilder, object>> argumentFormatters = new List<Action<StringBuilder, object>>();

        private readonly List<string> tokens = new List<string>();

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
                    string formattedParam = null;

                    if (!string.IsNullOrEmpty(formatStr))
                    {
                        if (value is IFormattable formattableParamValue)
                        {
                            formattedParam = formattableParamValue.ToString(formatStr, CultureInfo.CurrentCulture);
                        }
                    }

                    if (formattedParam == null)
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
            IReadOnlyList<object> args,
            IList<(string Name, object Value)> knownProperties)
        {
            if (args == null)
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
                        stringBuilder.Append(property.ToLogString());
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

        private static readonly ConcurrentDictionary<string, Formatter> cache = new ConcurrentDictionary<string, Formatter>();

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

            private readonly List<(string Name, object Value)> properties = new List<(string, object )>();

            public void Add(string key, object value) => properties.Add((key, value));

            public IEnumerator<(string Name, object Value)> GetEnumerator() => properties.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public int Count => properties.Count;

            public (string Name, object Value) this[int index] => properties[index];

            public override string ToString() => formattedMessage?.ToString() ?? "";
        }
    }
}
