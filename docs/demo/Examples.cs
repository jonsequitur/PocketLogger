using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Pocket;
using Serilog.Sinks.RollingFileAlternate;
using LoggerConfiguration = Serilog.LoggerConfiguration;

#region UsefulUsings
using static Pocket.Logger<Demo.Examples>;
#endregion

namespace Demo;

public class Examples
{
    static Examples()
    {
    }

    public static void HelloPocketLogger()
    {
        #region HelloPocketLogger

        Log.Info("Hello!");
        Log.Event("LoudNoise");
        Log.Warning("That's strange...");
        Log.Error("Oh no! Run!");

        #endregion
    }

    public static void LogInfo()
    {
        #region LogInfo

        var x = "world";
        var y = DateTime.Now;

        Log.Info("Hello", x, y);

        #endregion
    }

    public static void LogInfoWithNamedTemplateParams()
    {
        #region LogInfoWithNamedTemplateParams

        var x = "world";
        var y = DateTime.Now;

        Log.Info("Hello {x}! The time is {y}!", x, y);

        #endregion
    }

    public static void LogEvent()
    {
        #region LogEvent

        Log.Event("LoudNoise", ("loudness", 9000), ("nearness", 1.5));

        #endregion
    }

    public static void LogWarning()
    {
        #region LogWarning

        Log.Warning("That's strange...", new DataMisalignedException());

        #endregion
    }

    public static void LogError()
    {
        #region LogError

        Log.Error("Oh no! Run!", new BarrierPostPhaseException());

        #endregion
    }

    public static void LogOnExit()
    {
        #region LogOnExit

        using (var _ = Log.OnExit())
        {
        }

        #endregion
    }

    public static void LogOnEnterAndExit()
    {
        #region LogOnEnterAndExit

        using (var _ = Log.OnEnterAndExit())
        {
        }

        #endregion
    }

    public static void LogConfirmOnExit()
    {
        #region LogConfirmOnExit

        using (var operation = Log.ConfirmOnExit())
        {
        }

        #endregion
    }

    public static void LogConfirmOnExitSucceed()
    {
        #region LogConfirmOnExitSucceed

        using (var operation = Log.ConfirmOnExit())
        {
            operation.Succeed();
        }

        #endregion
    }

    public static void LogConfirmOnExitFail()
    {
        #region LogConfirmOnExitFail

        using (var operation = Log.ConfirmOnExit())
        {
            operation.Fail();
        }

        #endregion
    }

    public static void LogOnEnterAndConfirmOnExit()
    {
        #region LogOnEnterAndConfirmOnExit

        using (var operation = Log.OnEnterAndConfirmOnExit())
        {
            operation.Succeed();
        }

        #endregion
    }

    public static async Task Checkpoints()
    {
        #region Checkpoints

        using (var operation = Log.OnEnterAndConfirmOnExit())
        {
            var apiResult = await CallSomeApiAsync();

            operation.Event("CalledSomeApi", ("ResultCode", apiResult));

            operation.Succeed("We did it!");
        }

        #endregion
    }

    public static void ExitArgs()
    {
        #region ExitArgs

        var myVariable = "initial value";

        using (var operation = Log.OnEnterAndConfirmOnExit(
                   exitArgs: () => new[] { (nameof(myVariable), (object) myVariable) }))
        {
            operation.Info("", (nameof(myVariable), myVariable));

            myVariable = "new value";

            operation.Succeed("Yes!");
        }

        #endregion
    }

    #region ChildOperations

    public static async Task Method1()
    {
        using var operation = Log.OnEnterAndConfirmOnExit();

        await Method2(operation);

        operation.Succeed();
    }

    private static Task Method2(ConfirmationLogger operation)
    {
        using var childOperation = operation.OnEnterAndConfirmOnExit();

        childOperation.Succeed();

        return Task.CompletedTask;
    }

    #endregion

    public static void SubscribeAndSendToConsole()
    {
        Program.ConsoleSubscription.Dispose();

        #region SubscribeAndSendToConsole

        Log.Info("Before subscribing.");

        var subscription = 
            LogEvents.Subscribe(e => Console.WriteLine(e.ToLogString()));

        Log.Info("After subscribing.");

        subscription.Dispose();

        Log.Info("After disposing the subscription.");

        #endregion
    }

    public static void LogEventStructure()
    {
        Program.ConsoleSubscription.Dispose();

        #region LogEventStructure

        using var subscription =
            LogEvents.Subscribe(e =>
            {
                Console.WriteLine(e.ToLogString());
                Console.WriteLine($"    {nameof(e.TimestampUtc)}: {e.TimestampUtc}");
                Console.WriteLine($"    {nameof(e.Operation.Id)}: {e.Operation.Id}");
                Console.WriteLine($"    {nameof(e.Category)}: {e.Category}");
                Console.WriteLine($"    {nameof(e.OperationName)}: {e.OperationName}");
                Console.WriteLine($"    {nameof(e.LogLevel)}: {e.LogLevel}");
                Console.WriteLine($"    {nameof(e.Operation.IsStart)}: {e.Operation.IsStart}");
                Console.WriteLine($"    {nameof(e.Operation.IsEnd)}: {e.Operation.IsEnd}");
                Console.WriteLine($"    {nameof(e.Operation.Duration)}: {e.Operation.Duration}");
                Console.WriteLine($"    {nameof(e.Operation.IsSuccessful)}: {e.Operation.IsSuccessful}");
            });

        Log.Info("INFO");

        using var operation = Log.OnEnterAndConfirmOnExit();

        operation.Event("EVENT");

        operation.Succeed();

        #endregion
    }

    public static void Evaluate()
    {
        Program.ConsoleSubscription.Dispose();

        #region Evaluate

        using var subscription =
            LogEvents.Subscribe(e =>
            {
                var evaluated = e.Evaluate();

                Console.WriteLine(evaluated.Message);

                foreach (var property in evaluated.Properties)
                {
                    Console.WriteLine($"    {property.Name}: {property.Value}");
                }
            });

        Log.Event("EventWithProperties", ("a number", 1), ("a date", DateTime.Now));

        Log.Event("EventWithMetrics", ("one", 1d), ("pi, more or less", 3.14));

        #endregion
    }

    public static void SubscribeAndSendToSerilog()
    {
        Program.ConsoleSubscription.Dispose();

        #region SubscribeAndSendToSerilog
            
        using var serilogLogger = new LoggerConfiguration()
                                  .WriteTo
                                  .RollingFileAlternate(".", outputTemplate: "{Message}{NewLine}")
                                  .CreateLogger();

        using (var subscription = LogEvents.Subscribe(
                   e => serilogLogger.Information(e.ToLogString())))
        {
            Log.Info("After subscribing.");
        }

        var logFile = new DirectoryInfo(".")
                      .GetFiles()
                      .OrderBy(f => f.LastWriteTime)
                      .Last();

        Console.WriteLine(File.ReadAllText(logFile.FullName));

        #endregion
    }

    public static void Enrich()
    {
        #region Enrich

        LogEvents.Enrich(add =>
        {
            add(("app_version", Assembly.GetExecutingAssembly().GetName().Version));
            add(("machine_name", Environment.MachineName));
        });

        Log.Info("Hello!");

        #endregion
    }

    private static async Task<int> CallSomeApiAsync()
    {
        await Task.Delay(100);
        return 42;
    }
}