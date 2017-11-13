using System;
using System.Collections.Generic;
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

        public static LoggerSubscription Subscribe(
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

            var subscription = new LoggerSubscription();

            var postSafelyFromLocalLogger = onEntryPosted.Catch();
            Logger.Posted += postSafelyFromLocalLogger;

            subscription.Add(typeof(Logger), Disposable.Create(() =>
            {
                Logger.Posted -= postSafelyFromLocalLogger;
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

                    subscription.Add(loggerType, Disposable.Create(() =>
                    {
                        entryPostedEventHandler.RemoveEventHandler(
                            null,
                            postSafelyFromDiscoveredLogger);
                    }));

                }
            }

            return subscription;
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

    internal class LoggerSubscription : IDisposable
    {
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public void Add(Type pocketLoggerType, IDisposable unsubscribe)
        {
            DiscoveredLoggerTypes.Add(pocketLoggerType);
            disposables.Add(unsubscribe);
        }

        public List<Type> DiscoveredLoggerTypes { get; } = new List<Type>();

        public void Dispose() => disposables.Dispose();
    }
}
