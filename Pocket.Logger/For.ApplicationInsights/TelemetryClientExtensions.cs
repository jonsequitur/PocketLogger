using System;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Pocket.For.ApplicationInsights
{
    internal static class TelemetryClientExtensions
    {
        public static IDisposable SubscribeToPocketLogger(
            this TelemetryClient telemetryClient,
            bool discoverOtherPocketLoggers = false)
        {
            return Log.Subscribe(e =>
            {
                var entry = e.LogEntry;
                var operation = e.Operation;

                if (operation.IsEnd)
                {
                    var telemetry = new DependencyTelemetry
                    {
                        Id = operation.Id,
                        Data = entry.OperationName,
                        Duration = operation.Duration.Value,
                        Name = entry.OperationName,
                        Success = operation.IsSuccessful,
                        Timestamp = DateTimeOffset.UtcNow
                    };

                    foreach (var pair in entry.Evaluate().Properties)
                    {
                        telemetry.Properties.Add(pair.Key, pair.Value?.ToString());
                    }

                    telemetryClient.TrackDependency(telemetry);
                }
                else if (entry.LogLevel == (int) LogLevel.Telemetry)
                {
                    telemetryClient.TrackEvent(
                        eventName: entry.OperationName,
                        metrics: entry.Evaluate()
                                      .Properties
                                      .Select(p => p.Value)
                                      .OfType<(string name, double value)>()
                                      .ToDictionary(
                                          _ => _.name,
                                          _ => _.value)
                    );
                }
                else if (entry.Exception != null)
                {
                    telemetryClient.TrackException(new ExceptionTelemetry
                    {
                        Message = entry.Evaluate().Message,
                        Exception = entry.Exception,
                        Properties =
                        {
                            ["log"] = e.ToString()
                        },
                        SeverityLevel = MapSeverityLevel((LogLevel) entry.LogLevel)
                    });
                }
                else
                {
                    var traceTelemetry = new TraceTelemetry
                    {
                        Message = entry.Evaluate().Message,
                        SeverityLevel = MapSeverityLevel((LogLevel) entry.LogLevel)
                    };

                    foreach (var property in entry.Evaluate().Properties)
                    {
                        traceTelemetry.Properties.Add(property.Key, property.Value?.ToString());
                    }

                    telemetryClient.TrackTrace(traceTelemetry);
                }
            }, discoverOtherPocketLoggers: discoverOtherPocketLoggers);
        }

        private static SeverityLevel MapSeverityLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    return SeverityLevel.Verbose;

                case LogLevel.Information:
                    return SeverityLevel.Information;

                case LogLevel.Warning:
                    return SeverityLevel.Warning;

                case LogLevel.Error:
                    return SeverityLevel.Error;

                case LogLevel.Critical:
                    return SeverityLevel.Critical;

                default:
                    return SeverityLevel.Information;
            }
        }
    }
}
