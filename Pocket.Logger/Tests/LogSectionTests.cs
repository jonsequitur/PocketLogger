using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Pocket.Tests
{
    public class LogSectionTests : IDisposable
    {
        private readonly IDisposable disposables;

        public LogSectionTests(ITestOutputHelper output)
        {
            disposables =
                Log.Subscribe(e =>
                                  output.WriteLine(e.ToString()));
        }

        public void Dispose() => disposables.Dispose();

        [Fact]
        public void Log_Section_can_log_on_enter()
        {
            var log = new List<LogEntry>();

            using (Log.Subscribe(log.Add))
            using (Log.OnEnterAndExit().Info("starting..."))
            {
                log.Should().ContainSingle(e => e.Message.Contains("starting..."));
            }
        }

        [Fact]
        public void Log_OnExit_logs_a_Stop_event_on_exit()
        {
            var log = new List<LogEntry>();

            using (Log.Subscribe(log.Add))
            using (Log.OnExit())
            {
            }

            log.Should().HaveCount(1);
            log.Last().IsSectionComplete.Should().BeTrue();
            log.Last().IsSectionSuccessful.Should().BeNull();
        }

        [Fact]
        public void Log_OnEnterAndExit_logs_a_Start_event_on_enter_and_a_Stop_event_on_exit()
        {
            var log = new List<LogEntry>();

            using (Log.Subscribe(log.Add))
            using (Log.OnEnterAndExit())
            {
            }

            log.Should().HaveCount(2);
            log[0].IsSectionComplete.Should().BeFalse();
            log[1].IsSectionSuccessful.Should().BeNull();
        }

        [Fact]
        public void Log_section_start_and_stop_entries_are_identifiable_in_string_output()
        {
            var log = new List<string>();

            using (Log.Subscribe(e => log.Add(e.ToString())))
            using (Log.OnEnterAndExit())
            {
            }

            log[0].Should().Contain("▶️");
            log[1].Should().Contain("⏹");
        }

        [Fact]
        public void Log_section_succeeded_entries_are_identifiable_in_string_output()
        {
            var log = new List<string>();

            using (Log.Subscribe(e => log.Add(e.ToString())))
            using (var section = Log.Confirm())
            {
                section.Success();
            }

            log[0].Should().Contain("⏹ -> ✔️");
        }

        [Fact]
        public void Log_section_failed_entries_are_identifiable_in_string_output()
        {
            var log = new List<string>();

            using (Log.Subscribe(e => log.Add(e.ToString())))
            using (Log.Confirm())
            {
            }

            log[0].Should().Contain("⏹ -> ✖");
        }

        [Fact]
        public void When_no_confirmation_is_required_then_IsSectionSuccessful_is_null()
        {
              var log = new List<LogEntry>();

            using (Log.Subscribe(e => log.Add(e)))
            using (Log.OnExit(requireConfirm: false))
            {
            }

            log.Single().IsSectionSuccessful.Should().BeNull();
        }

        [Fact]
        public void Log_section_entries_contain_operation_name_in_string_output()
        {
            var log = new List<string>();

            using (Log.Subscribe(e => log.Add(e.ToString())))
            using (var section = Log.OnEnterAndExit())
            {
                section.Info("hello");
            }

            log.Should().OnlyContain(e => e.Contains(nameof(Log_section_entries_contain_operation_name_in_string_output)));
        }

        [Fact]
        public void Log_entries_within_a_section_share_an_id_when_specified()
        {
            var log = new List<LogEntry>();
            var sectionId = "the-section-id";

            using (Log.Subscribe(log.Add))
            using (var section = Log.OnEnterAndExit(id: sectionId))
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

            using (Log.Subscribe(log.Add))
            using (var section = Log.OnEnterAndExit(id: sectionId))
            {
                section.Info("hello");
            }

            log.Select(e => e.SectionId).Distinct().Should().HaveCount(1);
        }

        [Fact]
        public async Task Log_Section_records_timing_when_completed()
        {
            var log = new List<LogEntry>();

            using (Log.Subscribe(log.Add))
            using (Log.OnExit())
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

            using (Log.Subscribe(log.Add))
            using (Log.OnEnterAndExit())
            {
                await Task.Delay(200);
            }

            log[0].ElapsedMilliseconds.Should().BeInRange(0, 50);
            log[1].ElapsedMilliseconds.Should().BeGreaterOrEqualTo(200);
        }

        [Fact]
        public void Log_Section_logs_a_successful_result_when_completed()
        {
            var log = new List<LogEntry>();

            using (Log.Subscribe(log.Add))
            using (var section = Log.OnEnterAndExit(requireConfirm: true))
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

            using (Log.Subscribe(log.Add))
            using (Log.OnEnterAndExit(requireConfirm: true))
            {
            }

            log.Last().IsSectionComplete.Should().BeTrue();
            log.Last().IsSectionSuccessful.Should().BeFalse();
        }

        [Fact]
        public void Log_Section_logs_an_unsuccessful_result_when_completed_with_Fail()
        {
            var log = new List<LogEntry>();

            using (Log.Subscribe(log.Add))
            using (var section = Log.OnEnterAndExit(requireConfirm: true))
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

            using (Log.Subscribe(log.Add))
            using (var section = Log.OnEnterAndExit())
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

            using (Log.Subscribe(log.Add))
            using (var section = Log.OnEnterAndExit())
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

            using (Log.Subscribe(log.Add))
            using (var section = Log.OnExit())
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

            using (Log.Subscribe(log.Add))
            using (var section = Log.OnExit())
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

            using (Log.Subscribe(log.Add))
            using (var section = Log.OnExit())
            {
                section.Dispose();
                section.Info("hello");
            }

            log.Count.Should().Be(1);
        }

        [Fact]
        public void Default_Section_has_no_category()
        {
            var log = new List<LogEntry>();

            using (Log.Subscribe(log.Add))
            using (var section1 = Log.OnExit())
            using (var section2 = Log.OnEnterAndExit())
            {
                section1.Info("hello");
                section2.Info("hello");
            }

            log.Should().OnlyContain(e => e.Category == null);
        }
    }
}
