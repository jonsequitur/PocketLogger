#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
internal class Logger
{
    public Logger(string? category = "")
    {
        Category = category ?? "";
    }

    public static event Action<Action<(string Name, object Value)>>? Enrich;

    public static event Action<LogEvent>? Posted;

    public virtual void Post(LogEntry entry)
    {
        Enrich?.Invoke(entry.AddProperty);

        var tuple = (
                        entry.MessageTemplate,
                        entry.Args,
                        entry.Properties,
                        (byte)entry.LogLevel,
                        entry.TimestampUtc,
                        entry.Exception,
                        entry.OperationName,
                        entry.Category ?? Category,
                        (
                            entry.OperationId,
                            entry.IsStartOfOperation,
                            entry.IsEndOfOperation,
                            entry.IsOperationSuccessful,
                            entry.OperationDuration
                        )
                    );

        Posted?.Invoke(tuple);
    }

    protected internal void Post(
        string? message,
        LogLevel logLevel,
        string? operationName = null,
        Exception? exception = null,
        object[]? args = null,
        in (string Name, object Value)[]? properties = null)
    {
        var logEntry = new LogEntry(
            messageTemplate: message,
            logLevel: logLevel,
            operationName: operationName,
            exception: exception,
            category: Category,
            operation: this as OperationLogger,
            args: args);

        if (properties?.Length > 0)
        {
            for (var i = 0; i < properties.Length; i++)
            {
                logEntry.AddProperty(properties[i]);
            }
        }

        Post(logEntry);
    }

    public string Category { get; }

    public static Logger Log { get; } = new();
}

#if !SourceProject
[System.Diagnostics.DebuggerStepThrough]
#endif
internal class Logger<TCategory> : Logger
{
    public Logger() : base(typeof(TCategory).FullName)
    {
    }

    public new static Logger Log { get; } = new Logger<TCategory>();
}

internal enum LogLevel : byte
{
    Telemetry,
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}

#if !SourceProject
[System.Diagnostics.DebuggerStepThrough]
#endif
internal static class LoggerExtensions
{
    public static TLogger Trace<TLogger>(
        this TLogger logger,
        string? message = null,
        params object[]? args)
        where TLogger : Logger
    {
        logger.Post(
            message ?? "",
            LogLevel.Trace,
            exception: null,
            args: args);

        return logger;
    }

    public static TLogger Trace<TLogger>(
            this TLogger logger,
            params object[] args)
            where TLogger : Logger =>
        logger.Trace(message: null, args);

    public static TLogger Info<TLogger>(
        this TLogger logger,
        string message,
        params object[] args)
        where TLogger : Logger
    {
        logger.Post(
            message,
            LogLevel.Information,
            exception: null,
            args: args);

        return logger;
    }

    public static TLogger Info<TLogger>(
            this TLogger logger,
            params object[] args)
            where TLogger : Logger =>
        logger.Info(message: "", args);

    public static TLogger Warning<TLogger>(
        this TLogger logger,
        string? message = null,
        Exception? exception = null,
        params object[]? args)
        where TLogger : Logger
    {
        logger.Post(
            message,
            LogLevel.Warning,
            exception: exception,
            args: args);

        return logger;
    }

    public static TLogger Warning<TLogger>(
        this TLogger logger,
        Exception exception)
        where TLogger : Logger =>
            logger.Warning(message: "", exception);

    public static TLogger Warning<TLogger>(
            this TLogger logger,
            params object[]? args)
            where TLogger : Logger =>
        logger.Warning(message: null, exception: null, args);

    public static TLogger Error<TLogger>(
        this TLogger logger,
        string? message = null,
        Exception? exception = null,
        params object[]? args)
        where TLogger : Logger
    {
        logger.Post(
            message,
            LogLevel.Error,
            exception: exception,
            args: args);

        return logger;
    }

    public static TLogger Error<TLogger>(
        this TLogger logger,
        Exception exception)
        where TLogger : Logger =>
            logger.Error(message: null, exception);

    public static TLogger Error<TLogger>(
            this TLogger logger,
            params object[] args)
            where TLogger : Logger =>
        logger.Error(message: "", exception: null, args);

    public static OperationLogger OnEnterAndExit(
        this Logger logger,
        [CallerMemberName] string? name = null,
        Func<(string name, object value)[]>? exitArgs = null,
        object? arg = null) =>
            logger.OnEnterAndExit(
                name,
                exitArgs,
                arg is null ? null : [arg]);

    public static OperationLogger OnEnterAndExit(
        this Logger logger,
        [CallerMemberName] string? name = null,
        Func<(string name, object value)[]>? exitArgs = null,
        params object[]? args) =>
        new(
            name ?? "",
            logger.Category,
            message: null,
            exitArgs,
            logOnStart: true,
            args);

    public static OperationLogger OnExit(
        this Logger logger,
        [CallerMemberName] string? name = null,
        Func<(string name, object value)[]>? exitArgs = null,
        object? arg = null) =>
            logger.OnExit(
                name,
                exitArgs,
                arg is null ? null : [arg]);

    public static OperationLogger OnExit(
        this Logger logger,
        [CallerMemberName] string? name = null,
        Func<(string name, object value)[]>? exitArgs = null,
        params object[]? args) =>
        new(
            name ?? "",
            logger.Category,
            message: null,
            exitArgs,
            args: args);

    public static ConfirmationLogger ConfirmOnExit(
        this Logger logger,
        [CallerMemberName] string? name = null,
        Func<(string name, object value)[]>? exitArgs = null,
        object? arg = null) =>
            logger.ConfirmOnExit(
                name,
                exitArgs,
                arg is null ? null : [arg]);

    public static ConfirmationLogger ConfirmOnExit(
        this Logger logger,
        [CallerMemberName] string? name = null,
        Func<(string name, object value)[]>? exitArgs = null,
        params object[]? args) =>
        new(
            name ?? "",
            logger.Category,
            message: null,
            exitArgs,
            args: args);

    public static ConfirmationLogger OnEnterAndConfirmOnExit(
        this Logger logger,
        [CallerMemberName] string? name = null,
        Func<(string name, object value)[]>? exitArgs = null,
        object? arg = null) =>
            logger.OnEnterAndConfirmOnExit(
                name,
                exitArgs,
                arg is null ? null : [arg]);

    public static ConfirmationLogger OnEnterAndConfirmOnExit(
        this Logger logger,
        [CallerMemberName] string? name = null,
        Func<(string name, object value)[]>? exitArgs = null,
        params object[]? args) =>
        new(
            name ?? "",
            logger.Category,
            message: null,
            exitArgs,
            logOnStart: true,
            args);

    public static void Event(
        this Logger logger,
        [CallerMemberName] string? name = null) =>
        logger.Post("",
                    LogLevel.Telemetry,
                    operationName: name);

    public static void Event(
        this Logger logger,
        [CallerMemberName] string? name = null,
        params (string, double)[] metrics) =>
        logger.Post("",
                    LogLevel.Telemetry,
                    operationName: name,
                    args: metrics.Cast<object>().ToArray());

    public static void Event(
        this Logger logger,
        [CallerMemberName] string? name = null,
        params (string name, object value)[] properties) =>
        logger.Post(null,
                    LogLevel.Telemetry,
                    operationName: name,
                    properties: properties);

    public static void Event(
        this Logger logger,
        in (string, double)[] metrics,
        in (string name, object value)[]? properties,
        [CallerMemberName] string? name = null) =>
        logger.Post("",
                    LogLevel.Telemetry,
                    operationName: name,
                    args: metrics?.Cast<object>().ToArray(),
                    properties: properties);
}

#if !SourceProject
[System.Diagnostics.DebuggerStepThrough]
#endif
internal class LogEntry
{
    public LogEntry(
        LogLevel logLevel,
        string? messageTemplate = null,
        Exception? exception = null,
        string? category = null,
        string? operationName = null,
        OperationLogger? operation = null,
        object[]? args = null)
    {
        LogLevel = logLevel;
        Exception = exception;

        if (string.IsNullOrEmpty(messageTemplate))
        {
            MessageTemplate = exception?.ToString() ?? "";
        }
        else
        {
            MessageTemplate = messageTemplate ?? "";
        }

        Category = category;
        Operation = operation;
        Args = args;

        if (operation is not null)
        {
            OperationDuration = operation.Duration;
            IsStartOfOperation = operation.IsStarting;
            IsEndOfOperation = operation.IsComplete;
            OperationId = operation.Id;
            OperationName = operationName ?? operation.Name;
            IsOperationSuccessful = operation.IsSuccessful;
        }
        else
        {
            OperationName = operationName;
            OperationId = Activity.Current?.Id;
        }
    }

    public bool IsStartOfOperation { get; }

    public bool? IsOperationSuccessful { get; }

    public bool IsEndOfOperation { get; }

    public TimeSpan? OperationDuration { get; }

    public string? OperationName { get; }

    public string? Category { get; }

    public OperationLogger? Operation { get; }

    public DateTime TimestampUtc { get; } = DateTime.UtcNow;

    public LogLevel LogLevel { get; }

    public Exception? Exception { get; }

    public string? OperationId { get; }

    public string MessageTemplate { get; }

    public List<(string Name, object Value)> Properties { get; set; } = [];

    public object[]? Args { get; set; }

    public void AddProperty((string name, object value) property) =>
        Properties.Add(property);
}

#if !SourceProject
[System.Diagnostics.DebuggerStepThrough]
#endif
internal class ConfirmationLogger : OperationLogger
{
    public ConfirmationLogger(
        string operationName,
        string? category = null,
        string? message = null,
        Func<(string name, object value)[]>? exitArgs = null,
        bool logOnStart = false,
        object[]? args = null) :
        base(operationName, category, message, exitArgs, logOnStart, args)
    {
    }

    public void Fail(
        Exception? exception = null,
        string? message = null,
        params object[]? args)
    {
        IsSuccessful = false;
        Complete(message, exception, args);
    }

    public void Succeed(
        string message = "",
        params object[] args)
    {
        IsSuccessful = true;
        Complete(message, null, args);
    }

    public override void Dispose()
    {
        if (IsSuccessful != true)
        {
            Fail();
        }
        else
        {
            Succeed();
        }
    }
}

#if !SourceProject
[System.Diagnostics.DebuggerStepThrough]
#endif
internal class OperationLogger : Logger, IDisposable
{
    private readonly Func<(string name, object value)[]>? exitArgs;
    private readonly LogEntry initialEntry;
    private bool disposed;
    private readonly Activity activity;

    public OperationLogger(
        string operationName,
        string? category = null,
        string? message = null,
        Func<(string name, object value)[]>? exitArgs = null,
        bool logOnStart = false,
        object[]? args = null) : base(category)
    {
        this.exitArgs = exitArgs;

        activity = new Activity(operationName).Start();

        IsStarting = true;

        initialEntry = new LogEntry(
            LogLevel.Information,
            message,
            null,
            category,
            operationName,
            this,
            args);

        IsStarting = false;

        if (logOnStart)
        {
            Post(initialEntry);
        }
    }

    public string? Id => activity.Id;

    public string Name => activity.OperationName;

    public TimeSpan Duration => DateTime.UtcNow - activity.StartTimeUtc;

    public bool IsComplete { get; private set; }

    public bool IsStarting { get; }

    public bool? IsSuccessful { get; protected set; }

    public override void Post(LogEntry entry)
    {
        if (disposed)
        {
            return;
        }

        base.Post(entry);
    }

    protected void Complete(
        string? message = null,
        Exception? exception = null,
        params object[]? args)
    {
        if (IsComplete)
        {
            return;
        }

        IsComplete = true;

        (string name, object value)[]? evaluatedExitArgs = null;

        if (exitArgs is not null)
        {
            try
            {
                evaluatedExitArgs = exitArgs();
            }
            catch (Exception)
            {
            }
        }

        Post(message,
             initialEntry.LogLevel,
             exception: exception,
             args: args,
             properties: evaluatedExitArgs);
    }

    public virtual void Dispose()
    {
        Complete();
        activity.Stop();
        disposed = true;
    }

#pragma warning disable CS0465
    protected virtual void Finalize()
    {
        if (!disposed)
        {
            this.Warning($"Finalize called on non-disposed logger with id {Id}");
        }
    }
}