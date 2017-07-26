using System;
using System.Collections.Generic;
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
            bool discoverOtherPocketLoggers = false)
        {
            return LogEvents.Subscribe(e =>
            {
                if (e.Operation.IsEnd)
                {
                    telemetryClient.TrackDependency(e.ToDependencyTelemetry());
                }
                else if (e.LogLevel == (int) LogLevel.Telemetry)
                {
                    telemetryClient.TrackEvent(e.ToEventTelemetry());
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
            this (int LogLevel,
                DateTimeOffset Timestamp,
                Func<(string Message, IReadOnlyCollection<KeyValuePair<string, object>> Properties)> Evaluate,
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
                Data = properties.FirstOrDefault(p => p.Key == "RequestUri").Value?.ToString(),
                Duration = e.Operation.Duration.Value,
                Name = e.OperationName,
                ResultCode = properties.FirstOrDefault(p => p.Key == "ResultCode").Value?.ToString(),
                Success = e.Operation.IsSuccessful,
                Timestamp = DateTimeOffset.UtcNow
            };

            foreach (var pair in properties)
            {
                telemetry.Properties.Add(pair.Key, pair.Value?.ToString());
            }

            return telemetry;
        }

        internal static EventTelemetry ToEventTelemetry(
            this ( int LogLevel,
                DateTimeOffset Timestamp,
                Func<(string Message, IReadOnlyCollection<KeyValuePair<string, object>> Properties)> Evaluate,
                Exception Exception,
                string OperationName,
                string Category,
                (string Id,
                bool IsStart,
                bool IsEnd,
                bool? IsSuccessful,
                TimeSpan? Duration) Operation) e)
        {
            var evaluated = e.Evaluate();

            var telemetry = new EventTelemetry
            {
                Name = e.OperationName
            };

            foreach (var metric in evaluated
                .Properties
                .Select(p => p.Value)
                .OfType<Metric>())
            {
                telemetry.Metrics.Add(metric.Item1, metric.Item2);
            }

            return telemetry;
        }

        internal static ExceptionTelemetry ToExceptionTelemetry(
            this ( int LogLevel,
                DateTimeOffset Timestamp,
                Func<(string Message, IReadOnlyCollection<KeyValuePair<string, object>> Properties)> Evaluate,
                Exception Exception,
                string OperationName,
                string Category,
                (string Id,
                bool IsStart,
                bool IsEnd,
                bool? IsSuccessful,
                TimeSpan? Duration) Operation) e) =>
            new ExceptionTelemetry
            {
                Message = e.Evaluate().Message,
                Exception = e.Exception,
                Properties =
                {
                    ["log"] = e.ToString()
                },
                SeverityLevel = MapSeverityLevel((LogLevel) e.LogLevel)
            };

        internal static TraceTelemetry ToTraceTelemetry(
            this ( int LogLevel,
                DateTimeOffset Timestamp,
                Func<(string Message, IReadOnlyCollection<KeyValuePair<string, object>> Properties)> Evaluate,
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

            foreach (var property in e.Evaluate().Properties)
            {
                telemetry.Properties.Add(property.Key, property.Value?.ToString());
            }

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
