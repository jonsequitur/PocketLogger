using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using static Pocket.LogEvents;
using static Pocket.Logger<Pocket.For.ApplicationInsights.Tests.PocketLoggerForApplicationInsightsTests>;

namespace Pocket.For.ApplicationInsights.Tests;

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
                          this.output.WriteLine(e.ToLogString()));

        var telemetryConfiguration = new TelemetryConfiguration();

        telemetryConfiguration.TelemetryChannel = new FakeTelemetryChannel(telemetrySent.Add);

        client = new TelemetryClient(telemetryConfiguration);
    }

    public void Dispose() => disposables.Dispose();

    [Fact]
    public async Task Log_events_can_be_used_to_send_dependency_tracking_on_operation_complete()
    {
        client.TrackDependency(new DependencyTelemetry
        {
            Data = "http://example.com/",
            Duration = 500.Milliseconds(),
            Success = true,
            Timestamp = DateTimeOffset.UtcNow,
            Name = "my-operation",
            ResultCode = "200",
            Properties =
            {
                ["RequestUri"] = "http://example.com/",
                ["ResultCode"] = "200",
                ["Category"] = GetType().ToString()
            }
        });

        using (client.SubscribeToPocketLogger())
        using (var operation = Log.ConfirmOnExit(
                   "my-operation",
                   exitArgs: () => new (string, object)[] { ("RequestUri", new Uri("http://example.com") ) }))
        {
            await Task.Delay(200);
            operation.Succeed("{ResultCode}", 200);
        }

        var expected = (DependencyTelemetry) telemetrySent[0];
        var actual = (DependencyTelemetry) telemetrySent[1];

        actual.Data.Should().Be(expected.Data);
        actual.Duration.Should().BeGreaterOrEqualTo(190.Milliseconds());
        actual.Name.Should().Be(expected.Name);
        actual.Properties.Should().BeEquivalentTo(expected.Properties);
        actual.ResultCode.Should().Be(expected.ResultCode);
        actual.Success.Should().Be(expected.Success);
        actual.Timestamp.Should().BeCloseTo(expected.Timestamp, precision: 1500.Milliseconds());

        Log.Info(JsonConvert.SerializeObject(actual));
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
            },
            properties: new Dictionary<string, string>
            {
                ["Category"] = GetType().ToString()
            });

        using (client.SubscribeToPocketLogger())
        {
            Log.Event("my-event",
                      ("my-metric", 1.23),
                      ("my-other-metric", 123));
        }

        var expected = (EventTelemetry) telemetrySent[0];
        var actual = (EventTelemetry) telemetrySent[1];

        actual.Name.Should().Be(expected.Name);
        actual.Metrics.Should().BeEquivalentTo(expected.Metrics);
        actual.Properties.Should().BeEquivalentTo(expected.Properties);
        actual.Timestamp.Should().BeCloseTo(expected.Timestamp, precision: 1500.Milliseconds());
    }


    [Fact]
    public void Log_events_can_be_used_to_send_custom_events_with_properties()
    {
        client.TrackEvent(new EventTelemetry
        {
            Name = "my-event",
            Properties =
            {
                ["my-property"] = "my-property-value",
                ["Category"] = GetType().ToString()
            }
        });

        using (client.SubscribeToPocketLogger())
        {
            Log.Event("my-event",
                      properties: new (string, object)[]
                      {
                          ("my-property", "my-property-value")
                      }
            );
        }

        var expected = (EventTelemetry) telemetrySent[0];
        var actual = (EventTelemetry) telemetrySent[1];

        actual.Name.Should().Be(expected.Name);
        actual.Metrics.Should().BeEquivalentTo(expected.Metrics);
        actual.Properties.Should().BeEquivalentTo(expected.Properties);
        actual.Timestamp.Should().BeCloseTo(expected.Timestamp, precision: 1500.Milliseconds());
    }

    [Fact]
    public void When_a_custom_event_is_part_of_an_operation_it_includes_a_Duration_property()
    {
        using (var operation = Log.OnExit())
        using (client.SubscribeToPocketLogger())
        {
            operation.Event();
        }

        var @event = (EventTelemetry) telemetrySent[0];

        @event.Properties
              .Should()
              .ContainKey("Duration")
              .WhoseValue
              .Should()
              .MatchRegex(@"\d+", "it should be an int");
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
                Message = "oh no! 1 and 2 happened.",
                Exception = exception,
                SeverityLevel = SeverityLevel.Error,
                Properties =
                {
                    ["Category"] = GetType().ToString(),
                    ["this"] = 1.ToString(),
                    ["that"] = 2.ToString()
                }
            });

            using (client.SubscribeToPocketLogger())
            {
                Log.Error("oh no! {this} and {that} happened.", exception, 1, 2);
            }
        }

        var expected = (ExceptionTelemetry) telemetrySent[0];
        var actual = (ExceptionTelemetry) telemetrySent[1];

        actual.Exception.Should().Be(expected.Exception);
        actual.Message.Should().Be(expected.Message);
        actual.Properties.Should().BeEquivalentTo(expected.Properties);
        actual.SeverityLevel.Should().Be(expected.SeverityLevel);
        actual.Timestamp.Should().BeCloseTo(expected.Timestamp, precision: 1500.Milliseconds());
    }

    [Fact]
    public void Log_Error_uses_exception_ToString_if_no_message_is_specified()
    {
        string exceptionString;

        try
        {
            throw new Exception("oops!", new Exception("drat!"));
        }
        catch (Exception exception)
        {
            exceptionString = exception.ToString();

            using (client.SubscribeToPocketLogger())
            {
                Log.Error(exception);
            }
        }

        var actual = (ExceptionTelemetry) telemetrySent[0];

        actual.Message.Should().Be(exceptionString);
    }

    [Fact]
    public void Log_Warning_sets_message_property()
    {
        using (client.SubscribeToPocketLogger())
        {
            Log.Warning("oops...");
        }

        var actual = (ExceptionTelemetry) telemetrySent[0];

        actual.Message.Should().Be("oops...");
    }


    [Fact]
    public void Log_Warning_uses_exception_ToString_if_no_message_is_specified()
    {
        string exceptionString;

        try
        {
            throw new Exception("oops!", new Exception("drat!"));
        }
        catch (Exception exception)
        {
            exceptionString = exception.ToString();

            using (client.SubscribeToPocketLogger())
            {
                Log.Warning(exception);
            }
        }

        var actual = (ExceptionTelemetry) telemetrySent[0];

        actual.Message.Should().Be(exceptionString);
    }

    [Fact]
    public void When_an_exception_is_part_of_an_operation_it_includes_a_Duration_property()
    {
        using (var operation = Log.OnExit())
        using (client.SubscribeToPocketLogger())
        {
            operation.Warning(new Exception("oops"));
        }

        var exceptionTelemetry = (ExceptionTelemetry) telemetrySent[0];

        exceptionTelemetry.Properties
                          .Should()
                          .ContainKey("Duration")
                          .WhoseValue
                          .Should()
                          .MatchRegex(@"\d+", "it should be an int");
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
                ["some-tuple"] = "(this, 456)",
                ["Category"] = GetType().ToString()
            }
        });

        using (client.SubscribeToPocketLogger())
        {
            Log.Info("this is a trace of int {some-int} and tuple {some-tuple}", 123, ("this", 456));
        }

        var expected = (TraceTelemetry) telemetrySent[0];
        var actual = (TraceTelemetry) telemetrySent[1];

        actual.Message.Should().Be(expected.Message);
        actual.Properties.Should().BeEquivalentTo(expected.Properties);
        actual.SeverityLevel.Should().Be(expected.SeverityLevel);
        actual.Timestamp.Should().BeCloseTo(expected.Timestamp, precision: 1500.Milliseconds());
    }

    [Fact]
    public void When_a_trace_is_part_of_an_operation_it_includes_a_Duration_property()
    {
        using (var operation = Log.OnExit())
        using (client.SubscribeToPocketLogger())
        {
            operation.Trace("checkpoint");
        }

        var trace = (TraceTelemetry) telemetrySent[0];
         
        trace.Properties
             .Should()
             .ContainKey("Duration")
             .WhoseValue
             .Should()
             .MatchRegex(@"\d+", "it should be an int");
    }

    [Fact]
    public void Events_sent_after_operation_completion_are_treated_as_custom_events()
    {
        using (client.SubscribeToPocketLogger())
        using (var operation = Log.ConfirmOnExit())
        {
            operation.Succeed();

            operation.Event();
        }

        telemetrySent.Last()
                     .Should()
                     .BeOfType<EventTelemetry>();
    }

    [Fact]
    public void Context_Operation_name_is_set_in_all_telemetry()
    {
        using (client.SubscribeToPocketLogger())
        using (var operation = Log.OnEnterAndConfirmOnExit())
        {
            operation.Event();
            operation.Info("hi!");
            operation.Succeed();
        }

        WriteTelemetryToConsole();

        telemetrySent.Should()
                     .OnlyContain(t => t.Context.Operation.Name == nameof(Context_Operation_name_is_set_in_all_telemetry));
    }

    [Fact]
    public void Context_Operation_id_is_set_in_all_telemetry()
    {
        using (client.SubscribeToPocketLogger())
        using (var operation = Log.OnEnterAndConfirmOnExit())
        {
            operation.Event();
            operation.Info("hi!");
            operation.Succeed();
        }

        WriteTelemetryToConsole();

        telemetrySent.Should()
                     .OnlyContain(t => t.Context.Operation.Id != null);
    }

    [Fact]
    public void Context_Operation_parent_id_is_set_in_all_telemetry()
    {
        var activity = new Activity("the-ambient-activity").Start();

        using (Disposable.Create(() => activity.Stop()))
        using (client.SubscribeToPocketLogger())
        using (var operation = Log.OnEnterAndConfirmOnExit())
        {
            operation.Event();
            operation.Info("hi!");
            operation.Succeed();
        }

        WriteTelemetryToConsole();

        telemetrySent.Should()
                     .OnlyContain(t => t.Context.Operation.ParentId == activity.Id);
    }

    private void WriteTelemetryToConsole()
    {
        for (var i = 0; i < telemetrySent.Count; i++)
        {
            var telemetry = telemetrySent[i];
            output.WriteLine(JsonConvert.SerializeObject(telemetry, Formatting.Indented));
        }
    }
}