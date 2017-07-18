using System;
using Pocket;

namespace Example.Instrumented.Library
{
    public class Class1
    {
        public static void EmitSomeLogEvents(string parameter1 = null)
        {
            using (var section = Log.OnEnterAndExit())
            {
                section.Info($"{nameof(parameter1)} = {{{nameof(parameter1)}}}", parameter1);
            }
        }
    }
}
