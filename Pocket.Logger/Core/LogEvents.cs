#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using LogEvent = (
    string MessageTemplate,
    object[]? Args, System.Collections.Generic.List<(string Name, object Value)> Properties,
    byte LogLevel,
    System.DateTime TimestampUtc,
    System.Exception? Exception,
    string? OperationName,
    string? Category,
    (string? Id,
    bool IsStart,
    bool IsEnd,
    bool? IsSuccessful,
    System.TimeSpan? Duration) Operation);

namespace Pocket;
#if !SourceProject
[DebuggerStepThrough]
#endif
internal static partial class LogEvents
{
    public static IDisposable Enrich(
        Action<Action<(string Name, object Value)>> onEnrich,
        params Assembly[] searchInAssemblies)
    {
        var subscription = new LoggerSubscription();

        if (searchInAssemblies.Length == 0)
        {
            searchInAssemblies = [typeof(LogEvents).Assembly];
        }

        foreach (var loggerType in searchInAssemblies.Types().PocketLoggers())
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
        Action<LogEvent> onEntryPosted,
        params Assembly[] searchInAssemblies)
    {
        if (onEntryPosted is null)
        {
            throw new ArgumentNullException(nameof(onEntryPosted));
        }

        if (searchInAssemblies.Length == 0)
        {
            searchInAssemblies = [typeof(LogEvents).Assembly];
        }

        var subscription = new LoggerSubscription();

        SubscribeLoggers(searchInAssemblies.Types().PocketLoggers(),
                         subscription,
                         onEntryPosted);

        return subscription;
    }

    private static void SubscribeLoggers(
        IEnumerable<Type> pocketLoggerTypes,
        LoggerSubscription subscription,
        Action<LogEvent> onEntryPosted)
    {
        foreach (var loggerType in pocketLoggerTypes.Distinct())
        {
            var posted = (EventInfo)loggerType.GetMember(nameof(Logger.Posted))[0];

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

    private static Action<T> Catch<T>(this Action<T> publish)
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
    private readonly CompositeDisposable disposables = [];

    public void Add(Type pocketLoggerType, IDisposable unsubscribe)
    {
        DiscoveredLoggerTypes.Add(pocketLoggerType);
        disposables.Add(unsubscribe);
    }

    public List<Type> DiscoveredLoggerTypes { get; } = new();

    public void Dispose() => disposables.Dispose();
}