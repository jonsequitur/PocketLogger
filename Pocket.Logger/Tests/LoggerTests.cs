using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using static Pocket.Logger;
using static Pocket.LogEvents;

namespace Pocket.Tests
{
    public class LoggerTests : IDisposable
    {
        private readonly IDisposable disposables;

        public LoggerTests(ITestOutputHelper output)
        {
            disposables = Subscribe(e =>
                                        output.WriteLine(e.ToLogString(), false));
        }

        public void Dispose()
        {
            disposables.Dispose();
        }

        [Fact]
        public void Subscribe_allows_all_log_events_to_be_monitored()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            {
                Log.Info("hello");
            }

            log.Should()
               .ContainSingle(e =>
                                  e.Evaluate()
                                   .Message
                                   .Contains("hello"));
        }

        [Fact]
        public void Exceptions_thrown_by_subscribers_are_not_thrown_to_the_caller()
        {
            using (Subscribe(_ => throw new Exception("drat!")))
            {
                Log.Info("hello");
            }
        }

        [Fact]
        public void Further_log_entries_are_not_received_after_disposing_the_subscription()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            {
            }

            Log.Info("hello");

            log.Should()
               .BeEmpty();
        }

        [Fact]
        public void When_args_are_logged_then_they_are_templated_into_the_message()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            {
                Log.Info("It's {time} and all is {how}",
                         DateTimeOffset.Parse("12/12/2012 12:00am"),
                         "well");
            }

            log.Single()
               .Evaluate()
               .Message
               .Should()
               .Match("*It's 12/12/*12 12:00:00 AM * and all is well*");
        }

        [Fact]
        public void When_args_are_logged_they_are_accessble_on_the_LogEntry()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            {
                Log.Info("It's 12 o'clock and all is {how}", "well");
            }

            log.Single()
               .Evaluate()
               .Properties
               .Should()
               .ContainSingle(p =>
                                  p.Name.Equals("how") &&
                                  p.Value.Equals("well"));
        }

        [Fact]
        public void When_exceptions_are_logged_then_the_message_contains_the_exception_details()
        {
            var log = new LogEntryList();
            var exception = new Exception("oops!");

            using (Subscribe(log.Add))
            {
                Log.Error("oh no!", exception);
            }

            log.Single()
               .ToString()
               .Should()
               .Contain(exception.ToString());
        }

        [Fact]
        public void When_exceptions_are_logged_then_they_are_accessible_on_the_LogEntry()
        {
            var log = new LogEntryList();
            var exception = new Exception("oops!");

            using (Subscribe(log.Add))
            {
                Log.Error("oh no!", exception);
            }

            log.Single()
               .Exception
               .Should()
               .Be(exception);
        }

        [Fact]
        public void Logs_can_be_written_at_Info_level()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            {
                Log.Info("hello");
            }

            log.Single().LogLevel.Should().Be((int) LogLevel.Information);
        }

        [Fact]
        public void Logs_can_be_written_at_Warning_level()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            {
                Log.Warning("hello", new Exception("oops"));
            }

            log.Single().LogLevel.Should().Be((int) LogLevel.Warning);
            log.Single().Exception.Should().NotBeNull();
        }

        [Fact]
        public void Exceptions_can_be_written_at_Warning_level()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            {
                Log.Warning(new Exception("oops"));
            }

            log.Single().LogLevel.Should().Be((int) LogLevel.Warning);
            log.Single().Exception.Should().NotBeNull();
        }

        [Fact]
        public void Logs_can_be_written_at_Error_level()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            {
                Log.Error("oops!", new Exception("something went wrong..."));
            }

            log.Single().LogLevel.Should().Be((int) LogLevel.Error);
            log.Single().Exception.Should().NotBeNull();
        }

        [Fact]
        public void Exceptions_can_be_written_at_Error_level()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            {
                Log.Error(new Exception("oops"));
            }

            log.Single().LogLevel.Should().Be((int) LogLevel.Error);
            log.Single().Exception.Should().NotBeNull();
        }

        [Fact]
        public void Logger_T_captures_a_category_based_on_type_T()
        {
            var logger = new Logger<LoggerTests>();

            var log = new LogEntryList();

            using (Subscribe(log.Add))
            {
                logger.Info("hello");
            }

            log.Should().Contain(e => e.Category == typeof(LoggerTests).FullName);
        }

        [Fact]
        public void Default_logger_has_no_category()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            {
                new Logger().Info("hello");
                Log.Info("hello");
            }

            log.Should().OnlyContain(e => e.Category == "");
        }

        [Fact]
        public void Log_entries_have_no_duration()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            {
                Log.Info("hello");
            }

            log.Should().OnlyContain(e => e.Operation.Duration == null);
            log[0].ToLogString().Should().NotContain("ms)");
        }

        [Fact]
        public void Event_metrics_are_written_to_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToLogString())))
            {
                Log.Event("some-event", metrics: ("some-metric", 12345));
            }

            log[0].Should().Contain("some-metric");
            log[0].Should().Contain("12345");
        }

        [Fact]
        public void Event_properties_are_written_to_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToLogString())))
            {
               // Log.Info("this", "some-property", "hi!");
                Log.Event("some-event", properties: ("some-property", "hi!"));
            }

            log[0].Should().Contain("some-property");
            log[0].Should().Contain("hi!");
        }
    }
}
