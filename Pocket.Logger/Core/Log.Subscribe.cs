using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

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

            var handleSafely = HandleSafely(onEntryPosted);

            Logger.EntryPosted += handleSafely;

            return Disposable.Create(() =>
            {
                Logger.EntryPosted -= handleSafely;
            });
        }

        public static IDisposable DiscoverAndSubscribe(
            Action<IReadOnlyCollection<KeyValuePair<string, object>>> onEntryPosted)
        {
            if (onEntryPosted == null)
            {
                throw new ArgumentNullException(nameof(onEntryPosted));
            }

            var thisAssembly = typeof(Log).GetTypeInfo().Assembly;

            var loggerTypes = Discover.ConcreteTypes()
                                      .Where(t => !t.GetTypeInfo()
                                                    .Assembly
                                                    .Equals(thisAssembly))
                                      .Where(t => t.FullName == typeof(Logger).FullName);

            var disposables = new CompositeDisposable();

            foreach (var loggerType in loggerTypes)
            {
                var entryPosted = (EventInfo) loggerType.GetMember(nameof(Logger.EntryPosted)).Single();

                var handleSafely = HandleSafely(onEntryPosted);

                entryPosted.AddEventHandler(null, handleSafely);

                disposables.Add(Disposable.Create(() =>
                {
                    entryPosted.RemoveEventHandler(null, handleSafely);
                }));
            }

            return disposables;
        }

        private static Action<T> HandleSafely<T>(Action<T> onEntryPosted)
        {
            void LoggerOnEntryPosted(T entry)
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

            return LoggerOnEntryPosted;
        }
    }
}
