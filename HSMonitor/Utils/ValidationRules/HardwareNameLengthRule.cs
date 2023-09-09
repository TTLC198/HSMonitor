using System;
using System.Globalization;
using System.Windows.Controls;

namespace HSMonitor.Utils.ValidationRules;

public class HardwareNameLengthRule: ValidationRule
{
    public int Max { get; set; }
    
    public int Min { get; set; }

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        try
        {
            if (((string)value).Length > Max)
                return new ValidationResult(false,
                    $"Please enter a string less than {Max}.");
            
            if (((string)value).Length < Min)
                return new ValidationResult(false,
                    $"Please enter a string more than {Min}.");
        }
        catch (Exception e)
        {
            return new ValidationResult(false, $"Illegal characters or {e.Message}");
        }

        return ValidationResult.ValidResult;
    }
}