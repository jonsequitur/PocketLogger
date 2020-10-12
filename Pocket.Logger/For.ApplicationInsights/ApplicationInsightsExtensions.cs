using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Metric = System.ValueTuple<string, double>;

namespace Pocket.For.ApplicationInsights
{
#if !SourceProject
    [System.Diagnostics.DebuggerStepThrough]
#endif
    internal static class ApplicationInsightsExtensions
    {
        public static LoggerSubscription SubscribeToPocketLogger(
            this TelemetryClient telemetryClient,
            IReadOnlyCollection<Assembly> onlySearchAssemblies = null)
        {
            if (telemetryClient == null)
            {
                throw new ArgumentNullException(nameof(telemetryClient));
            }

            return LogEvents.Subscribe(e =>
            {
                WriteTelemetry(telemetryClient, e);
            }, onlySearchAssemblies);
        }

        private static void WriteTelemetry(
            TelemetryClient telemetryClient,
            (
                byte LogLevel,
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
            if (e.LogLevel == (byte) LogLevel.Telemetry)
            {
                telemetryClient.TrackEvent(e.ToEventTelemetry());
            }
            else if (e.Operation.IsEnd)
            {
                telemetryClient.TrackDependency(e.ToDependencyTelemetry());
            }
            else if (e.Exception != null ||
                     e.LogLevel >= (byte) LogLevel.Warning)
            {
                telemetryClient.TrackException(e.ToExceptionTelemetry());
            }
            else
            {
                telemetryClient.TrackTrace(e.ToTraceTelemetry());
            }
        }

        private static void AddProperties(this ISupportProperties telemetry, (string Name, object Value)[] properties)
        {
            for (var i = 0; i < properties.Length; i++)
            {
                var pair = properties[i];
                if (!(pair.Value is Metric))
                {
                    telemetry.Properties.Add(
                        pair.Item1,
                        pair.Item2?.ToString());
                }
            }
        }

        internal static T AttachActivity<T>(this T telemetry) where T : ITelemetry
        {
            var activity = Activity.Current;

            if (activity != null)
            {
                telemetry.Context.Operation.Name = activity.OperationName;
                telemetry.Context.Operation.Id = activity.Id;
                telemetry.Context.Operation.ParentId = activity.ParentId;
            }

            return telemetry;
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
            telemetry.AddCategory(e.Category);

            return telemetry.AttachActivity();
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

            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
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

            telemetry.AddCategory(e.Category);
            telemetry.AddDuration(e.Operation.Duration);

            return telemetry.AttachActivity();
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
            var telemetry = new ExceptionTelemetry
            {
                Message = e.Evaluate().Message,
                Exception = e.Exception,
                SeverityLevel = MapSeverityLevel((LogLevel) e.LogLevel)
            };

            telemetry.AddProperties(e.Evaluate().Properties);
            telemetry.AddCategory(e.Category);
            telemetry.AddDuration(e.Operation.Duration);

            return telemetry.AttachActivity();
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
            telemetry.AddCategory(e.Category);
            telemetry.AddDuration(e.Operation.Duration);

            return telemetry.AttachActivity();
        }

        private static void AddCategory(this ISupportProperties telemetry, string value) =>
            telemetry.Properties.Add("Category", value);

        private static void AddDuration(this ISupportProperties telemetry, TimeSpan? duration)
        {
            if (duration.HasValue)
            {
                telemetry.Properties.Add("Duration", duration.Value.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
            }
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
