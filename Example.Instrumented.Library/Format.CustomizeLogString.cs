namespace Pocket;

internal partial class Format
{
    static partial void CustomizeLogString(object value, ref string output)
    {
        if (value?.Equals("replace me with empty string") == true)
        {
            output = "";
        }
        else
        {
            output = value + " (custom)";
        }
    }
}