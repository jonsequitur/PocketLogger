using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Xunit;
using Xunit.Abstractions;

namespace Pocket.For.ApplicationInsights.Tests
{
    public class PocketLoggerForApplicationInsightsTests : IDisposable
    {
        private readonly TelemetryClient client;
        private readonly ITestOutputHelper output;

        private readonly IDisposable disposables;
        private readonly List<ITelemetry> telemetrySent;

        public PocketLoggerForApplicationInsightsTests(ITestOutputHelper output)
        {
            this.output = output;
            telemetrySent = new List<ITelemetry>();

            disposables =
                Log.Subscribe(e =>
                                  this.output.WriteLine(e.ToString()));

            client = new TelemetryClient(
                new TelemetryConfiguration(
                    "<instrumentation key>",
                    new FakeTelemetryChannel(telemetrySent.Add)
                    {
                        DeveloperMode = true
                    }));
        }

        public void Dispose() => disposables.Dispose();

        [Fact]
        public void Log_events_can_be_used_to_send_dependency_tracking_on_operation_complete()
        {
            var id = Guid.NewGuid().ToString();
            client.TrackDependency(new DependencyTelemetry
            {
                Data = "my-operation",
                Duration = 500.Milliseconds(),
                Id = id,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow,
                Name = "my-operation"
            });

            using (client.SubscribeToPocketLogger())
            using (var operation = Log.ConfirmOnExit("my-operation", id))
            {
                Thread.Sleep(500);
                operation.Succeed();
            }

            var expected = (DependencyTelemetry) telemetrySent[0];
            var actual = (DependencyTelemetry) telemetrySent[1];

            actual.Data.Should().Be(expected.Data);
            actual.Duration.Should().BeCloseTo(expected.Duration);
            actual.Id.Should().Be(expected.Id);
            actual.Name.Should().Be(expected.Name);
            actual.Success.Should().Be(expected.Success);
            actual.Timestamp.Should().BeCloseTo(expected.Timestamp, precision: 1500);
        }

        [Fact(Skip = "Not implemented yet")]
        public void Log_events_can_be_used_to_send_custom_metrics()
        {
            client.TrackMetric(
                "my-event",
                value: 1.23);

            using (client.SubscribeToPocketLogger())
            {
                // TODO      Log.Event(("my-metric", 1.23));
            }

            var expected = (MetricTelemetry) telemetrySent[0];
            var actual = (MetricTelemetry) telemetrySent[1];

            telemetrySent[0].ShouldBeEquivalentTo(telemetrySent[1]);
        }

        [Fact]
        public void Log_events_can_be_used_to_send_custom_events()
        {
            client.TrackEvent(
                "my-event",
                //                properties: new Dictionary<string, string>
                //                {
                //                    ["my-property"] = "my-property-value"
                //                },
                metrics: new Dictionary<string, double>
                {
                    ["my-metric"] = 1.23
                });

            using (client.SubscribeToPocketLogger())
            {
                Log.Event("my-event", ("my-metric", 1.23));
            }

            var expected = (EventTelemetry) telemetrySent[0];
            var actual = (EventTelemetry) telemetrySent[1];

            actual.Name.Should().Be(expected.Name);
            actual.Metrics.ShouldBeEquivalentTo(expected.Metrics);
            actual.Properties.ShouldBeEquivalentTo(expected.Properties);
            actual.Timestamp.Should().BeCloseTo(expected.Timestamp, precision: 1500);
        }

        [Fact]
        public void Log_events_can_be_used_to_send_exceptions()
        {
            try
            {
                throw new Exception("oops!");
            }
            catch (Exception exception)
            {
                client.TrackException(new ExceptionTelemetry
                {
                    Message = "oh no!",
                    Exception = exception,
                    SeverityLevel = SeverityLevel.Error
                });

                using (client.SubscribeToPocketLogger())
                {
                    Log.Error("oh no!", exception);
                }
            }

            var expected = (ExceptionTelemetry) telemetrySent[0];
            var actual = (ExceptionTelemetry) telemetrySent[1];

            actual.Exception.Should().Be(expected.Exception);
            actual.Message.Should().Be(expected.Message);
            actual.SeverityLevel.Should().Be(expected.SeverityLevel);
            actual.Timestamp.Should().BeCloseTo(expected.Timestamp, precision: 1500);
        }

        [Fact]
        public void Log_events_can_be_used_to_send_traces()
        {
            client.TrackTrace(new TraceTelemetry
            {
                Message = $"this is a trace of int 123 and tuple (this, 456)",
                SeverityLevel = SeverityLevel.Information,
                Properties =
                {
                    ["some-int"] = "123",
                    ["some-tuple"] = "(this, 456)"
                }
            });

            using (client.SubscribeToPocketLogger())
            {
                Log.Info("this is a trace of int {some-int} and tuple {some-tuple}", 123, ("this", 456));
            }

            var expected = (TraceTelemetry) telemetrySent[0];
            var actual = (TraceTelemetry) telemetrySent[1];

            actual.Message.Should().Be(expected.Message);
            actual.Properties.ShouldBeEquivalentTo(expected.Properties);
            actual.SeverityLevel.Should().Be(expected.SeverityLevel);
            actual.Timestamp.Should().BeCloseTo(expected.Timestamp, precision: 1500);
        }
    }
}
