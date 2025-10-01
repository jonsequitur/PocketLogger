#nullable enable

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using Xunit.Sdk;

namespace Pocket.For.Xunit;

internal class LogToPocketLoggerAttribute : BeforeAfterTestAttribute
{
    private bool _writeToFile;
    private string? _fileName;
    private string? _fileNameEnvironmentVariable;
    private Type[]? _subscribeAssembliesContainingTypes;
    private Assembly[]? _subscribeAssemblies;

    private static readonly ConcurrentDictionary<MethodInfo, (OperationLogger operation, FileLog? fileLog)> _operations = new();
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

    public Type[]? SubscribeAssembliesContainingTypes
    {
        get => _subscribeAssembliesContainingTypes;
        set
        {
            _subscribeAssembliesContainingTypes = value;

            if (value is not null)
            {
                _subscribeAssemblies = [..value.Select(t => t.Assembly).Distinct()];
            }
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

            if (_subscribeAssemblies is {} assemblies)
            {
                testLog.Subscribe(assemblies);
            }

            CurrentFileLog = testLog;
        }

        var operation = new OperationLogger($"🧪:{operationName}", logOnStart: true);

        _operations.TryAdd(
            methodUnderTest,
            (operation, CurrentFileLog));
    }

    public override void After(MethodInfo methodUnderTest)
    {
        if (_operations.TryRemove(methodUnderTest, out var tuple))
        {
            var (operation, fileLog) = tuple;
            operation.Dispose();
            fileLog?.Dispose();

            if (CurrentOperation == operation)
            {
                CurrentFileLog = null;
                CurrentOperation = null;
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