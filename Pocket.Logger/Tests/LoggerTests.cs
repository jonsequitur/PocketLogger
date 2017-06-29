using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
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

            using (Log.Subscribe(log.Add))
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
        public void Log_Section_can_log_on_enter()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            using (OnEnterAndExit().Info("starting..."))
            {
                log.Should().ContainSingle(e => e.Message.Contains("starting..."));
            }
        }

        [Fact]
        public void Log_OnExit_logs_a_Stop_event_on_exit()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            using (OnExit())
            {
            }

            log.Should().HaveCount(1);
            log.Last().IsSectionComplete.Should().BeTrue();
            log.Last().IsSectionSuccessful.Should().BeTrue();
        }

        [Fact]
        public void Log_OnEnterAndExit_logs_a_Start_event_on_enter_and_a_Stop_event_on_exit()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            using (OnEnterAndExit())
            {
            }

            log.Should().HaveCount(2);
            log[0].IsSectionComplete.Should().BeFalse();
            log[1].IsSectionSuccessful.Should().BeTrue();
        }

        [Fact]
        public void Log_entries_within_a_section_share_an_id_when_specified()
        {
            var log = new List<LogEntry>();
            var sectionId ="the-section-id";

            using (Subscribe(log.Add))
            using (var section = OnEnterAndExit(id: sectionId))
            {
                section.Info("hello");
            }

            log.Select(e => e.SectionId).Should().OnlyContain(id => id == sectionId);
        }

        [Fact]
        public void Log_entries_within_a_section_share_an_id_when_not_specified()
        {
            var log = new List<LogEntry>();
            var sectionId = Guid.NewGuid().ToString();

            using (Subscribe(log.Add))
            using (var section = OnEnterAndExit(id: sectionId))
            {
                section.Info("hello");
            }

            log.Select(e => e.SectionId).Distinct().Should().HaveCount(1);
        }

        [Fact]
        public async Task Log_Section_records_timing_when_completed()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            using (OnExit())
            {
                await Task.Delay(500);
            }

            log.Last()
               .ElapsedMilliseconds
               .Should()
               .BeGreaterOrEqualTo(500);
        }

        [Fact]
        public async Task Log_Section_records_timings_for_checkpoints()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            using (OnEnterAndExit())
            {
                await Task.Delay(200);
            }

            log[0].ElapsedMilliseconds.Should().BeInRange(0, 50);
            log[1].ElapsedMilliseconds.Should().BeInRange(200, 250);
        }

        [Fact]
        public void Log_Section_logs_a_successful_result_when_completed()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            using (var section = OnEnterAndExit(requireConfirm: true))
            {
                section.Success();
            }

            log.Last().IsSectionComplete.Should().BeTrue();
            log.Last().IsSectionSuccessful.Should().BeTrue();
        }

        [Fact]
        public void Log_Section_logs_an_unsuccessful_result_when_not_completed()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            using (OnEnterAndExit(requireConfirm: true))
            {
            }

            log.Last().IsSectionComplete.Should().BeTrue();
            log.Last().IsSectionSuccessful.Should().BeFalse();
        }

        [Fact]
        public void Log_Section_logs_an_unsuccessful_result_when_completed_with_Fail()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            using (var section = OnEnterAndExit(requireConfirm: true))
            {
                section.Fail();
            }

            log.Last().IsSectionComplete.Should().BeTrue();
            log.Last().IsSectionSuccessful.Should().BeFalse();
        }

        [Fact]
        public void Log_Section_Success_can_be_used_to_add_additional_properties_on_complete()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            using (var section = OnEnterAndExit())
            {
                section.Success("All done {0}", args: "bye!");
            }

            log.Last()
               .Select(_ => _.Value)
               .Should()
               .ContainSingle(arg => arg == "bye!");
        }

        [Fact]
        public void Log_Section_Fail_can_be_used_to_add_additional_properties_on_complete()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            using (var section = OnEnterAndExit())
            {
                section.Fail(message: "Oops! {0}", args: "bye!");
            }

            log.Last()
               .Select(_ => _.Value)
               .Should()
               .ContainSingle(arg => arg == "bye!");
        }

        [Fact]
        public void After_Success_is_called_then_a_LogSection_does_not_log_again()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            using (var section = OnExit())
            {
                section.Success(message: "Oops! {0}", args: "bye!");
                section.Info("hello");
            }

            log.Count.Should().Be(1);
        }

        [Fact]
        public void After_Fail_is_called_then_a_LogSection_does_not_log_again()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            using (var section = OnExit())
            {
                section.Fail(message: "Oops! {0}", args: "bye!");
                section.Info("hello");
            }

            log.Count.Should().Be(1);
        }

        [Fact]
        public void After_Dispose_is_called_then_a_LogSection_does_not_log_again()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            using (var section = OnExit())
            {
                section.Dispose();
                section.Info("hello");
            }

            log.Count.Should().Be(1);
        }

        [Fact]
        public void Default_logger_has_no_category()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            {
                Logger.Default.Info("hello");
                Info("hello");
            }

            log.Should().OnlyContain(e => e.Category == "");
        }

        [Fact]
        public void Default_Section_has_no_category()
        {
            var log = new List<LogEntry>();

            using (Subscribe(log.Add))
            using (var section1 = OnExit())
            using (var section2 = OnEnterAndExit())
            {
                section1.Info("hello");
                section2.Info("hello");
            }

            log.Should().OnlyContain(e => e.Category == null);
        }

        [Fact(Skip = "not implemented yet")]
        public void All_logs_can_be_filtered_using_middleware()
        {
            // TODO (Logs_can_be_enriched) write test
            Assert.True(false, "Test Logs_can_be_enriched is not written yet.");
        }

        [Fact(Skip = "not implemented yet")]
        public void All_logs_can_be_enriched_using_middleware()
        {
            // TODO (Logs_can_be_enriched) write test
            Assert.True(false, "Test Logs_can_be_enriched is not written yet.");
        }

        [Fact(Skip = "not implemented yet")]
        public void Exceptions_thrown_by_middleware_do_not_impact_the_caller()
        {
            // TODO (Exceptions_thrown_by_enrichers_do_not_impact_the_caller) write test
            Assert.True(false, "Test Exceptions_thrown_by_enrichers_do_not_impact_the_caller is not written yet.");
        }

        [Fact(Skip = "not implemented yet")]
        public void Loggers_in_different_assemblies_can_be_discovered_and_subscribed()
        {
            // TODO (Loggers_in_different_assemblies_can_be_discovered_and_subscribed) write test
            Assert.True(false, "Test Loggers_in_different_assemblies_can_be_discovered_and_subscribed is not written yet.");
        }
    }
}
