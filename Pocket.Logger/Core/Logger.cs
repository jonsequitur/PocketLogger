using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Pocket
{
    internal class Logger
    {
        public Logger(string category = null)
        {
            Category = category;
        }

        public static event Action<(
            (int LogLevel,
            DateTimeOffset Timestamp,
            Func<(string Message, IReadOnlyCollection<KeyValuePair<string, object>> properties)> Evaluate,
            Exception Exception,
            string OperationName,
            string Category) logEntry,
            (string Id,
            bool IsStart,
            bool IsEnd,
            bool? IsSuccessful,
            TimeSpan? duration) Operation)> Posted;

        public virtual void Post(LogEntry entry) =>
            Posted?.Invoke(
                (
                ((int) entry.LogLevel,
                entry.Timestamp,
                entry.Evaluate,
                entry.Exception,
                entry.OperationName,
                entry.Category ?? Category
                ),
                (entry.OperationId,
                entry.IsStartOfOperation,
                entry.IsEndOfOperation,
                entry.IsOperationSuccessful,
                entry.OperationDuration
                )
                )
            );

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
        public static string Format(
            this (
                (int LogLevel,
                DateTimeOffset Timestamp,
                Func<(string Message, IReadOnlyCollection<KeyValuePair<string, object>> Properties)> Evaluate,
                Exception Exception,
                string OperationName,
                string Category) LogEntry,
                (string Id,
                bool IsStart,
                bool IsEnd,
                bool? IsSuccessful,
                TimeSpan? duration) Operation) e)
        {
            var evaluated = e.LogEntry.Evaluate();

            var logLevelString =
                LogLevelString((LogLevel) e.LogEntry.LogLevel,
                               e.Operation.IsStart,
                               e.Operation.IsEnd,
                               e.Operation.IsSuccessful);

            return
                $"{e.LogEntry.Timestamp:o} {e.Operation.Id.BracketIfNotEmpty()}{e.LogEntry.Category.BracketIfNotEmpty()}{e.LogEntry.OperationName.BracketIfNotEmpty()}[{logLevelString}] {evaluated.Message} {e.LogEntry.Exception}";
        }

        private static (
            (int LogLevel,
            DateTimeOffset Timestamp,
            Func<(string Message, IReadOnlyCollection<KeyValuePair<string, object>> properties)> Evaluate,
            Exception Exception,
            string OperationName,
            string category) LogEntry,
            (string Id,
            bool IsStart,
            bool IsEnd,
            bool? IsSuccessful,
            TimeSpan? Duration) Operation) Tuplify(
                LogLevel logLevel,
                string message,
                Exception exception = null,
                string category = null,
                string callingMethod = null,
                OperationLogger operation = null,
                object[] args = null)
        {
            var logEntry = new LogEntry(logLevel, message, exception, category, callingMethod, operation, args);

            return (dynamic) logEntry;
        }

        public static string BracketIfNotEmpty(this string value) =>
            string.IsNullOrEmpty(value) ? "" : $"[{value}]";

        private static string LogLevelString(
            LogLevel logLevel,
            bool isStartOfOperation,
            bool isEndOfOperation,
            bool? isOperationSuccessful)
        {
            if (isStartOfOperation)
            {
                return "▶️";
            }

            if (isEndOfOperation)
            {
                if (isOperationSuccessful == true)
                {
                    return "⏹ -> ✔️";
                }

                if (isOperationSuccessful == false)
                {
                    return "⏹ -> ✖️";
                }

                return "⏹";
            }

            switch (logLevel)
            {
                case LogLevel.Trace:
                    return "🐾";
                case LogLevel.Debug:
                    return "🔍";
                case LogLevel.Information:
                    return "ℹ️";
                case LogLevel.Warning:
                    return "⚠️";
                case LogLevel.Error:
                    return "✖️";
                case LogLevel.Critical:
                    return "💩";
                default:
                    return "ℹ️";
            }
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
                CreateLogEntry(
                    logger,
                    message,
                    LogLevel.Trace,
                    exception: null,
                    args: args));

            return logger;
        }

        public static TLogger Info<TLogger>(
            this TLogger logger,
            string message,
            params object[] args)
            where TLogger : Logger
        {
            logger.Post(
                CreateLogEntry(
                    logger,
                    message,
                    LogLevel.Information,
                    exception: null,
                    args: args));

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
                CreateLogEntry(
                    logger,
                    message,
                    LogLevel.Warning,
                    exception,
                    args));

            return logger;
        }

        public static TLogger Error<TLogger>(
            this TLogger logger,
            string message,
            Exception exception = null,
            params object[] args)
            where TLogger : Logger
        {
            logger.Post(
                CreateLogEntry(
                    logger,
                    message,
                    LogLevel.Error,
                    exception,
                    args));

            return logger;
        }

        public static OperationLogger OnEnterAndExit(
            this Logger logger,
            bool requireConfirm = false,
            [CallerMemberName] string name = null,
            string id = null)
        {
            var operation = new OperationLogger(
                requireConfirm,
                name,
                category: logger.Category,
                id: id);

            operation.Post(operation[0]);

            return operation;
        }

        public static OperationLogger OnExit(
            this Logger logger,
            bool requireConfirm = false,
            [CallerMemberName] string name = null,
            string id = null) =>
            new OperationLogger(
                requireConfirm,
                name,
                category: logger.Category,
                id: id);

        public static OperationLogger ConfirmOnExit(
            this Logger logger,
            [CallerMemberName] string name = null,
            string id = null) =>
            new OperationLogger(
                true,
                name,
                category: logger.Category,
                id: id);

        public static void Event(
            this Logger logger,
            [CallerMemberName] string name = null,
            params (string name, double value)[] metrics)
        {
            var args = new List<object>
            {
                name
            };

            if (metrics != null)
            {
                foreach (var metric in metrics)
                {
                    args.Add(metric);
                }
            }

            logger.Post(
                new LogEntry(LogLevel.Telemetry,
                             message: "{name}",
                             callingMethod: name,
                             category: logger.Category,
                             args: args.ToArray()
                ));
        }

        private static LogEntry CreateLogEntry<TLogger>(
            TLogger logger,
            string message,
            LogLevel logLevel,
            Exception exception = null,
            object[] args = null) where TLogger : Logger
        {
            return new LogEntry(
                message: message,
                logLevel: logLevel,
                exception: exception,
                category: logger.Category,
                operation: logger as OperationLogger,
                args: args);
        }
    }

    internal class LogEntry
    {
        private readonly List<KeyValuePair<string, object>> properties = new List<KeyValuePair<string, object>>();

        public LogEntry(
            LogLevel logLevel,
            string message,
            Exception exception = null,
            string category = null,
            string callingMethod = null,
            OperationLogger operation = null,
            object[] args = null)
        {
            LogLevel = logLevel;
            Exception = exception;
            Category = category;

            if (operation != null)
            {
                OperationDuration = operation.Duration;
                IsStartOfOperation = operation.Count == 0;
                IsEndOfOperation = operation.IsComplete;
                OperationId = operation?.Id;
                OperationName = callingMethod ?? operation.Name;

                if (operation.IsSuccessful != null)
                {
                    IsOperationSuccessful = operation.IsSuccessful == true;
                }
            }
            else
            {
                OperationName = callingMethod;
            }

            (string message, IReadOnlyCollection<KeyValuePair<string, object>> Properties)? evaluated = null;

            Evaluate = () =>
            {
                if (evaluated == null)
                {
                    if (args == null || args.Length == 0)
                    {
                        message = message ?? "";
                    }
                    else
                    {
                        var formatter = Formatter.Parse(message);

                        var formatterResult = formatter.Format(args);

                        message = formatterResult.ToString();

                        properties.AddRange(formatterResult);
                    }
                    evaluated = (message, properties);
                }

                return evaluated.Value;
            };
        }

        public Func<(string message, IReadOnlyCollection<KeyValuePair<string, object>> Properties)> Evaluate { get; }

        public bool IsStartOfOperation { get; }

        public bool? IsOperationSuccessful { get; }

        public bool IsEndOfOperation { get; }

        public TimeSpan? OperationDuration { get; }

        public string OperationName { get; }

        public string Category { get; }

        public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;

        public LogLevel LogLevel { get; }

        public Exception Exception { get; }

        public string OperationId { get; }
    }

    internal class OperationLogger : Logger, IDisposable
    {
        private readonly List<LogEntry> logEntries = new List<LogEntry>();

        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

        private bool disposed;

        public OperationLogger(
            bool requireConfirm = false,
            [CallerMemberName] string callingMethod = null,
            string category = null,
            string id = null,
            params object[] args) : base(category)
        {
            Id = id ?? Guid.NewGuid().ToString();
            RequireConfirm = requireConfirm;

            Name = callingMethod;

            logEntries.Add(new LogEntry(
                               LogLevel.Information,
                               null,
                               null,
                               category,
                               callingMethod,
                               this,
                               args: args));
        }

        public string Id { get; }

        public string Name { get; }

        public TimeSpan Duration => stopwatch.Elapsed;

        public bool RequireConfirm { get; }

        public bool IsComplete { get; private set; }

        public bool? IsSuccessful { get; private set; }

        public override void Post(LogEntry entry)
        {
            if (disposed)
            {
                return;
            }

            logEntries.Add(entry);

            base.Post(entry);
        }

        private void Complete(
            bool? isSuccessful,
            string message = null,
            Exception exception = null,
            params object[] args)
        {
            if (IsComplete)
            {
                return;
            }

            IsComplete = true;
            IsSuccessful = isSuccessful;

            stopwatch.Stop();

            var initialLogEntry = this[0];

            Post(new LogEntry(initialLogEntry.LogLevel,
                              message,
                              exception: exception,
                              category: initialLogEntry.Category,
                              operation: this,
                              args: args));

            disposed = true;
        }

        public void Fail(
            Exception exception = null,
            string message = null,
            params object[] args) =>
            Complete(false, message, exception, args);

        public void Succeed(
            string message = "",
            params object[] args) =>
            Complete(true, message, null, args);

        public void Dispose()
        {
            if (RequireConfirm)
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
            else
            {
                Complete(null, this[0].OperationName);
            }
        }

        public IEnumerator<LogEntry> GetEnumerator() => logEntries.GetEnumerator();

        public int Count => logEntries.Count;

        public LogEntry this[int index] => logEntries[index];
    }
}
