using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;

namespace Pocket.For.MicrosoftExtensionsLogging
{
    internal static class LoggerExtensions
    {
        public static IDisposable SubscribeToPocket(
            this ILogger logger)
        {
            return Log.Subscribe(entry =>
            {
                logger.Log(
                    entry.LogLevel.MapLogLevel(),
                    new EventId(),
                    entry.ToFormattedLogValues(),
                    entry.Exception,
                    (state, exception) => entry.ToString());
            });
        }
    }

    internal static class LogEntryExtensions
    {
        public static FormattedLogValues ToFormattedLogValues(this LogEntry logEntry)
        {
            var logValues = new FormattedLogValues(
                logEntry.MessageTemplate,
                logEntry.Select(_ => _.Value).ToArray());

            return logValues;
        }

        internal static Microsoft.Extensions.Logging.LogLevel MapLogLevel(this LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return Microsoft.Extensions.Logging.LogLevel.Trace;

                case LogLevel.Debug:
                    return Microsoft.Extensions.Logging.LogLevel.Debug;

                case LogLevel.Information:
                    return Microsoft.Extensions.Logging.LogLevel.Information;

                case LogLevel.Warning:
                    return Microsoft.Extensions.Logging.LogLevel.Warning;

                case LogLevel.Error:
                    return Microsoft.Extensions.Logging.LogLevel.Error;

                case LogLevel.Critical:
                    return Microsoft.Extensions.Logging.LogLevel.Critical;

                default:
                    return Microsoft.Extensions.Logging.LogLevel.Information;
            }
        }
    }
}
