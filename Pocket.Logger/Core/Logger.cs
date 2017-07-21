using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Pocket
{
    internal static partial class Log
    {
        public static void Trace(
            string message,
            params object[] args) =>
            Logger.Default.Trace(message, args);

        public static void Info(
            string message,
            params object[] args) =>
            Logger.Default.Info(message, args);

        public static void Warning(
            string message,
            Exception exception = null,
            params object[] args) =>
            Logger.Default.Warning(message, exception, args);

        public static void Error(
            string message,
            Exception exception = null,
            params object[] args) =>
            Logger.Default.Error(message, exception, args);

        public static OperationLogger OnEnterAndExit(
            bool requireConfirm = false,
            [CallerMemberName] string name = null,
            string id = null)
        {
            var operation = new OperationLogger(
                requireConfirm,
                name,
                id: id);

            operation.Log(operation[0]);

            return operation;
        }

        public static OperationLogger OnExit(
            bool requireConfirm = false,
            [CallerMemberName] string name = null,
            string id = null) =>
            new OperationLogger(
                requireConfirm,
                name,
                id: id);

        public static OperationLogger ConfirmOnExit(
            [CallerMemberName] string name = null,
            string id = null) =>
            new OperationLogger(
                true,
                name,
                id: id);

        public static void Event(
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

            Logger.Default.Log(
                new LogEntry(LogLevel.Telemetry,
                             message: "{name}",
                             callingMethod: name,
                             category: nameof(Event),
                             args: args.ToArray()
                ));
        }

        public static TLogger Trace<TLogger>(
            this TLogger logger,
            string message,
            params object[] args)
            where TLogger : Logger
        {
            logger.Log(
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
            logger.Log(
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
            logger.Log(
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
            logger.Log(
                CreateLogEntry(
                    logger,
                    message,
                    LogLevel.Error,
                    exception,
                    args));

            return logger;
        }

        public static string Format(
            this (
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
                TimeSpan? duration) Operation) e)
        {
            var evaluated = e.LogEntry.Evaluate();

            var logLevelString =
                LogLevelString((LogLevel) e.LogEntry.LogLevel,
                               e.Operation.IsStart,
                               e.Operation.IsEnd,
                               e.Operation.IsSuccessful);

            return
                $"{e.LogEntry.Timestamp:o} {e.Operation.Id.BracketIfNotEmpty()}{e.LogEntry.category.BracketIfNotEmpty()}{e.LogEntry.OperationName.BracketIfNotEmpty()}[{logLevelString}] {evaluated.Message} {e.LogEntry.Exception}";
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

        public override void Log(LogEntry entry)
        {
            if (disposed)
            {
                return;
            }

            logEntries.Add(entry);

            base.Log(entry);
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

            Log(new LogEntry(initialLogEntry.LogLevel,
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

        public virtual void Log(LogEntry entry) =>
            Posted?.Invoke(
                (
                ((int) entry.LogLevel,
                entry.Timestamp,
                entry.Evaluate,
                entry.Exception,
                entry.OperationName,
                entry.Category
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

        public static Logger Default { get; } = new Logger(category: "");
    }

    internal class Logger<TCaller> : Logger
    {
        public Logger() : base(typeof(TCaller).FullName)
        {
        }

        public new static Logger Default { get; } = new Logger<TCaller>();
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
                        message =  message ?? "";
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
}
