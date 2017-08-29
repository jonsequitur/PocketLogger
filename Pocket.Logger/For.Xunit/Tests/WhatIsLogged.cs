using System.Linq;
using System.Reflection;
using Xunit;
using AssertionExtensions = FluentAssertions.AssertionExtensions;

namespace Pocket.For.Xunit.Tests
{
    public class WhatIsLogged
    {
        [Fact]
        public void Start_events_are_logged_for_each_test()
        {
            var attribute = new LogToPocketLoggerAttribute();

            var methodInfo = GetType().GetMethod(nameof(Start_events_are_logged_for_each_test));

            attribute.Before(methodInfo);

            var log = TestLog.Current.Text;

            attribute.After(methodInfo);

            AssertionExtensions.Should(log.First())
               .Contain($"[{GetType().Name}.{nameof(Start_events_are_logged_for_each_test)}]  ▶");
        }

        [Fact]
        public void Stop_events_are_logged_for_each_test()
        {
            var attribute = new LogToPocketLoggerAttribute();

            var methodInfo = GetType().GetMethod(nameof(Stop_events_are_logged_for_each_test));

            attribute.Before(methodInfo);

            var log = TestLog.Current.Text;

            attribute.After(methodInfo);

            AssertionExtensions.Should(log.Last())
               .Match($"*[{GetType().Name}.{nameof(Stop_events_are_logged_for_each_test)}]  ⏹ (*ms)*");
        }
    }
}