using System;
using System.ComponentModel.DataAnnotations;

public class RequiredIfAttribute : ValidationAttribute
{
    private string _propertyName;
    private object _desiredValue;

    public RequiredIfAttribute(string propertyName, object desiredValue, string errorMessage = "")
    {
        _propertyName = propertyName;
        _desiredValue = desiredValue;
        ErrorMessage = errorMessage;
    }

    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        var instance = context.ObjectInstance;
        var type = instance.GetType();
        var propertyValue = type.GetProperty(_propertyName)?.GetValue(instance, null);

        if (propertyValue != null && propertyValue.Equals(_desiredValue) && string.IsNullOrWhiteSpace(value?.ToString()))
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}