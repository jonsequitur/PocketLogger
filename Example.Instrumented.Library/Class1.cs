using Pocket;
using static Pocket.Logger;

namespace Example.Instrumented.Library
{
    public class Class1
    {
        public static void EmitSomeLogEvents(string parameter1 = null)
        {
            using var operation = Log.OnEnterAndExit();

            operation.Info($"{nameof(parameter1)} = {{{nameof(parameter1)}}}", parameter1);
        }
    }
}