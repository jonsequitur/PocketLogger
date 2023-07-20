﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
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
        public void Start_entries_do_not_write_duration_in_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToLogString())))
            using (Log.OnEnterAndExit())
            {
            }

            log[0].Should().NotContain("ms)");
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
        public void Events_contain_operation_name_in_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToString())))
            using (var operation = Log.OnEnterAndExit())
            {
                operation.Info("hello");
            }

            log.Should().OnlyContain(e => e.Contains(nameof(Events_contain_operation_name_in_string_output)));
        }

        [Fact]
        public void Log_events_within_an_operation_share_an_id()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (var operation = Log.OnEnterAndExit())
            {
                operation.Info("hello");
            }

            log.Select(e => e.Operation.Id).Distinct().Should().HaveCount(1);
        }

        [Fact]
        public async Task Operation_id_is_the_same_before_and_after_operation()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            {
                Log.Info("not part of the operation");

                using (Log.OnEnterAndExit())
                using (Log.OnEnterAndConfirmOnExit())
                {
                    await Task.Yield();
                    Log.Info("hello");
                }

                Log.Info("not part of the operation");
            }

            log.Where(e => e.Evaluate().Message == "not part of the operation")
               .Select(e => e.Operation.Id)
               .Distinct()
               .Should()
               .HaveCount(1);
        }

        [Fact]
        public void Log_events_emitted_by_regular_Loggers_within_the_scope_of_an_operation_bear_the_id_of_the_ambient_operation()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (Log.OnEnterAndExit())
            {
                Log.Info("hello");
            }

            log.Select(e => e.Operation.Id).Distinct().Should().HaveCount(1);
        }

        [Fact]
        public void Log_events_emitted_by_regular_Loggers_within_the_scope_of_an_operation_include_the_ambient_operation_id_in_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToLogString())))
            using (var operation = Log.OnExit())
            {
                Log.Info("hello");

                log.First()
                   .Should()
                   .Match($"*{operation.Id}*hello*");
            }
        }

        [Fact]
        public void Log_entries_within_an_operation_contain_their_id_in_string_output()
        {
            var log = new LogEntryList();
            string operationId;

            using (Subscribe(log.Add))
            using (var operation = Log.OnEnterAndExit())
            {
                operationId = operation.Id;

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
                await Task.Delay(550);
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
                await Task.Delay(250);
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

        [Fact]
        public void Exit_args_appear_in_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToLogString())))
            using (Log.OnExit(exitArgs: () => new (string, object)[]
            {
                ("hello", 12345)
            }))
            {
            }

            log.Last()
               .Should()
               .Contain("hello");
            log.Last()
               .Should()
               .Contain("12345");
        }

        [Fact]
        public void OperationLogger_message_appears_in_initial_log_event_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToLogString())))
            using (new OperationLogger(operationName: "Test", message: "hello!", logOnStart: true))
            {
            }

            log.First().Should().Contain("hello!");
        }

        [Fact]
        public void OperationLogger_args_appears_in_initial_log_event_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToLogString())))
            using (new OperationLogger(operationName: "Test", message: "{one} and {two} and {three}", logOnStart: true, args: new object[] { 1, 2, 3 }))
            {
            }

            log.First().Should().Contain("1 and 2 and 3");
        }
    }
}
