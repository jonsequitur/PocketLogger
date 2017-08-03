using System;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Metric = System.ValueTuple<string, double>;

namespace Pocket.For.ApplicationInsights
{
    internal static class ApplicationInsightsExtensions
    {
        public static IDisposable SubscribeToPocketLogger(
            this TelemetryClient telemetryClient,
            bool discoverOtherPocketLoggers = true)
        {
            return LogEvents.Subscribe(e =>
            {
                if (e.LogLevel == (byte) LogLevel.Telemetry)
                {
                    telemetryClient.TrackEvent(e.ToEventTelemetry());
                }
                else if (e.Operation.IsEnd)
                {
                    telemetryClient.TrackDependency(e.ToDependencyTelemetry());
                }
                else if (e.Exception != null)
                {
                    telemetryClient.TrackException(e.ToExceptionTelemetry());
                }
                else
                {
                    telemetryClient.TrackTrace(e.ToTraceTelemetry());
                }
            }, discoverOtherPocketLoggers: discoverOtherPocketLoggers);
        }

        internal static DependencyTelemetry ToDependencyTelemetry(
            this (byte LogLevel,
                DateTime TimestampUtc,
                Func<(string Message, (string Name, object Value)[] Properties)> Evaluate,
                Exception Exception,
                string OperationName,
                string Category,
                (string Id,
                bool IsStart,
                bool IsEnd,
                bool? IsSuccessful,
                TimeSpan? Duration) Operation) e)
        {
            var properties = e.Evaluate().Properties;

            var telemetry = new DependencyTelemetry
            {
                Id = e.Operation.Id,
                Data = properties.FirstOrDefault(p => p.Name == "RequestUri").Value?.ToString(),
                Duration = e.Operation.Duration.Value,
                Name = e.OperationName,
                ResultCode = properties.FirstOrDefault(p => p.Name == "ResultCode").Value?.ToString(),
                Success = e.Operation.IsSuccessful,
                Timestamp = DateTimeOffset.UtcNow
            };

            telemetry.AddProperties(properties);

            return telemetry;
        }

        private static void AddProperties(this ISupportProperties telemetry, (string Name, object Value)[] properties)
        {
            foreach (var pair in properties)
            {
                if (!(pair.Value is Metric))
                {
                    telemetry.Properties.Add(
                        pair.Item1,
                        pair.Item2?.ToString());
                }
            }
        }

        internal static EventTelemetry ToEventTelemetry(
            this (byte LogLevel,
                DateTime TimestampUtc,
                Func<(string Message, (string Name, object Value)[] Properties)> Evaluate,
                Exception Exception,
                string OperationName,
                string Category,
                (string Id,
                bool IsStart,
                bool IsEnd,
                bool? IsSuccessful,
                TimeSpan? Duration) Operation) e)
        {
            var properties = e.Evaluate().Properties;

            var telemetry = new EventTelemetry
            {
                Name = e.OperationName
            };

            foreach (var property in properties)
            {
                if (property.Value is Metric m)
                {
                    telemetry.Metrics.Add(
                        m.Item1,
                        m.Item2);
                }
                else
                {
                    telemetry.Properties.Add(
                        property.Name,
                        property.Value.ToLogString());
                }
            }

            return telemetry;
        }

        internal static ExceptionTelemetry ToExceptionTelemetry(
            this (byte LogLevel,
                DateTime TimestampUtc,
                Func<(string Message, (string Name, object Value)[] Properties)> Evaluate,
                Exception Exception,
                string OperationName,
                string Category,
                (string Id,
                bool IsStart,
                bool IsEnd,
                bool? IsSuccessful,
                TimeSpan? Duration) Operation) e)
        {
            return new ExceptionTelemetry
            {
                Message = e.Evaluate().Message,
                Exception = e.Exception,
                SeverityLevel = MapSeverityLevel((LogLevel) e.LogLevel)
            };
        }

        internal static TraceTelemetry ToTraceTelemetry(
            this (byte LogLevel,
                DateTime TimestampUtc,
                Func<(string Message, (string Name, object Value)[] Properties)> Evaluate,
                Exception Exception,
                string OperationName,
                string Category,
                (string Id,
                bool IsStart,
                bool IsEnd,
                bool? IsSuccessful,
                TimeSpan? Duration) Operation) e)
        {
            var telemetry = new TraceTelemetry
            {
                Message = e.Evaluate().Message,
                SeverityLevel = MapSeverityLevel((LogLevel) e.LogLevel)
            };

            telemetry.AddProperties(e.Evaluate().Properties);

            return telemetry;
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
