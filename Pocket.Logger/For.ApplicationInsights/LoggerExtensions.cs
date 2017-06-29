using System;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Pocket.For.ApplicationInsights
{
    internal static class LoggerExtensions
    {
        public static IDisposable SubscribeToPocket(
            this TelemetryClient telemetryClient)
        {
            return Log.Subscribe(entry =>
            {
                if (entry?.Section?.IsComplete == true)
                {
                    var telemetry = new DependencyTelemetry
                    {
                        Id = entry.Section.Id,
                        Data = entry.Section.Name,
                        Duration = TimeSpan.FromMilliseconds(entry.Section.ElapsedMilliseconds),
                        Name = entry.Section.Name,
                        Success = entry.Section.IsSuccessful,
                        Timestamp = DateTimeOffset.UtcNow
                    };

                    foreach (var pair in entry)
                    {
                        telemetry.Properties.Add(pair.Key, pair.Value?.ToString());
                    }

                    telemetryClient.TrackDependency(telemetry);
                }
                else if (entry.IsTelemetry)
                {
                    telemetryClient.TrackEvent(
                        eventName: entry.CallingMethod,
                        metrics: entry.Properties<(string name, double value)>()
                                      .ToDictionary(
                                          _ => _.name,
                                          _ => _.value)
                    );
                }
                else if (entry.Exception != null)
                {
                    telemetryClient.TrackException(new ExceptionTelemetry
                    {
                        Message = entry.Message,
                        Exception = entry.Exception,
                        Properties =
                        {
                            ["log"] = entry.ToString()
                        },
                        SeverityLevel = MapSeverityLevel(entry.LogLevel)
                    });
                }
            });
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
