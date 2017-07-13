using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using static Pocket.Logger;

namespace Pocket
{
    internal static partial class Log
    {
        public static IDisposable DiscoverAndSubscribe(
            Action<IReadOnlyCollection<KeyValuePair<string, object>>> onEntryPosted)
        {
            EntryPosted += onEntryPosted ??
                           throw new ArgumentNullException(nameof(onEntryPosted));

            return Disposable.Create(() => { EntryPosted -= onEntryPosted; });
        }
    }

    internal interface ILogSection : IReadOnlyList<LogEntry>
    {
        bool IsComplete { get; }
        bool IsSuccessful { get; }
        long ElapsedMilliseconds { get; }
        string Id { get; }
        string Name { get; }
    }

    internal class LogSection : Logger, IDisposable, ILogSection
    {
        private readonly List<LogEntry> logEntries = new List<LogEntry>();

        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

        private bool disposed;

        public LogSection(
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
                               callingMethod,
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

        public bool IsSuccessful { get; private set; }

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
            bool isSuccessful,
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
                             section: this,
                             args: args));

            disposed = true;
        }

        public void Fail(
            Exception exception = null,
            string message = null,
            params object[] args) =>
            Complete(false, message, exception, args);

        public void Success(
            string message = "",
            params object[] args) =>
            Complete(true, message, null, args);

        public void Dispose()
        {
            if (RequireConfirm && !IsSuccessful)
            {
                Fail();
            }
            else
            {
                Success();
            }
        }

        public IEnumerator<LogEntry> GetEnumerator() => logEntries.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
                section: logger as ILogSection,
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
            ILogSection section = null,
            bool isTelemetry = false,
            object[] args = null)
        {
            LogLevel = logLevel;
            Exception = exception;
            Category = category;
            CallingMethod = callingMethod;
            Section = section;
            IsTelemetry = isTelemetry;

            if (section != null)
            {
                ElapsedMilliseconds = section.ElapsedMilliseconds;
                IsSectionComplete = section.IsComplete;
                IsSectionSuccessful = section.IsSuccessful;
                SectionId = section.Id;
            }

            MessageTemplate = message;

            if (args == null || args.Length == 0)
            {
                Message = message;
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

        public bool? IsSectionSuccessful { get; }

        public bool? IsSectionComplete { get; }

        public long? ElapsedMilliseconds { get; }

        public string CallingMethod { get; }

        public string Category { get; }

        // QUESTION: (LogEntry) rename to Operation? Activity?
        public ILogSection Section { get; }

        private string LogLevelString()
        {
            // 💩👀☠️👓⏱🔎🔍❗️⁉️✖️✔️👣👀⏰⚠️ℹ️☑️

            switch (LogLevel)
            {
                case LogLevel.Trace:
                    return "👣";
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

        public string Message { get; }

        public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;

        public LogLevel LogLevel { get; }

        public Exception Exception { get; }

        public void Add(string key, object value) => properties.Add(new KeyValuePair<string, object>(key, value));

        public override string ToString() =>
            $"{Timestamp:o} {CategoryString()}{OperationString()}[{LogLevelString()}] {Message} {Exception}";

        private string CategoryString() =>
            string.IsNullOrWhiteSpace(Category) ? "" : $"[{Category}] ";

        private string OperationString() =>
            string.IsNullOrWhiteSpace(CallingMethod) ? "" : $"[{CallingMethod}] ";

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() =>
            properties.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => properties.Count;

        public string MessageTemplate { get; }

        public string SectionId { get; }

        public bool IsTelemetry { get; }

        public IEnumerable<T> Properties<T>() =>
            properties.Select(p => p.Value)
                      .OfType<T>();
    }
}
