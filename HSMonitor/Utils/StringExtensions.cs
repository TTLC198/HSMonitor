using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HSMonitor.Utils;

public static class StringExtensions
{
    // https://stackoverflow.com/questions/4488969/split-a-string-by-capital-letters
    public static IEnumerable<string> SplitByCapitalLettersConvention(this string source)
    {
        return Regex.Split(source, @"(?<!^)(?=[A-Z])|(?=[0-9])");
    }
}