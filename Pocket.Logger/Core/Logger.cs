using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Pocket
{
    [DebuggerStepThrough]
    internal class Logger
    {
        public Logger(string category = "")
        {
            Category = category ?? "";
        }

        public static event Action<Action<(string Name, object Value)>> Enrich;

        public static event Action<(
            byte LogLevel,
            DateTime TimestampUtc,
            Func<(string Message, (string Name, object Value)[] properties)> Evaluate,
            Exception Exception,
            string OperationName,
            string Category,
            (string Id,
            bool IsStart,
            bool IsEnd,
            bool? IsSuccessful,
            TimeSpan? duration) Operation)> Posted;

        public virtual void Post(LogEntry entry)
        {
            Enrich?.Invoke(entry.AddProperty);

            Posted?.Invoke(
                ((byte) entry.LogLevel,
                entry.TimestampUtc,
                entry.Evaluate,
                entry.Exception,
                entry.OperationName,
                entry.Category ?? Category,
                (entry.OperationId,
                entry.IsStartOfOperation,
                entry.IsEndOfOperation,
                entry.IsOperationSuccessful,
                entry.OperationDuration
                )
                )
            );
        }

        protected internal void Post(
            string message,
            LogLevel logLevel,
            string operationName = null,
            Exception exception = null,
            object[] args = null,
            (string Name, object Value)[] properties = null)
        {
            var logEntry = new LogEntry(
                message: message,
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

        public static Logger Log { get; } = new Logger();
    }

    internal class Logger<TCategory> : Logger
    {
        public Logger() : base(typeof(TCategory).FullName)
        {
        }

        public new static Logger Log { get; } = new Logger<TCategory>();
    }

    internal static partial class LogFormattingExtensions
    {
        public static string ToLogString(
            this (
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
                TimeSpan? Duration) Operation) e)
        {
            var evaluated = e.Evaluate();

            var logLevelString =
                LogLevelString((LogLevel) e.LogLevel,
                               e.Operation.IsStart,
                               e.Operation.IsEnd,
                               e.Operation.IsSuccessful,
                               e.Operation.Duration);

            return
                $"{e.TimestampUtc:o} {e.Operation.Id.IfNotEmpty()}{e.Category.IfNotEmpty()}{e.OperationName.IfNotEmpty()} {logLevelString} {evaluated.Message} {e.Exception}";
        }

        internal static string ToLogString(this object objectToFormat)
        {
            if (objectToFormat == null)
            {
                return "[null]";
            }

            if (objectToFormat is IEnumerable enumerable &&
                !(objectToFormat is string))
            {
                return $"[ {string.Join(", ", enumerable.Cast<object>())} ]";
            }

            return objectToFormat.ToString();
        }

        private static string IfNotEmpty(
            this string value,
            string prefix = "[",
            string suffix = "] ") =>
            string.IsNullOrEmpty(value)
                ? ""
                : $"{prefix}{value}{suffix}";

        private static string LogLevelString(
            LogLevel logLevel,
            bool isStartOfOperation,
            bool isEndOfOperation,
            bool? isOperationSuccessful,
            TimeSpan? duration)
        {
            string symbol()
            {
                if (isStartOfOperation)
                {
                    return "▶";
                }

                if (isEndOfOperation)
                {
                    if (isOperationSuccessful == true)
                    {
                        return "⏹ -> ✔";
                    }

                    if (isOperationSuccessful == false)
                    {
                        return "⏹ -> ❌";
                    }

                    return "⏹";
                }

                switch (logLevel)
                {
                    case LogLevel.Telemetry:
                        return "📊";
                    case LogLevel.Trace:
                    case LogLevel.Debug:
                        return "⏱";
                    case LogLevel.Information:
                        return "ℹ";
                    case LogLevel.Warning:
                        return "⚠";
                    case LogLevel.Error:
                        return "✖";
                    case LogLevel.Critical:
                        return "💩";
                    default:
                        return "ℹ";
                }
            }

            return duration == null ||
                   isStartOfOperation
                       ? symbol()
                       : $"{symbol()} ({duration?.TotalMilliseconds}ms)";
        }
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

    internal static class LoggerExtensions
    {
        public static TLogger Trace<TLogger>(
            this TLogger logger,
            string message,
            params object[] args)
            where TLogger : Logger
        {
            logger.Post(
                message,
                LogLevel.Trace,
                exception: null,
                args: args);

            return logger;
        }

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

        public static TLogger Warning<TLogger>(
            this TLogger logger,
            string message,
            Exception exception = null,
            params object[] args)
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
            logger.Warning(null, exception);

        public static TLogger Error<TLogger>(
            this TLogger logger,
            string message,
            Exception exception = null,
            params object[] args)
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
            logger.Error(null, exception);

        public static OperationLogger OnEnterAndExit(
            this Logger logger,
            [CallerMemberName] string name = null,
            Func<(string name, object value)[]> exitArgs = null)
        {
            return new OperationLogger(
                name,
                logger.Category,
                null,
                exitArgs,
                true);
        }

        public static OperationLogger OnExit(
            this Logger logger,
            [CallerMemberName] string name = null,
            Func<(string name, object value)[]> exitArgs = null) =>
            new OperationLogger(
                name,
                logger.Category,
                null,
                exitArgs);

        public static ConfirmationLogger ConfirmOnExit(
            this Logger logger,
            [CallerMemberName] string name = null,
            Func<(string name, object value)[]> exitArgs = null) =>
            new ConfirmationLogger(
                name,
                logger.Category,
                null,
                exitArgs);

        public static ConfirmationLogger OnEnterAndConfirmOnExit(
            this Logger logger,
            [CallerMemberName] string name = null,
            Func<(string name, object value)[]> exitArgs = null)
        {
            return new ConfirmationLogger(
                name,
                logger.Category,
                null,
                exitArgs,
                true);
        }

        public static void Event(
            this Logger logger,
            [CallerMemberName] string name = null) =>
            logger.Post(null,
                        LogLevel.Telemetry,
                        operationName: name);

        public static void Event(
            this Logger logger,
            [CallerMemberName] string name = null,
            params (string, double)[] metrics) =>
            logger.Post(null,
                        LogLevel.Telemetry,
                        operationName: name,
                        args: metrics.Cast<object>().ToArray());

        public static void Event(
            this Logger logger,
            [CallerMemberName] string name = null,
            params (string name, object value)[] properties) =>
            logger.Post(null,
                        LogLevel.Telemetry,
                        operationName: name,
                        properties: properties);
    }

    internal class LogEntry
    {
        private readonly List<(string Name, object Value)> properties = new List<(string, object)>();

        public LogEntry(
            LogLevel logLevel,
            string message,
            Exception exception = null,
            string category = null,
            string operationName = null,
            OperationLogger operation = null,
            object[] args = null)
        {
            LogLevel = logLevel;
            Exception = exception;
            Category = category;
            Operation = operation;
            message = message ?? "";

            if (operation != null)
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

            (string message, (string Name, object Value)[] Properties)? evaluated = null;

            Evaluate = () =>
            {
                if (evaluated == null)
                {
                    if (args?.Length != 0)
                    {
                        var formatter = Formatter.Parse(message);

                        var formatterResult = formatter.Format(args);

                        message = formatterResult.ToString();

                        properties.AddRange(formatterResult);
                    }

                    evaluated = (message, properties.ToArray());
                }

                return evaluated.Value;
            };
        }

        public Func<(string message, (string Name, object Value)[] Properties)> Evaluate { get; }

        public bool IsStartOfOperation { get; }

        public bool? IsOperationSuccessful { get; }

        public bool IsEndOfOperation { get; }

        public TimeSpan? OperationDuration { get; }

        public string OperationName { get; }

        public string Category { get; }

        public OperationLogger Operation { get; }

        public DateTime TimestampUtc { get; } = DateTime.UtcNow;

        public LogLevel LogLevel { get; }

        public Exception Exception { get; }

        public string OperationId { get; }

        public void AddProperty((string name, object value) property) =>
            properties.Add(property);
    }

    internal class ConfirmationLogger : OperationLogger
    {
        public ConfirmationLogger(
            string operationName = null,
            string category = null,
            string message = null,
            Func<(string name, object value)[]> exitArgs = null,
            bool logOnStart = false,
            object[] args = null) :
            base(operationName, category, message, exitArgs, logOnStart, args)
        {
        }

        public void Fail(
            Exception exception = null,
            string message = null,
            params object[] args)
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

    internal class OperationLogger : Logger, IDisposable
    {
        private readonly Func<(string name, object value)[]> exitArgs;
        private readonly LogEntry initialEntry;
        private bool disposed;
        private readonly Activity activity;

        public OperationLogger(
            string operationName = null,
            string category = null,
            string message = null,
            Func<(string name, object value)[]> exitArgs = null,
            bool logOnStart = false,
            object[] args = null) : base(category)
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

        public string Id => activity.Id;

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
            string message = null,
            Exception exception = null,
            params object[] args)
        {
            if (IsComplete)
            {
                return;
            }

            IsComplete = true;

            (string name, object value)[] evaluatedExitArgs = null;

            if (exitArgs != null)
            {
                try
                {
                    evaluatedExitArgs = exitArgs();
                }
                catch (Exception)
                {
                    // TODO: (Complete) publish on error channel
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
    }
}
