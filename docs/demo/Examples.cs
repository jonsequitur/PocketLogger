using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Pocket;

#region UsefulUsings

using static Pocket.Logger<Demo.Examples>;

#endregion

namespace Demo
{
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

        private static async Task Method2(ConfirmationLogger operation)
        {
            using var childOperation = operation.OnEnterAndConfirmOnExit();

            childOperation.Succeed();
        }

        #endregion

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
}