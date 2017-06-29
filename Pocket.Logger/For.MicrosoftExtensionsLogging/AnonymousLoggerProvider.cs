using System;
using Microsoft.Extensions.Logging;

namespace Pocket.For.MicrosoftExtensionsLogging
{
    internal static class LoggerFactoryExtensions
    {
        public static ILoggerFactory Add(
            this ILoggerFactory factory,
            LogDelegate log)
        {
            factory.AddProvider(new AnonymousLoggerProvider(
                                    categoryName => new AnonymousLogger(log)));

            return factory;
        }

        private class AnonymousLogger : ILogger
        {
            private readonly LogDelegate log;

            public AnonymousLogger(LogDelegate log) =>
                this.log = log ?? throw new ArgumentNullException(nameof(log));

            public void Log<TState>(
                Microsoft.Extensions.Logging.LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception exception,
                Func<TState, Exception, string> formatter) =>
                log(logLevel, eventId, state, exception, (s, e) =>
                        formatter((TState) s, e));

            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) =>
                true;

            public IDisposable BeginScope<TState>(TState state) =>
                Disposable.Create(() => { });
        }

        private class AnonymousLoggerProvider : ILoggerProvider
        {
            private readonly Func<string, ILogger> createLogger;

            public AnonymousLoggerProvider(
                Func<string, ILogger> createLogger) => this.createLogger =
                                                           createLogger ?? throw new ArgumentNullException(nameof(createLogger));

            public void Dispose()
            {
            }

            public ILogger CreateLogger(string categoryName) => createLogger(categoryName);
        }
    }

    internal delegate void LogDelegate(
        Microsoft.Extensions.Logging.LogLevel logLevel,
        EventId eventId,
        object state,
        Exception exception,
        Func<object, Exception, string> formatter);
}
