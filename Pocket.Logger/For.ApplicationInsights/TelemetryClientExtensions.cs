using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Metric = System.ValueTuple<string, double>;

namespace Pocket.For.ApplicationInsights
{
    internal static class TelemetryClientExtensions
    {
        public static IDisposable SubscribeToPocketLogger(
            this TelemetryClient telemetryClient,
            bool discoverOtherPocketLoggers = false)
        {
            return LogEvents.Subscribe(e =>
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
                    var evaluated = entry.Evaluate();

                    telemetryClient.TrackEvent(
                        eventName: entry.OperationName,
                        metrics: evaluated
                            .Properties
                            .Select(p => p.Value)
                            .OfType<Metric>()
                            .ToDictionary(
                                _ => _.Item1,
                                _ => _.Item2),
                        properties: evaluated
                            .Properties
                            .Select(p => p.Value)
                            .OfType<(string, string)>()
                            .Select(_ => new KeyValuePair<string, object>(_.Item1, _.Item2))
                            .ToDictionary(_ => _.Key,
                                          _ => _.Value?.ToString()));
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
