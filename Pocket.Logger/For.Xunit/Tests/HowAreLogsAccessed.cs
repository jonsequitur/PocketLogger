using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using static Pocket.Logger<Pocket.For.Xunit.Tests.HowAreLogsAccessed>;

namespace Pocket.For.Xunit.Tests
{
    [LogToPocketLogger]
    public class HowAreLogsAccessed
    {
        [Fact]
        public void Log_entries_written_to_PocketLogger_are_captured_by_the_test_log()
        {
            Logger.Log.Info("hi!");

            TestLog.Current
                   .Text
                   .Should()
                   .Contain(line => line.Contains("hi!"));
        }

        [Fact]
        public void The_test_can_write_directly_to_the_test_log()
        {
            TestLog.Current.Log.Info("hello!");

            TestLog.Current
                   .Text
                   .Should()
                   .Contain(line => line.Contains("hello!"));
        }

        [Fact]
        public void Logs_can_be_written_to_ITestOutputHelper()
        {
            var output = new TestTestOutputHelper();

            TestLog.Current.LogTo(output);

            var message = Guid.NewGuid().ToString();

            Log.Info(message);

            output.Text.Should().Contain(s => s.Contains(message));
        }

        private class TestTestOutputHelper : ITestOutputHelper
        {
            private readonly ConcurrentQueue<string> text = new();

            public void WriteLine(string message)
            {
                text.Enqueue(message);
            }

            public void WriteLine(string format, params object[] args) => throw new NotImplementedException();

            public IEnumerable<string> Text => text;
        }
    }
}
