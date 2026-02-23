using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace HSMonitor.Utils.ValidationRules;

[ContentProperty("RuleValue")]
public class HardwareNameLengthRule: ValidationRule
{
    public HardwareNameLengthRuleValue RuleValue { get; set; }

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        try
        {
            if (((string)value).Length > RuleValue.Max)
                return new ValidationResult(false,
                    $"Please enter a string less than {RuleValue.Max}."); //todo localization
            
            if (((string)value).Length < RuleValue.Min)
                return new ValidationResult(false,
                    $"Please enter a string more than {RuleValue.Min}.");
        }
        catch (Exception e)
        {
            return new ValidationResult(false, $"Illegal characters or {e.Message}");
        }

        return ValidationResult.ValidResult;
    }
}