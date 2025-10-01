using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Pocket.For.Xunit.Tests;

[LogToPocketLogger(FileNameEnvironmentVariable = $"{nameof(PerTestSubscriptionsAreDisposed)}_LOG_FILE")]
public class PerTestSubscriptionsAreDisposed(ITestOutputHelper output)
{
    private FileInfo? _logFile;

    static PerTestSubscriptionsAreDisposed()
    {
        Environment.SetEnvironmentVariable(
            $"{nameof(PerTestSubscriptionsAreDisposed)}_LOG_FILE",
            $"{nameof(PerTestSubscriptionsAreDisposed)}-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.log");
    }

    private async Task AssertSingleOccurrenceOfStringInLogFile([CallerMemberName] string? methodName = null)
    {
        _logFile = LogToPocketLoggerAttribute.CurrentFileLog?.File;

        if (_logFile is not null)
        {
            output.WriteLine($"Log file is: {_logFile.FullName}");

            await Task.Delay(20);

            var text = await File.ReadAllTextAsync(_logFile.FullName);

            if (methodName is not null &&
                CountSubstring(text, $"{methodName}]  ▶") > 1)
            {
                throw new AssertionFailedException(
                    $"""
                     {methodName} was logged more than once:

                     {text}
                     """);
            }
        }
    }

    [Fact]
    public async Task test1()
    {
        await AssertSingleOccurrenceOfStringInLogFile();
    }

    [Fact]
    public async Task test2()
    {
        await AssertSingleOccurrenceOfStringInLogFile();
    }

    [Fact]
    public async Task test3()
    {
        await AssertSingleOccurrenceOfStringInLogFile();
    }

    [Fact]
    public async Task test4()
    {
        await AssertSingleOccurrenceOfStringInLogFile();
    }

    private int CountSubstring(string text, string substring)
    {
        int count = 0;
        int index = 0;

        while ((index = text.IndexOf(substring, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += substring.Length;
        }

        return count;
    }
}