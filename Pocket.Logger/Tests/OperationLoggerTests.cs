using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static Pocket.LogEvents;
using static Pocket.Logger;

namespace Pocket.Tests
{
    public class OperationLoggerTests : IDisposable
    {
        private readonly IDisposable disposables;

        public OperationLoggerTests(ITestOutputHelper output)
        {
            disposables =
                Subscribe(e =>
                              output.WriteLine(e.ToLogString()));
        }

        public void Dispose() => disposables.Dispose();

        [Fact]
        public void Log_OnEnterAndExit_logs_a_Start_event_on_exit()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (Log.OnEnterAndExit())
            {
                log.Should().ContainSingle(e => e.Operation.IsStart);
            }
        }

        [Fact]
        public void Log_OnExit_logs_a_Stop_event_on_exit()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (Log.OnExit())
            {
            }

            log.Should().HaveCount(1);
            log.Last().Operation.IsEnd.Should().BeTrue();
            log.Last().Operation.IsSuccessful.Should().BeNull();
        }

        [Fact]
        public void Log_OnEnterAndExit_logs_a_Start_event_on_enter_and_a_Stop_event_on_exit()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (Log.OnEnterAndExit())
            {
            }

            log.Should().HaveCount(2);
            log[0].Operation.IsEnd.Should().BeFalse();
            log[1].Operation.IsSuccessful.Should().BeNull();
        }

        [Fact]
        public void Start_and_stop_entries_are_identifiable_in_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToLogString())))
            using (Log.OnEnterAndExit())
            {
            }

            log[0].Should().Contain("▶");
            log[1].Should().Contain("⏹");
        }

        [Fact]
        public async Task Stop_entries_write_duration_in_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToLogString())))
            using (Log.OnExit())
            {
                await Task.Delay(10);
            }

            log[0].Should().Match("*⏹ (*ms)*");
        }

        [Fact]
        public void When_no_confirmation_is_required_then_IsOperationSuccessful_is_null()
        {
            var log = new LogEntryList();

            using (Subscribe(e => log.Add(e)))
            using (Log.OnExit())
            {
            }

            log.Single()
               .Operation
               .IsSuccessful
               .Should()
               .BeNull();
        }

        [Fact]
        public void Entries_contain_operation_name_in_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToString())))
            using (var operation = Log.OnEnterAndExit())
            {
                operation.Info("hello");
            }

            log.Should().OnlyContain(e => e.Contains(nameof(Entries_contain_operation_name_in_string_output)));
        }

        [Fact]
        public void Log_entries_within_an_operation_share_an_id_when_specified()
        {
            var log = new LogEntryList();
            var operationId = "the-operation-id";

            using (Subscribe(log.Add))
            using (var operation = Log.OnEnterAndExit(id: operationId))
            {
                operation.Info("hello");
            }

            log.Select(e => e.Operation.Id).Should().OnlyContain(id => id == operationId);
        }

        [Fact]
        public void Log_entries_within_an_operation_share_an_id_when_not_specified()
        {
            var log = new LogEntryList();
            var operationId = Guid.NewGuid().ToString();

            using (Subscribe(log.Add))
            using (var operation = Log.OnEnterAndExit(id: operationId))
            {
                operation.Info("hello");
            }

            log.Select(e => e.Operation.Id).Distinct().Should().HaveCount(1);
        }

        [Fact]
        public void Log_entries_within_an_operation_contain_their_id_in_string_output()
        {
            var log = new LogEntryList();
            var operationId = Guid.NewGuid().ToString();

            using (Subscribe(log.Add))
            using (var operation = Log.OnEnterAndExit(id: operationId))
            {
                operation.Info("hello");
            }

            log.Should().OnlyContain(e => e.ToString().Contains(operationId));
        }

        [Fact]
        public async Task It_records_timing_when_completed()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (Log.OnExit())
            {
                await Task.Delay(500);
            }

            log.Last()
               .Operation
               .Duration
               .Value
               .TotalMilliseconds
               .Should()
               .BeGreaterOrEqualTo(500);
        }

        [Fact]
        public async Task It_records_timings_for_checkpoints()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (Log.OnEnterAndExit())
            {
                await Task.Delay(200);
            }

            log[0].Operation.Duration?.TotalMilliseconds.Should().BeInRange(0, 50);
            log[1].Operation.Duration?.TotalMilliseconds.Should().BeGreaterOrEqualTo(200);
        }

        [Fact]
        public async Task Timings_for_checkpoints_are_written_to_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToLogString())))
            using (var operation = Log.OnExit())
            {
                await Task.Delay(20);
                operation.Event();
                await Task.Delay(20);
                operation.Info("");
                await Task.Delay(20);
                operation.Warning("");
                await Task.Delay(20);
                operation.Error("");
            }

            log[0].Should().Match("*(*ms)*");
            log[1].Should().Match("*(*ms)*");
            log[2].Should().Match("*(*ms)*");
            log[3].Should().Match("*(*ms)*");
        }

        [Fact]
        public void After_Dispose_is_called_then_an_OperationLogger_does_not_log_again()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (var operation = Log.OnExit())
            {
                operation.Dispose();
                operation.Info("hello");
            }

            log.Count.Should().Be(1);
        }

        [Fact]
        public void By_befault_an_operation_has_no_category()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (var operation1 = Log.OnExit())
            using (var operation2 = Log.OnEnterAndExit())
            {
                operation1.Info("hello");
                operation2.Info("hello");
            }

            log.Should().OnlyContain(e => e.Category == "");
        }

        [Fact]
        public void OnEnterAndExit_can_capture_args_on_exit()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (Log.OnEnterAndExit(exitArgs: () => new (string, object)[]
            {
                ("hello", 123)
            }))
            {
            }

            log.Last()
               .Evaluate()
               .Properties
               .Should()
               .Contain(p => p.Name == "hello" && p.Value.Equals(123));
        }

        [Fact]
        public void OnExit_can_capture_args_on_exit()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (Log.OnExit(exitArgs: () => new (string, object)[]
            {
                ("hello", 123)
            }))
            {
            }

            log.Last()
               .Evaluate()
               .Properties
               .Should()
               .Contain(p => p.Name == "hello" && p.Value.Equals(123));
        }
    }
}
