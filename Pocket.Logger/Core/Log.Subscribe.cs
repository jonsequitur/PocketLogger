using System;

namespace Pocket
{
    internal static partial class Log
    {
        public static IDisposable Subscribe(
            Action<LogEntry> onEntryPosted)
        {
            if (onEntryPosted == null)
            {
                throw new ArgumentNullException(nameof(onEntryPosted));
            }

            void LoggerOnEntryPosted(LogEntry entry)
            {
                try
                {
                    onEntryPosted(entry);
                }
                catch (Exception exception)
                {
                    // TODO: (Subscribe) publish on error channel
                }
            }

            Logger.EntryPosted += LoggerOnEntryPosted;

            return Disposable.Create(() => { Logger.EntryPosted -= LoggerOnEntryPosted; });
        }
    }
}
