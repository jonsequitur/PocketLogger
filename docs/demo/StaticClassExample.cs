using Pocket;

namespace Demo;

#region LogApiStaticClass

public static class StaticClassExample
{
    private static readonly Logger Log = new Logger(nameof(StaticClassExample));

    public static void Hello()
    {
        Log.Info("Hello!");
    }
}

#endregion