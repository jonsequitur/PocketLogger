using System;
using System.Threading.Tasks;
using Pocket;

namespace Demo;

static class Program
{
    static async Task<int> Main(
        string session = null,
        string region = null,
        string project = null,
        string package = null,
        string[] args = null)
    {
        ConsoleSubscription =
            #region LoggingToTheConsole
            LogEvents.Subscribe(e => Console.WriteLine(e.ToLogString()));
        #endregion

        return region switch
        {
            "HelloPocketLogger" => Call(Examples.HelloPocketLogger),

            "LogInfo" => Call(Examples.LogInfo),

            "LogInfoWithNamedTemplateParams" => Call(Examples.LogInfoWithNamedTemplateParams),

            "LogEvent" => Call(Examples.LogEvent),

            "LogWarning" => Call(Examples.LogWarning),

            "LogError" => Call(Examples.LogError),

            "LogOnExit" => Call(Examples.LogOnExit),

            "LogOnEnterAndExit" => Call(Examples.LogOnEnterAndExit),

            "LogConfirmOnExit" => Call(Examples.LogConfirmOnExit),

            "LogConfirmOnExitSucceed" => Call(Examples.LogConfirmOnExitSucceed),

            "LogConfirmOnExitFail" => Call(Examples.LogConfirmOnExitFail),

            "LogOnEnterAndConfirmOnExit" => Call(Examples.LogOnEnterAndConfirmOnExit),

            "LogApiStaticClass" => Call(StaticClassExample.Hello),

            "Checkpoints" => await Call(Examples.Checkpoints),

            "ExitArgs" => Call(Examples.ExitArgs),

            "ChildOperations" => await Call(Examples.Method1),

            "Enrich" => Call(Examples.Enrich),

            "SubscribeAndSendToConsole" => Call(Examples.SubscribeAndSendToConsole),

            "LogEventStructure" => Call(Examples.LogEventStructure),
                
            "Evaluate" => Call(Examples.Evaluate),

            "SubscribeAndSendToSerilog" => Call(Examples.SubscribeAndSendToSerilog),

            _ =>
                throw new ArgumentException($"There's no case in Program.Main for {nameof(region)} '{region}'")
        };
    }

    public static LoggerSubscription ConsoleSubscription { get; private set; }

    private static int Call(Action action)
    {
        action();
        return 0;
    }

    private static async Task<int> Call(Func<Task> action)
    {
        await action();
        return 0;
    }
}