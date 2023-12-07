namespace HSMonitor.Utils;

public static class NumberExtensions
{
    public static float GetFloatAsCorrectNumber(this float? value) 
        => float.IsNaN(value ?? 0) ? 0 : value ?? 0;
}