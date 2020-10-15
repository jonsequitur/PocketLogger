using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#nullable disable

namespace Pocket
{
#if !SourceProject
    [System.Diagnostics.DebuggerStepThrough]
#endif
    internal static partial class LogEvents
    {
        private static readonly Lazy<Type[]> loggerTypes = new Lazy<Type[]>(
            () => Discover.ConcreteTypes()
                          .PocketLoggers()
                          .ToArray());

        public static IDisposable Enrich(
            Action<Action<(string Name, object Value)>> onEnrich,
            IReadOnlyCollection<Assembly> onlySearchAssemblies = null)
        {
            var subscription = new LoggerSubscription();

            foreach (var loggerType in onlySearchAssemblies?.Types().PocketLoggers()
                                       ??
                                       loggerTypes.Value)
            {
                var enrich = loggerType.GetMember(nameof(Logger.Enrich)).OfType<EventInfo>().Single();

                var enrichSubscriber = onEnrich.Catch();

                enrich.AddEventHandler(
                    null,
                    enrichSubscriber);

                subscription.Add(loggerType, Disposable.Create(() =>
                {
                    enrich.RemoveEventHandler(
                        null,
                        enrichSubscriber);
                }));
            }

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
            IReadOnlyCollection<Assembly> onlySearchAssemblies = null)
        {
            if (onEntryPosted == null)
            {
                throw new ArgumentNullException(nameof(onEntryPosted));
            }
         
            var subscription = new LoggerSubscription();

            SubscribeLoggers(onlySearchAssemblies?.Types().PocketLoggers() 
                             ??
                             loggerTypes.Value,
                             subscription,
                             onEntryPosted);

            return subscription;
        }

        private static void SubscribeLoggers<T>(
            IEnumerable<Type> pocketLoggerTypes,
            LoggerSubscription subscription,
            Action<T> onEntryPosted = null)
        {
            foreach (var loggerType in pocketLoggerTypes.Distinct())
            {
                if (onEntryPosted != null)
                {
                    var posted = (EventInfo) loggerType.GetMember(nameof(Logger.Posted))[0];

                    var subscriber = onEntryPosted.Catch();

                    posted.AddEventHandler(
                        null,
                        subscriber);

                    subscription.Add(loggerType,
                                     Disposable.Create(() => posted.RemoveEventHandler(
                                                           null,
                                                           subscriber)));
                }
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