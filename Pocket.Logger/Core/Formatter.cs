using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Pocket
{
    internal class Formatter
    {
        private readonly string template;

        private static readonly Regex tokenRegex = new Regex(
            @"{(?<key>[^{}:]*)(?:\:(?<format>.+))?}",
            RegexOptions.IgnoreCase
            | RegexOptions.Multiline
            | RegexOptions.CultureInvariant
            | RegexOptions.Compiled
        );

        private readonly List<Action<StringBuilder, object>> argumentFormatters = new List<Action<StringBuilder, object>>();

        private readonly List<string> tokens = new List<string>();

        public Formatter(string template)
        {
            this.template = template;
            var matches = tokenRegex.Matches(template);
            var position = 0;

            foreach (Match match in matches)
            {
                var argName = match.Groups["key"].Captures[0].Value;
                tokens.Add(argName);

                var replacementTarget = match.Value;

                var formatStr = match.Groups["format"].Success ? match.Groups["format"].Captures[0].Value : null;

                void format(StringBuilder sb, object value)
                {
                    string formattedParam = null;

                    if (!string.IsNullOrEmpty(formatStr))
                    {
                        var formattableParamValue = value as IFormattable;
                        if (formattableParamValue != null)
                        {
                            formattedParam = formattableParamValue.ToString(formatStr, CultureInfo.CurrentCulture);
                        }
                    }

                    if (formattedParam == null)
                    {
                        formattedParam = Format(value);
                    }

                    sb.Replace(replacementTarget, formattedParam);
                }

                position++;

                argumentFormatters.Add(format);
            }
        }

        public IReadOnlyList<string> Tokens => tokens;

        public FormatterResult Format(IReadOnlyList<object> args)
        {
            var stringBuilder = new StringBuilder(template);
            var result = new FormatterResult(stringBuilder);

            for (var i = 0; i < Math.Min(argumentFormatters.Count, args.Count); i++)
            {
                var argument = args[i];
                argumentFormatters[i](stringBuilder, argument);
                result.Add(Tokens[i], argument);
            }

            if (args.Count > argumentFormatters.Count)
            {
                stringBuilder.Append(" +[ ");

                for (var i = argumentFormatters.Count; i < args.Count - 1; i++)
                {
                    var argument = args[i];
                    stringBuilder.Append(argument);
                    stringBuilder.Append(", ");
                    result.Add($"arg{i}", argument);
                }

                var lastArgument = args[args.Count - 1];
                stringBuilder.Append(lastArgument);
                stringBuilder.Append(" ]");
                result.Add($"arg{args.Count - 1}", lastArgument);
            }

            return result;
        }

        public FormatterResult Format(params object[] args)
        {
            return Format((IReadOnlyList<object>) args);
        }

        private static string Format(object objectToFormat)
        {
            if (objectToFormat == null)
            {
                return "";
            }

            var enumerable = objectToFormat as IEnumerable;

            if (enumerable != null &&
                !(objectToFormat is string))
            {
                return $"[ {string.Join(", ", enumerable.Cast<object>())} ]";
            }

            return objectToFormat.ToString();
        }

        internal class FormatterResult : IReadOnlyList<KeyValuePair<string, object>>
        {
            private readonly StringBuilder formattedMessage;

            public FormatterResult(StringBuilder formattedMessage)
            {
                this.formattedMessage = formattedMessage;
            }

            private readonly List<KeyValuePair<string, object>> properties = new List<KeyValuePair<string, object>>();

            public void Add(string key, object value) =>
                properties.Add(new KeyValuePair<string, object>(key, value));

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => properties.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public int Count => properties.Count;

            public KeyValuePair<string, object> this[int index] => properties[index];

            public override string ToString() => formattedMessage.ToString();
        }

        private static readonly ConcurrentDictionary<string, Formatter> formatters = new ConcurrentDictionary<string, Formatter>();

        public static Formatter Parse(string template) =>
            formatters.GetOrAdd(template, t => new Formatter(t));
    }
}