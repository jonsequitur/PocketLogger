using System;
using System.Reflection;
using System.Linq;

namespace Pocket
{
    internal static partial class LogEvents
    {
        private static readonly Lazy<Type[]> loggerTypes = new Lazy<Type[]>(
            () =>
            {
                var thisAssembly = typeof(Logger).GetTypeInfo().Assembly;

                return Discover.ConcreteTypes()
                               .Where(t => !t.GetTypeInfo()
                                             .Assembly
                                             .Equals(thisAssembly))
                               .Where(t => t.FullName == typeof(Logger).FullName)
                               .ToArray();
            });

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
                    int LogLevel,
                    DateTimeOffset Timestamp,
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

            var postSafely = onEntryPosted.Catch();
            Logger.Posted += postSafely;

            disposables.Add(Disposable.Create(() =>
            {
                Logger.Posted -= postSafely;
            }));

            if (discoverOtherPocketLoggers)
            {
                foreach (var loggerType in loggerTypes.Value)
                {
                    var entryPosted = (EventInfo) loggerType.GetMember(nameof(Logger.Posted)).Single();

                    postSafely = onEntryPosted.Catch();

                    entryPosted.AddEventHandler(null, postSafely);

                    disposables.Add(Disposable.Create(() =>
                    {
                        entryPosted.RemoveEventHandler(null, postSafely);
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
                catch (Exception exception)
                {
                    // TODO: (Subscribe) publish on error channel
                }
            }

            return invoke;
        }
    }
}
