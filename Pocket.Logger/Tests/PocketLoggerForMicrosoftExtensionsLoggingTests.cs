using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using static Pocket.Logger;

namespace Pocket.For.MicrosoftExtensionsLogging.Tests
{
    public class PocketLoggerForMicrosoftExtensionsLoggingTests : IDisposable
    {
        private readonly ITestOutputHelper output;

        private readonly IDisposable disposables = new CompositeDisposable();

        public void Dispose()
        {
            disposables.Dispose();
        }

        [Fact(Skip = "explicit")]
        public async Task EXAMPLE()
        {
            var factory = new LoggerFactory()
                .AddConsole()
                .AddDebug();

            var logger = factory.CreateLogger<PocketLoggerForMicrosoftExtensionsLoggingTests>();
            var someInt = 5;
            var someDate = DateTimeOffset.Now;

            logger.LogInformation("before the scope block with {someInt} and {someDate}", someInt, someDate);

            using (logger.BeginScope(Disposable.Create(() => { })))
            {
                logger.LogInformation("inside the scope block");

                await Task.Delay(1000);

                logger.LogInformation("inside the scope block again");
            }

            Assert.True(false, "Test EXAMPLE is not written yet.");
        }

        [Fact]
        public void Pocket_Logger_entries_can_be_published_to_Microsoft_Extensions_Logging()
        {
            var log = new List<Microsoft.Extensions.Logging.LogLevel>();

            var logger = new LoggerFactory()
                .AddConsole()
                .Add((level, id, state, exception, formatter) =>
                         log.Add(level))
                .CreateLogger("the-category");

            using (logger.SubscribeToPocket())
            {
                Log.Info("hi!");
                Log.Warning("uh oh...");
                Log.Error("oops!");
            }

            log.Should()
               .BeEquivalentTo(
                   Microsoft.Extensions.Logging.LogLevel.Information,
                   Microsoft.Extensions.Logging.LogLevel.Warning,
                   Microsoft.Extensions.Logging.LogLevel.Error
               );
        }

        [Fact]
        public void Categories_are_included()
        {
            var log = new List<string>();

            var logger = new LoggerFactory()
                .AddConsole()
                .Add((level, id, state, exception, formatter) =>
                         log.Add(formatter(state, exception)))
                .CreateLogger("the-category");

            using (logger.SubscribeToPocket())
            {
                new Logger<DeclarativeSecurityAction>().Info("hello");
            }

            log.Should().Contain(e => e.Contains(nameof(DeclarativeSecurityAction)));
        }

        [Fact]
        public void Arguments_are_included()
        {
            var log = new List<IReadOnlyCollection<KeyValuePair<string, object>>>();

            var logger = new LoggerFactory()
                .AddConsole()
                .Add((level, id, state, exception, formatter) =>
                         log.Add((IReadOnlyCollection<KeyValuePair<string, object>>) state))
                .CreateLogger("the-category");

            using (logger.SubscribeToPocket())
            {
                Log.Error("THIS IS DEFINITELY A {WHAT}!",
                          new Exception("oops!"),
                          "BIG PROBLEM");
            }

            log.Should()
               .ContainSingle(e => e.Any(
                                  _ => _.Key == "WHAT" &&
                                       _.Value == "BIG PROBLEM"));
        }
    }
}
