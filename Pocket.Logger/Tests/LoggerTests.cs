using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using static Pocket.Log;

namespace Pocket.Tests
{
    public class LoggerTests : IDisposable
    {
        private readonly ITestOutputHelper output;

        private readonly IDisposable disposables;

        public LoggerTests(ITestOutputHelper output)
        {
            this.output = output;

            disposables =
                Subscribe(e =>
                              output.WriteLine(e.ToString()));
        }

        public void Dispose() => disposables.Dispose();

        [Fact]
        public void Subscribe_allows_all_log_events_to_be_monitored()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            {
                Info("hello");
            }

            log.Should().ContainSingle(e => e.Message.Contains("hello"));
        }

        [Fact]
        public void Exceptions_thrown_by_subscribers_are_not_thrown_to_the_caller()
        {
            using (Subscribe(_ => throw new Exception("drat!")))
            {
                Info("hello");
            }
        }

        [Fact]
        public void Further_log_entries_are_not_received_after_disposing_the_subscription()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            {
            }

            Info("hello");

            log.Should()
               .BeEmpty();
        }

        [Fact]
        public void When_args_are_logged_then_they_are_templated_into_the_message()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            {
                Info("It's {time} and all is {how}",
                     DateTimeOffset.Parse("12/12/2012 12:00am"),
                     "well");
            }

            log.Single()
               .Message
               .Should()
               .Match("*It's 12/12/2012 12:00:00 AM * and all is well*");
        }

        [Fact]
        public void When_args_are_logged_they_are_accessble_on_the_LogEntry()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            {
                Info("It's 12 o'clock and all is {how}", "well");
            }

            log.Single()
               .Should()
               .ContainSingle(p =>
                                  p.Key == "how" &&
                                  p.Value == "well");
        }

        [Fact]
        public void When_exceptions_are_logged_then_the_message_contains_the_exception_details()
        {
            var log = new List<LogEntry>();
            var exception = new Exception("oops!");

            using (Subscribe(log.Add))
            {
                Error("oh no!", exception);
            }

            log.Single()
               .ToString()
               .Should()
               .Contain(exception.ToString());
        }

        [Fact]
        public void When_exceptions_are_logged_then_they_are_accessible_on_the_LogEntry()
        {
            var log = new List<LogEntry>();
            var exception = new Exception("oops!");

            using (Subscribe(log.Add))
            {
                Error("oh no!", exception);
            }

            log.Single()
               .Exception
               .Should()
               .Be(exception);
        }

        [Fact]
        public void Logs_can_be_written_at_Info_level()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            {
                Info("hello");
            }

            log.Single().LogLevel.Should().Be(LogLevel.Information);
        }

        [Fact]
        public void Logs_can_be_written_at_Warning_level()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            {
                Warning("hello");
            }

            log.Single().LogLevel.Should().Be(LogLevel.Warning);
        }

        [Fact]
        public void Logs_can_be_written_at_Error_level()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            {
                Error("oops!", new Exception("something went wrong..."));
            }

            log.Single().LogLevel.Should().Be(LogLevel.Error);
        }

        [Fact(Skip = "not implemented")]
        public void Log_captures_the_calling_method()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            {
                Info("hello");
            }

            log.Should()
               .Contain(e => e.CallingMethod == nameof(Log_captures_the_calling_method));
        }

        [Fact]
        public void Logger_T_captures_a_category_based_on_type_T()
        {
            var logger = new Logger<LoggerTests>();

            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            {
                logger.Info("hello");
            }

            log.Should().Contain(e => e.Category == typeof(LoggerTests).FullName);
        }

        [Fact]
        public void Default_logger_has_no_category()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            {
                Logger.Default.Info("hello");
            }

            log.Should().OnlyContain(e => e.Category == "");
        }
    }
}
