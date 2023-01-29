using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace HSMonitor.Utils.ValidationRules;

public class IntValueRule: ValidationRule
{
    public int Min { get; set; }
    public int Max { get; set; }

    public IntValueRule()
    {
    }
    
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        try
        {
            if (!int.TryParse((string)value, out var num))
                return new ValidationResult(false,
                    $"Please enter a valid number.");
            if (num < Min)
                return new ValidationResult(false,
                    $"Please enter a value above {Min}.");
            if (num > Max)
                return new ValidationResult(false,
                    $"Please enter a value below {Max}.");
        }
        catch (Exception e)
        {
            return new ValidationResult(false, $"Illegal characters or {e.Message}");
        }

        return ValidationResult.ValidResult;
    }
}