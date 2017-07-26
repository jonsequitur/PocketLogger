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
            bool discoverOtherPocketLoggers = false)
        {
            if (onEntryPosted == null)
            {
                throw new ArgumentNullException(nameof(onEntryPosted));
            }

            var disposables = new CompositeDisposable();

            var postSafely = Safely(onEntryPosted);
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

                    postSafely = Safely(onEntryPosted);

                    entryPosted.AddEventHandler(null, postSafely);

                    disposables.Add(Disposable.Create(() =>
                    {
                        entryPosted.RemoveEventHandler(null, postSafely);
                    }));
                }
            }

            return disposables;
        }

        private static Action<T> Safely<T>(Action<T> action) =>
            e =>
            {
                try
                {
                    action(e);
                }
                catch (Exception exception)
                {
                    // TODO: (Subscribe) publish on error channel
                }
            };
    }
}
