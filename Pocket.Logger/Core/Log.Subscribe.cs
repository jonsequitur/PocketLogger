using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Pocket
{
    internal static partial class LogEvents
    {
        public static IDisposable Subscribe(
            Action<(
                    int LogLevel,
                    DateTimeOffset Timestamp,
                    Func<(string Message, IReadOnlyCollection<KeyValuePair<string, object>> Properties)> Evaluate,
                    Exception Exception,
                    string OperationName,
                    string Category,
                    (string Id,
                    bool IsStart,
                    bool IsEnd,
                    bool? IsSuccessful,
                    TimeSpan? Duration) Operation)>
                onEntryPosted,
            bool discoverOtherPocketLoggers = false)
        {
            if (onEntryPosted == null)
            {
                throw new ArgumentNullException(nameof(onEntryPosted));
            }

            var disposables = new CompositeDisposable();

            var handleSafely = HandleSafely(onEntryPosted);

            Logger.Posted += handleSafely;

            disposables.Add(Disposable.Create(() =>
            {
                Logger.Posted -= handleSafely;
            }));

            if (discoverOtherPocketLoggers)
            {
                var thisAssembly = typeof(Logger).GetTypeInfo().Assembly;

                var loggerTypes = Discover.ConcreteTypes()
                                          .Where(t => !t.GetTypeInfo()
                                                        .Assembly
                                                        .Equals(thisAssembly))
                                          .Where(t => t.FullName == typeof(Logger).FullName);

                foreach (var loggerType in loggerTypes)
                {
                    var entryPosted = (EventInfo) loggerType.GetMember(nameof(Logger.Posted)).Single();

                    handleSafely = HandleSafely(onEntryPosted);

                    entryPosted.AddEventHandler(null, handleSafely);

                    disposables.Add(Disposable.Create(() =>
                    {
                        entryPosted.RemoveEventHandler(null, handleSafely);
                    }));
                }
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
