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
                new LogEntry(LogLevel.Trace,
                             message: "{name}",
                             callingMethod: name,
                             category: nameof(Event),
                             isTelemetry: true,
                             args: args.ToArray()
                ));
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

        public long ElapsedMilliseconds => stopwatch.ElapsedMilliseconds;

        public bool RequireConfirm { get; }

        public bool IsComplete { get; private set; }

        public bool? IsSuccessful { get; private set; }

        public override void Log(LogEntry logEntry)
        {
            if (disposed)
            {
                return;
            }

            logEntries.Add(logEntry);

            base.Log(logEntry);
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
                Complete(null, this[0].Message);
            }
        }

        public IEnumerator<LogEntry> GetEnumerator() => logEntries.GetEnumerator();

        public int Count => logEntries.Count;

        public LogEntry this[int index] => logEntries[index];
    }

    internal enum LogLevel
    {
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

        public static event Action<LogEntry> EntryPosted;

        public virtual void Log(LogEntry logEntry) => EntryPosted?.Invoke(logEntry);

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

    internal static class LoggerExtensions
    {
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

    internal class LogEntry : IReadOnlyCollection<KeyValuePair<string, object>>
    {
        private readonly List<KeyValuePair<string, object>> properties = new List<KeyValuePair<string, object>>();

        public LogEntry(
            LogLevel logLevel,
            string message,
            Exception exception = null,
            string category = null,
            string callingMethod = null,
            OperationLogger operation = null,
            bool isTelemetry = false,
            object[] args = null)
        {
            LogLevel = logLevel;
            Exception = exception;
            Category = category;
            Operation = operation;
            IsTelemetry = isTelemetry;

            if (operation != null)
            {
                ElapsedMilliseconds = operation.ElapsedMilliseconds;
                IsStartOfOperation = operation.Count == 0;
                IsOperationComplete = operation.IsComplete;
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

            MessageTemplate = message ?? "";

            if (args == null || args.Length == 0)
            {
                Message = MessageTemplate;
            }
            else
            {
                // TODO: (LogEntry) make this lazy upon initial ToString or iteration
                var formatter = Formatter.Parse(message);

                var formatterResult = formatter.Format(args);

                properties.AddRange(formatterResult);

                Message = formatterResult.ToString();
            }
        }

        public bool IsStartOfOperation { get; }

        public bool? IsOperationSuccessful { get; }

        public bool? IsOperationComplete { get; }

        public long? ElapsedMilliseconds { get; }

        public string OperationName { get; }

        public string Category { get; }

        public OperationLogger Operation { get; }

        public string Message { get; }

        public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;

        public LogLevel LogLevel { get; }

        public Exception Exception { get; }

        public void Add(string key, object value) => properties.Add(new KeyValuePair<string, object>(key, value));
     
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() =>
            properties.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => properties.Count;

        public string MessageTemplate { get; }

        public string OperationId { get; }

        public bool IsTelemetry { get; }

        public IEnumerable<T> Properties<T>() =>
            properties.Select(p => p.Value)
                      .OfType<T>();
        
        public override string ToString() =>
            $"{Timestamp:o} {OperationIdString()}{CategoryString()}{OperationString()}[{LogLevelString()}] {Message} {Exception}";

        private string CategoryString() =>
            string.IsNullOrWhiteSpace(Category) ? "" : $"[{Category}] ";

        private string LogLevelString()
        {
            if (IsStartOfOperation)
            {
                return "▶️";
            }

            if (IsOperationComplete == true)
            {
                if (IsOperationSuccessful == true)
                {
                    return "⏹ -> ✔️";
                }

                if (IsOperationSuccessful == false)
                {
                    
                    return "⏹ -> ✖️";
                }

                return "⏹";
            }

            switch (LogLevel)
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

        private string OperationString() =>
            string.IsNullOrWhiteSpace(OperationName) ? "" : $"[{OperationName}] ";

        private string OperationIdString() =>
            OperationId == null ? "" : $"[{OperationId}] ";
    }
}
