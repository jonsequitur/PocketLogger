using FluentAssertions;
using Xunit;

namespace Pocket.For.Xunit.Tests;

[LogToPocketLogger(writeToFile: true)]
public class HowAreLogsAccessed
{
    [Fact]
    public void Log_entries_written_to_PocketLogger_are_captured_by_the_test_log()
    {
        Logger.Log.Info("hi!");

        LogToPocketLoggerAttribute.CurrentFileLog
               .Lines
               .Should()
               .Contain(line => line.Contains("hi!"));
    }
}