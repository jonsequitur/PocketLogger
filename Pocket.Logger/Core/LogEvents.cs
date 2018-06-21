using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Pocket
{
    [DebuggerStepThrough]
    internal static partial class LogEvents
    {
        private static readonly Lazy<Type[]> loggerTypes = new Lazy<Type[]>(
            () => Discover.ConcreteTypes()
                          .PocketLoggers()
                          .Where(t => t.AssemblyQualifiedName != typeof(Logger).AssemblyQualifiedName)
                          .ToArray());

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
            IReadOnlyCollection<Assembly> assembliesToSubscribe,
            Action<Action<(string Name, object Value)>> enrich = null)
        {
            if (onEntryPosted == null)
            {
                throw new ArgumentNullException(nameof(onEntryPosted));
            }

            if (assembliesToSubscribe == null)
            {
                throw new ArgumentNullException(nameof(assembliesToSubscribe));
            }

            var subscription = new LoggerSubscription();

            SubscribeLoggers(
                onEntryPosted,
                assembliesToSubscribe
                    .Types()
                    .PocketLoggers(),
                subscription,
                enrich);

            return subscription;
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
            bool discoverOtherPocketLoggers = true,
            Action<Action<(string Name, object Value)>> enrich = null)
        {
            if (onEntryPosted == null)
            {
                throw new ArgumentNullException(nameof(onEntryPosted));
            }

            var subscription = new LoggerSubscription();

            SubscribeContainingAssembly(
                onEntryPosted,
                subscription,
                enrich);

            if (discoverOtherPocketLoggers)
            {
                SubscribeLoggers(
                    onEntryPosted,
                    loggerTypes.Value,
                    subscription,
                    enrich);
            }

            return subscription;
        }

        private static void SubscribeContainingAssembly<T>(
            Action<T> onEntryPosted,
            LoggerSubscription subscription,
            Action<Action<(string Name, object Value)>> enrich = null) =>
            SubscribeLoggers(
                onEntryPosted,
                new[] { typeof(Logger) },
                subscription, enrich);

        private static void SubscribeLoggers<T>(
            Action<T> onEntryPosted,
            IEnumerable<Type> pocketLoggerTypes,
            LoggerSubscription subscription,
            Action<Action<(string Name, object Value)>> enrich = null)
        {
            foreach (var loggerType in pocketLoggerTypes.Distinct())
            {
                var onDispose = new CompositeDisposable();

                var posted = (EventInfo) loggerType.GetMember(nameof(Logger.Posted)).Single();

                var subscriber = onEntryPosted.Catch();

                posted.AddEventHandler(
                    null,
                    subscriber);

                onDispose.Add(() => posted.RemoveEventHandler(
                                  null,
                                  subscriber));

                if (enrich != null)
                {
                    var enrichEvent = loggerType.GetMember(nameof(Logger.Enrich)).OfType<EventInfo>().Single();

                    var enrichSubscriber = enrich.Catch();

                    enrichEvent.AddEventHandler(
                        null,
                        enrichSubscriber);

                    onDispose.Add(() => enrichEvent.RemoveEventHandler(
                                      null,
                                      enrichSubscriber));
                }

                subscription.Add(loggerType, onDispose);
            }
        }

        private static Action<T> Catch<T>(
            this Action<T> publish)
        {
            return Invoke;

            void Invoke(T e)
            {
                try
                {
                    publish(e);
                }
                catch (Exception)
                {
                }
            }
        }

        private static IEnumerable<Type> PocketLoggers(this IEnumerable<Type> types) =>
            types.Where(t => t.FullName == typeof(Logger).FullName);
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
