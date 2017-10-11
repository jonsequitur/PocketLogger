using System;
using FluentAssertions;
using System.Linq;
using Microsoft.Extensions.Logging;
using Pocket.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Pocket.For.MicrosoftExtensionsLogging.Tests
{
    public class When_publishing_from_ILogger_to_PocketLogger : IDisposable
    {
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private readonly LogEntryList log = new LogEntryList();
        private readonly ILoggerFactory loggerFactory;

        public When_publishing_from_ILogger_to_PocketLogger(ITestOutputHelper output)
        {
            loggerFactory = new LoggerFactory()
                .AddConsole()
                .AddPocketLogger();

            disposables.Add(LogEvents.Subscribe(e => output.WriteLine(e.ToLogString())));
            disposables.Add(LogEvents.Subscribe(e => log.Add(e)));
        }

        public void Dispose() => disposables.Dispose();

        [Fact]
        public void The_message_is_not_included_in_properties()
        {
            loggerFactory.CreateLogger("the-category").LogInformation("hi!");

            var evaluated = log.Single().Evaluate();

            evaluated.Message
                     .Should()
                     .Be("hi!");
            evaluated.Properties
                     .Should()
                     .HaveCount(0);
        }

        [Fact]
        public void LogInformation_logs_at_the_correct_log_level()
        {
            var logger = loggerFactory.CreateLogger("the-category");

            logger.LogInformation("This is info with no args");

            log.Single()
               .LogLevel
               .Should().Be((int) LogLevel.Information);
        }

        [Fact]
        public void LogWarning_logs_at_the_correct_log_level()
        {
            var logger = loggerFactory.CreateLogger("the-category");

            logger.LogWarning("uh oh...");

            log.Single()
               .LogLevel
               .Should()
               .Be((int) LogLevel.Warning);
        }

        [Fact]
        public void LogError_logs_at_the_correct_log_level()
        {
            var logger = loggerFactory.CreateLogger("the-category");

            logger.LogError("uh oh...");

            log.Single()
               .LogLevel
               .Should()
               .Be((int) LogLevel.Error);
        }

        [Fact]
        public void The_category_is_set_correctly_when_specified_using_a_string()
        {
            var logger = loggerFactory.CreateLogger("the-category");

            logger.LogInformation("This is info with no args");

            log.Single()
               .Category
               .Should()
               .Be("the-category");
        }

        [Fact]
        public void The_category_is_set_correctly_when_specified_using_a_Type()
        {
            var logger = loggerFactory.CreateLogger<When_publishing_from_ILogger_to_PocketLogger>();

            logger.LogInformation("This is info with no args");

            log.Single().Category
               .Should()
               .Be(typeof(When_publishing_from_ILogger_to_PocketLogger).ToString());
        }

        [Fact]
        public void Args_are_available()
        {
            var logger = loggerFactory.CreateLogger("the-category");

            logger.LogInformation("This is info with some args: {an-int} and {a-string}", 123, "hello");

            var properties = log.Single().Evaluate().Properties;

            properties
                .Should()
                .Contain(("an-int", 123));
            properties
                .Should()
                .Contain(("a-string", "hello"));
        }

        [Fact]
        public void Scopes_are_treated_as_operations()
        {
            var logger = loggerFactory.CreateLogger("the-category");

            using (logger.BeginScope("the-scope"))
            {
                logger.LogInformation("This is info in a scope");
            }

            log[0].ToLogString().Should().Contain("▶");
            log[0].ToLogString().Should().Contain("[the-scope]");

            log[1].ToLogString().Should().Contain("This is info in a scope");

            log[2].ToLogString().Should().Contain("⏹");
            log[2].ToLogString().Should().Contain("the-scope");
        }
    }
}
