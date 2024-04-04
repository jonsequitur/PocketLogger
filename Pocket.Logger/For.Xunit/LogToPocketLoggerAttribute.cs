#nullable enable

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Xunit.Sdk;

namespace Pocket.For.Xunit;

internal class LogToPocketLoggerAttribute : BeforeAfterTestAttribute
{
    private bool _writeToFile;
    private string? _fileName;
    private string? _fileNameEnvironmentVariable;

    private static readonly ConcurrentDictionary<MethodInfo, TestLog> _operations = new();

    public LogToPocketLoggerAttribute(bool writeToFile = false)
    {
        _writeToFile = writeToFile;
    }

    public LogToPocketLoggerAttribute(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(filename));
        }

        FileName = filename;
        _writeToFile = true;
    }

    public string? FileName
    {
        get => _fileName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null or consist entirely of whitespace");
            }
            _fileName = value;
            _writeToFile = true;
        }
    }

    public string? FileNameEnvironmentVariable
    {
        get => _fileNameEnvironmentVariable;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null or consist entirely of whitespace");
            }

            _fileNameEnvironmentVariable = value;

            if (Environment.GetEnvironmentVariable(value) is { } variableValue && 
                !string.IsNullOrWhiteSpace(variableValue))
            {
                FileName = variableValue;
            }
        }
    }

    public override void Before(MethodInfo methodUnderTest)
    {
        var testLog = new TestLog(
            methodUnderTest,
            _writeToFile,
            FileName);

        TestLog.Current = testLog;

        _operations.TryAdd(
            methodUnderTest,
            testLog);
    }

    public override void After(MethodInfo methodUnderTest)
    {
        if (_operations.TryRemove(methodUnderTest, out var testLog))
        {
            testLog.Dispose();

            if (TestLog.Current == testLog)
            {
                TestLog.Current = default;
            }
        }

        base.After(methodUnderTest);
    }
}