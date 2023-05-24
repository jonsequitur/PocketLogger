namespace Pocket;

internal partial class Format
{
    static partial void CustomizeLogString(object value, ref string output)
    {
        output = value + " (custom)";
    }
}