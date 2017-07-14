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

        public static IDisposable DiscoverAndSubscribe(
            Action<IReadOnlyCollection<KeyValuePair<string, object>>> onEntryPosted)
        {
            var thisAssembly = typeof(Log).GetTypeInfo().Assembly;

            var logTypeName = typeof(Logger).FullName;

            var loggerTypes = Discover.ConcreteTypes()
                                      .Where(t => !t.GetTypeInfo()
                                                    .Assembly
                                                    .Equals(thisAssembly))
                                      .Where(t => t.FullName == logTypeName);

            var disposables = new CompositeDisposable();

            foreach (var loggerType in loggerTypes)
            {
                var entryPosted = (EventInfo) loggerType.GetMember(nameof(Logger.EntryPosted)).Single();

                entryPosted.AddEventHandler(null, onEntryPosted);

                disposables.Add(Disposable.Create(() => { entryPosted.RemoveEventHandler(null, onEntryPosted); }));
            }

            return disposables;
        }
    }
}
