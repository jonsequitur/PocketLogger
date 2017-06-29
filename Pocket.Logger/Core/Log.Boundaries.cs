using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Pocket
{
    internal static partial class Log
    {
        public static LogSection OnEnterAndExit(
            bool requireConfirm = false,
            [CallerMemberName] string name = null,
            string id = null)
        {
            var section = new LogSection(
                requireConfirm,
                name,
                id: id);

            section.Log(section[0]);

            return section;
        }

        public static LogSection OnExit(
            bool requireConfirm = false,
            [CallerMemberName] string name = null,
            string id = null) =>
            // ReSharper disable once ExplicitCallerInfoArgument
            new LogSection(
                requireConfirm,
                name,
                id: id);

        public static LogSection Confirm(
            [CallerMemberName] string name = null,
            string id = null) =>
            // ReSharper disable once ExplicitCallerInfoArgument
            new LogSection(
                true,
                name,
                id: id);

        public static void Event(
            [CallerMemberName] string name = null,
            params (string name, double value)[] metrics)
        {
            var args = new List<object>
            {
                name
            };

            if (metrics != null)
            {
                foreach (var metric in metrics)
                {
                    args.Add(metric);
                }
            }

            Logger.Default.Log(
                new LogEntry(LogLevel.Trace,
                             message: "{name}",
                             callingMethod: name,
                             category: nameof(Event),
                             isTelemetry: true,
                             args: args.ToArray()
                ));
        }
    }
}
