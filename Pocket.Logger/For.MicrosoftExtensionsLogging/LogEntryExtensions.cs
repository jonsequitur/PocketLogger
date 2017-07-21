using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Pocket.For.MicrosoftExtensionsLogging
{
    internal static class LoggerExtensions
    {
        public static IDisposable SubscribeToPocket(
            this ILogger logger)
        {
            return Log.Subscribe(e =>
            {
                logger.Log(
                    ((LogLevel) e.LogEntry.LogLevel).MapLogLevel(),
                    new EventId(),
                    e.LogEntry
                     .Evaluate()
                     .Properties,
                    e.LogEntry.Exception,
                    (state, exception) => e.Format());
            });
        }
    }

    internal static class LogEntryExtensions
    {
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
