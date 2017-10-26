using System;
using System.Diagnostics;
using System.Reflection;
using System.Linq;

namespace Pocket
{
    [DebuggerStepThrough]
    internal static partial class LogEvents
    {
        private static readonly Lazy<Type[]> loggerTypes = new Lazy<Type[]>(
            () =>
                Discover.ConcreteTypes()
                        .Where(t => t.AssemblyQualifiedName != typeof(Logger).AssemblyQualifiedName)
                        .Where(t => t.FullName == typeof(Logger).FullName)
                        .ToArray());

        public static IDisposable Enrich(Action<Action<(string Name, object Value)>> enrich)
        {
            enrich = enrich.Catch();
            Logger.Enrich += enrich;

            return Disposable.Create(() =>
            {
                Logger.Enrich -= enrich;
            });
        }

        public static IDisposable Subscribe(
            Action<(
                    byte LogLevel,
                    DateTime TimestampUtc,
                    Func<(string Message, (string Name, object Value)[] Properties)> Evaluate,
                    Exception Exception,
                    string OperationName,
                    string Category,
                    (string Id,
                    bool IsStart,
                    bool IsEnd,
                    bool? IsSuccessful,
                    TimeSpan? Duration) Operation)>
                onEntryPosted,
            bool discoverOtherPocketLoggers = true)
        {
            if (onEntryPosted == null)
            {
                throw new ArgumentNullException(nameof(onEntryPosted));
            }

            var disposables = new CompositeDisposable();

            var postSafeltFromLocalLogger = onEntryPosted.Catch();
            Logger.Posted += postSafeltFromLocalLogger;

            disposables.Add(Disposable.Create(() =>
            {
                Logger.Posted -= postSafeltFromLocalLogger;
            }));

            if (discoverOtherPocketLoggers)
            {
                foreach (var loggerType in loggerTypes.Value)
                {
                    var entryPostedEventHandler = (EventInfo) loggerType.GetMember(nameof(Logger.Posted)).Single();

                    var postSafelyFromDiscoveredLogger = onEntryPosted.Catch();

                    entryPostedEventHandler.AddEventHandler(
                        null,
                        postSafelyFromDiscoveredLogger);

                    disposables.Add(Disposable.Create(() =>
                    {
                        entryPostedEventHandler.RemoveEventHandler(
                            null,
                            postSafelyFromDiscoveredLogger);
                    }));
                }
            }

            return disposables;
        }

        internal static Action<T> Catch<T>(this Action<T> action)
        {
            void invoke(T e)
            {
                try
                {
                    action(e);
                }
                catch (Exception)
                {
                }
            }

            return invoke;
        }
    }
}
