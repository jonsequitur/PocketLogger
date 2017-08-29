using System;
using FluentAssertions;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Pocket.For.Xunit.Tests
{
    [LogToPocketLogger]
    public class HowAreLogsAccessed : IDisposable
    {
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public HowAreLogsAccessed(ITestOutputHelper output)
        {
            LogEvents.Subscribe(e => output.WriteLine(e.ToLogString()));
        }

        public void Dispose() => disposables.Dispose();

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
    }
}
