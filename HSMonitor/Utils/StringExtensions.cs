using static System.Text.RegularExpressions.Regex;

namespace HSMonitor.Utils;

public static class StringExtensions
{
    // https://stackoverflow.com/questions/4488969/split-a-string-by-capital-letters
    public static IEnumerable<string> SplitByCapitalLettersConvention(this string source)
    {
        return Split(source, @"(?<!^)(?=[A-Z])|(?=[0-9])");
    }
}