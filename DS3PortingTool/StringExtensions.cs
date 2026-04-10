namespace DS3PortingTool;

public static class StringExtensions
{
    public static bool IsDigits(this string s)
    {
        return s.All(c => c is >= '0' and <= '9');
    }
}