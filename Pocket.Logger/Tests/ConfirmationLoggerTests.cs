using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;
using Xunit.Abstractions;
using static Pocket.LogEvents;
using static Pocket.Logger<Pocket.Tests.ConfirmationLoggerTests>;

namespace Pocket.Tests
{
    public class ConfirmationLoggerTests : IDisposable
    {
        private readonly IDisposable disposables;

        public ConfirmationLoggerTests(ITestOutputHelper output)
        {
            disposables =
                Subscribe(e =>
                              output.WriteLine(e.ToLogString()));
            Activity.DefaultIdFormat = ActivityIdFormat.Hierarchical;
        }

        public void Dispose() => disposables.Dispose();

        [Fact]
        public void Successful_entries_are_identifiable_in_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToLogString())))
            using (var operation = Logger.Log.ConfirmOnExit())
            {
                operation.Succeed();
            }

            log[0].Should().Contain("⏹ -> ✔");
        }

        [Fact]
        public void Failed_entries_are_identifiable_in_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToLogString())))
            using (Logger.Log.ConfirmOnExit())
            {
            }

            log[0].Should().Contain("⏹ -> ❌");
        }

        [Fact]
        public async Task Successful_entries_write_duration_in_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToLogString())))
            using (var operation = Log.ConfirmOnExit())
            {
                await Task.Delay(10);
                operation.Succeed();
            }

            log[0].Should().Match("*⏹ -> ✔ (*ms)*");
        }

        [Fact]
        public async Task Failed_entries_write_duration_in_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToLogString())))
            using (Log.ConfirmOnExit())
            {
                await Task.Delay(10);
            }

            log[0].Should().Match("*⏹ -> ❌ (*ms)*");
        }

        [Fact]
        public void When_confirmation_is_required_then_it_logs_a_successful_result_when_completed()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                operation.Succeed();
            }

            log.Last().Operation.IsEnd.Should().BeTrue();
            log.Last().Operation.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void When_confirmation_is_required_then_it_logs_an_unsuccessful_result_when_not_confirmed()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (Log.OnEnterAndConfirmOnExit())
            {
                // don't call Fail or Succeed
            }

            log.Last().Operation.IsEnd.Should().BeTrue();
            log.Last().Operation.IsSuccessful.Should().BeFalse();
        }

        [Fact]
        public void When_confirmation_is_required_then_it_logs_an_unsuccessful_result_when_completed_with_Fail()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                operation.Fail();
            }

            log.Last().Operation.IsEnd.Should().BeTrue();
            log.Last().Operation.IsSuccessful.Should().BeFalse();
        }

        [Fact]
        public void Succeed_can_be_used_to_add_additional_properties_on_complete()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                operation.Succeed("All done {0}", args: "bye!");
            }

            log.Last()
               .Evaluate()
               .Properties
               .Select(_ => _.Value)
               .Should()
               .ContainSingle(arg => arg.Equals("bye!"));
        }

        [Fact]
        public void Fail_can_be_used_to_add_additional_properties_on_complete()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                operation.Fail(message: "Oops! {0}", args: "bye!");
            }

            var properties = log.Last()
                                 .Evaluate()
                                 .Properties;

            properties
               .Select(_ => _.Value)
               .Should()
               .ContainSingle(arg => arg.Equals("bye!"));
        }

        [Fact]
        public void After_Succeed_is_called_then_a_ConfirmationLogger_does_not_log_on_dispose()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (var operation = Log.ConfirmOnExit())
            {
                operation.Succeed();
                operation.Info("hello");
            }

            log.Last().Evaluate().Message.Should().Be("hello");
        }

        [Fact]
        public async Task After_Succeed_is_called_then_timing_of_subsequent_log_entries_continues()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (var operation = Log.ConfirmOnExit())
            {
                operation.Succeed("success!");
                await Task.Delay(150);
                operation.Info("hello");
            }

            var infoEvent = log
                .Single(e => e.Evaluate().Message == "hello")
                .Operation
                .Duration
                .Value;

            infoEvent.Should()
                     .BeGreaterOrEqualTo(100.Milliseconds());
        }

        [Fact]
        public void After_Fail_is_called_then_a_ConfirmationLogger_does_not_log_on_dispose()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (var operation = Log.ConfirmOnExit())
            {
                operation.Fail(message: "Oops! {0}", args: "bye!");
                operation.Info("hello");
            }

            log.Last().Evaluate().Message.Should().Be("hello");
        }

        [Fact]
        public void ConfirmOnExit_can_capture_args_on_exit()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (Log.ConfirmOnExit(exitArgs: () => new (string, object)[]
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
            using (Log.ConfirmOnExit(exitArgs: () => new (string, object)[]
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
        public void When_exit_args_delegate_throws_then_exception_is_not_thrown_to_the_caller()
        {
            using (Log.ConfirmOnExit(exitArgs: () => throw new Exception("oops!")))
            {
            }
        }

        [Fact]
        public void Fail_uses_exception_as_message_when_no_message_is_specified()
        {
            var log = new LogEntryList();
            var exception = new Exception("bad");
            string exceptionString;

            using (Subscribe(log.Add))
            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                try
                {
                    throw exception;
                }
                catch (Exception e)
                {
                    exceptionString = e.ToString();
                    operation.Fail(e);
                }
            }

            log.Last().Evaluate().Message.Should().Be(exceptionString);
        }

        [Fact]
        public void Child_operations_have_ids_derived_from_their_parent_by_default()
        {
            using var parent = Log.ConfirmOnExit();
            using var child = parent.ConfirmOnExit();
            using var grandchild = child.ConfirmOnExit();

            child.Id.Should().StartWith(parent.Id);
            grandchild.Id.Should().StartWith(parent.Id);
        }

        [Fact]
        public void Child_operations_can_succeed_or_fail_independently_of_parent_operations()
        {
            var log = new LogEntryList();

            using (Subscribe(log.Add))
            using (var parent = Log.OnEnterAndConfirmOnExit())
            {
                for (var index = 0; index < 3; index++)
                {
                    using var child = parent.ConfirmOnExit();

                    child.Fail();
                }

                parent.Succeed();
            }

            var results = log.Select(l => l.Operation.IsSuccessful);

            results.Should().BeEquivalentTo(
                new object[] { null, false, false, false, true });
        }

        [Fact]
        public void ConfirmationLogger_message_appears_in_initial_log_event_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToLogString())))
            using (new ConfirmationLogger(operationName: "Test", message: "hello!", logOnStart: true))
            {
            }

            log.First().Should().Contain("hello!");
        }

        [Fact]
        public void ConfirmationLogger_args_appears_in_initial_log_event_string_output()
        {
            var log = new List<string>();

            using (Subscribe(e => log.Add(e.ToLogString())))
            using (new ConfirmationLogger(operationName: "Test", message: "{one} and {two} and {three}", logOnStart: true, args: new object[] { 1, 2, 3 }))
            {
            }

            log.First().Should().Contain("1 and 2 and 3");
        }
    }
}
