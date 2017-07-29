using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Pocket
{
    internal class Logger
    {
        public Logger(string category = null)
        {
            Category = category;
        }

        public static event Action<Action<(string Name, object Value)>> Enrich;

        public static event Action<(
            int LogLevel,
            DateTimeOffset Timestamp,
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
                ((int) entry.LogLevel,
                entry.Timestamp,
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

        public static Logger Log { get; } = new Logger(category: "");
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
                $"{e.Timestamp:o} {e.Operation.Id.IfNotEmpty()}{e.Category.IfNotEmpty()}{e.OperationName.IfNotEmpty()} {logLevelString}  {evaluated.Message} {e.Exception}";
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
                        return "🔍";
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

            return duration == null
                       ? symbol()
                       : $"{symbol()} ({duration?.TotalMilliseconds}ms)";
        }
    }

    internal enum LogLevel
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
            string id = null,
            Func<(string name, object value)[]> exitArgs = null)
        {
            var operation = new OperationLogger(
                name,
                logger.Category,
                id,
                logger as OperationLogger,
                exitArgs);

            operation.Post(operation[0]);

            return operation;
        }

        public static OperationLogger OnExit(
            this Logger logger,
            bool requireConfirm = false,
            [CallerMemberName] string name = null,
            string id = null,
            Func<(string name, object value)[]> exitArgs = null) =>
            new OperationLogger(
                name,
                logger.Category,
                id,
                logger as OperationLogger,
                exitArgs);

        public static ConfirmationLogger ConfirmOnExit(
            this Logger logger,
            [CallerMemberName] string name = null,
            string id = null,
            Func<(string name, object value)[]> exitArgs = null) =>
            new ConfirmationLogger(
                name,
                logger.Category,
                id,
                logger as OperationLogger,
                exitArgs);

        public static ConfirmationLogger OnEnterAndConfirmOnExit(
            this Logger logger,
            [CallerMemberName] string name = null,
            string id = null,
            Func<(string name, object value)[]> exitArgs = null)
        {
            var operation = new ConfirmationLogger(
                name,
                logger.Category,
                id,
                logger as OperationLogger,
                exitArgs);

            operation.Post(operation[0]);

            return operation;
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
                IsStartOfOperation = operation.Count == 0;
                IsEndOfOperation = operation.IsComplete;
                OperationId = operation?.Id;
                OperationName = operationName ?? operation.Name;
                IsOperationSuccessful = operation.IsSuccessful;
            }
            else
            {
                OperationName = operationName;
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

        public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;

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
            string id = null,
            OperationLogger parentOperation = null,
            Func<(string name, object value)[]> exitArgs = null) :
            base(operationName, category, id, parentOperation, exitArgs)
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
        private readonly List<LogEntry> logEntries = new List<LogEntry>();
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private bool disposed;
        private int sequenceNumber;

        public OperationLogger(
            string operationName = null,
            string category = null,
            string id = null,
            OperationLogger parentOperation = null,
            Func<(string name, object value)[]> exitArgs = null) : base(category)
        {
            this.exitArgs = exitArgs;
            Id = id ?? CreateId(parentOperation);

            Name = operationName;

            logEntries.Add(new LogEntry(
                               LogLevel.Information,
                               null,
                               null,
                               category,
                               operationName,
                               this));
        }

        private string CreateId(OperationLogger parentOperation = null) =>
            parentOperation != null
                ? $"{parentOperation.Id}.{Interlocked.Increment(ref parentOperation.sequenceNumber)}"
                : Guid.NewGuid().ToString();

        public string Id { get; }

        public string Name { get; }

        public TimeSpan Duration => stopwatch.Elapsed;

        public bool IsComplete { get; private set; }

        public bool? IsSuccessful { get; protected set; }

        public override void Post(LogEntry entry)
        {
            if (disposed)
            {
                return;
            }

            logEntries.Add(entry);

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
                 this[0].LogLevel,
                 exception: exception,
                 args: args,
                 properties: evaluatedExitArgs);
        }

        public virtual void Dispose()
        {
            stopwatch.Stop();
            Complete();
            disposed = true;
        }

        public IEnumerator<LogEntry> GetEnumerator() => logEntries.GetEnumerator();

        public int Count => logEntries.Count;

        public LogEntry this[int index] => logEntries[index];
    }
}
