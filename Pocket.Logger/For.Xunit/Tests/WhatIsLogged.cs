using FluentAssertions;
using System.Linq;
using Xunit;

namespace Pocket.For.Xunit.Tests;

public class WhatIsLogged
{
    [Fact]
    public void Start_events_are_logged_for_each_test()
    {
        var attribute = new LogToPocketLoggerAttribute(true);

        var methodInfo = GetType().GetMethod(nameof(Start_events_are_logged_for_each_test));

        attribute.Before(methodInfo);

        var log = LogToPocketLoggerAttribute.CurrentFileLog.Lines;

        attribute.After(methodInfo);

        log.First()
           .Should()
           .Contain($"[🧪:{GetType().Name}.{nameof(Start_events_are_logged_for_each_test)}]  ▶");
    }

    [Fact]
    public void Stop_events_are_logged_for_each_test()
    {
        var attribute = new LogToPocketLoggerAttribute(true);

        var methodInfo = GetType().GetMethod(nameof(Stop_events_are_logged_for_each_test));

        attribute.Before(methodInfo);

        var log = LogToPocketLoggerAttribute.CurrentFileLog;

        attribute.After(methodInfo);

        log.Lines
           .Last()
           .Should()
           .Match($"*[🧪:{GetType().Name}.{nameof(Stop_events_are_logged_for_each_test)}]  ⏹ (*ms)*");
    }
}