namespace Pocket;

internal partial class Format
{
    static partial void CustomizeLogString(object value, ref string output)
    {
        if (value is string stringValue &&
            stringValue.StartsWith("customize me:"))
        {
            stringValue = stringValue.Replace("customize me:","");

            if (stringValue.Equals("replace me with empty string"))
            {
                output = "";
            }
            else
            {
                output = stringValue + " (custom)";
            }
        }
    }
}