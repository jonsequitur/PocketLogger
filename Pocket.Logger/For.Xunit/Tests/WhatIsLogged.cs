using System.Linq;
using Example.Instrumented.Library;
using FluentAssertions;
using Xunit;

namespace Pocket.For.Xunit.Tests;

public class WhatIsLogged
{
    [Fact]
    public void Start_events_are_logged_for_each_test()
    {
        var attribute = new LogToPocketLoggerAttribute(true);

        var methodInfo = GetType().GetMethod(nameof(Start_events_are_logged_for_each_test))!;

        attribute.Before(methodInfo);

        var log = LogToPocketLoggerAttribute.CurrentFileLog!.Lines;

        attribute.After(methodInfo);

        log.First()
           .Should()
           .Contain($"[🧪:{GetType().Name}.{nameof(Start_events_are_logged_for_each_test)}]  ▶");
    }

    [Fact]
    public void Stop_events_are_logged_for_each_test()
    {
        var attribute = new LogToPocketLoggerAttribute(true);

        var methodInfo = GetType().GetMethod(nameof(Stop_events_are_logged_for_each_test))!;

        attribute.Before(methodInfo);

        var log = LogToPocketLoggerAttribute.CurrentFileLog!;

        attribute.After(methodInfo);

        log.Lines
           .Last()
           .Should()
           .Match($"*[🧪:{GetType().Name}.{nameof(Stop_events_are_logged_for_each_test)}]  ⏹ (*ms)*");
    }

    [Fact]
    public void Events_from_called_assemblies_can_be_logged()
    {
        var attribute = new LogToPocketLoggerAttribute(true)
        {
            SubscribeAssembliesContainingTypes = [ typeof(Class1) ]
        };

        var methodInfo = GetType().GetMethod(nameof(Stop_events_are_logged_for_each_test))!;

        attribute.Before(methodInfo);

        var log = LogToPocketLoggerAttribute.CurrentFileLog!;

        Class1.EmitSomeLogEvents("hello");

        attribute.After(methodInfo);

        log.Lines
           .Should()
           .Contain(line => line.Contains("hello"));
    }
}