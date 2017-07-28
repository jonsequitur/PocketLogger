using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Pocket.Tests;
using Xunit;
using Xunit.Abstractions;
using static Pocket.LogEvents;
using static Pocket.Logger;

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
                Subscribe(e =>
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
                Data = "http://example.com/",
                Duration = 500.Milliseconds(),
                Id = id,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow,
                Name = "my-operation",
                ResultCode = "200",
                Properties =
                {
                    ["RequestUri"] = "http://example.com/",
                    ["ResultCode"] = "200"
                }
            });

            using (client.SubscribeToPocketLogger())
            using (var operation = Log.ConfirmOnExit(
                "my-operation",
                id,
                exitArgs: () => new (string, object)[] { ("RequestUri", new Uri("http://example.com") ) }))
            {
                Thread.Sleep(500);
                operation.Succeed("{ResultCode}", 200);
            }

            var expected = (DependencyTelemetry) telemetrySent[0];
            var actual = (DependencyTelemetry) telemetrySent[1];

            actual.Data.Should().Be(expected.Data);
            actual.Duration.Should().BeCloseTo(expected.Duration);
            actual.Id.Should().Be(expected.Id);
            actual.Name.Should().Be(expected.Name);
            actual.Properties.ShouldBeEquivalentTo(expected.Properties);
            actual.ResultCode.Should().Be(expected.ResultCode);
            actual.Success.Should().Be(expected.Success);
            actual.Timestamp.Should().BeCloseTo(expected.Timestamp, precision: 1500);
        }

        [Fact]
        public void Log_events_can_be_used_to_send_custom_events_with_metrics()
        {
            client.TrackEvent(
                "my-event",
                metrics: new Dictionary<string, double>
                {
                    ["my-metric"] = 1.23,
                    ["my-other-metric"] = 123
                });

            using (client.SubscribeToPocketLogger())
            {
                Log.Event("my-event",
                          ("my-metric", 1.23),
                          ("my-other-metric", (double) 123));
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
