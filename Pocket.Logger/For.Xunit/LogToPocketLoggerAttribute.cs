#nullable enable

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using Xunit.Sdk;

namespace Pocket.For.Xunit;

internal class LogToPocketLoggerAttribute : BeforeAfterTestAttribute
{
    private bool _writeToFile;
    private string? _fileName;
    private string? _fileNameEnvironmentVariable;

    private static readonly ConcurrentDictionary<MethodInfo, OperationLogger> _operations = new();
    private static readonly AsyncLocal<FileLog> _currentFileLog = new();
    private static readonly AsyncLocal<OperationLogger> _currentOperation = new();

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
        var operationName = $"{methodUnderTest.DeclaringType?.Name}.{methodUnderTest.Name}";

        if (_writeToFile)
        {
            var testLog = new FileLog(FileName ?? $"{operationName}-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.log");
            CurrentFileLog = testLog;
        }

        var operation = new OperationLogger($"🧪:{operationName}", logOnStart: true);

        _operations.TryAdd(
            methodUnderTest,
            operation);
    }

    public override void After(MethodInfo methodUnderTest)
    {
        if (_operations.TryRemove(methodUnderTest, out var operation))
        {
            operation.Dispose();

            if (CurrentOperation == operation)
            {
                CurrentFileLog = default;
                CurrentOperation = default;
            }
        }

        base.After(methodUnderTest);
    }

    public static FileLog? CurrentFileLog
    {
        get => _currentFileLog.Value;
        set => _currentFileLog.Value = value!;
    }

    public static OperationLogger? CurrentOperation
    {
        get => _currentOperation.Value;
        set => _currentOperation.Value = value!;
    }
}