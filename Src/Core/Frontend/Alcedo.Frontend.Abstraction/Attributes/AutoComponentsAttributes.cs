namespace Alcedo.Frontend.Abstraction.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class AutoFormIgnoreRenderAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property)]
public sealed class AutoRenderLabelAttribute(string label) : Attribute
{
    public string Label { get; } = label;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class AutoFormRenderHelperAttribute(string helper) : Attribute
{
    public string Helper { get; } = helper;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class AutoFormRenderReadOnlyAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property)]
public sealed class AutoFormRenderTextAreaAttribute(int lines = 1, int maxLines = 6) : Attribute
{
    public int Lines { get; } = lines;
    
    public int? MaxLines { get; } = maxLines;
}

public sealed class AutoFormNumericStepAttribute(string step) : Attribute
{
    public string Step { get; } = step;
}

[AttributeUsage(AttributeTargets.Property)]
public class AutoFormCellWidthAttribute(int width) : Attribute
{
    public int Width { get; } = width;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class AutoTableIgnoreRenderAttribute() : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AutoTableLinkAttribute(string propName, string template) : Attribute
{
    public string PropName { get; } = propName;
    public string Template { get; } = template;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class AutoSearchSortFieldAttribute() : Attribute
{
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class AutoComponentValueAtomOptionsAttribute(Type optionsType) : Attribute
{
    public Type OptionsType { get; } = optionsType;
}

// Alcedo.Frontend.Abstraction/Attributes/Validation/

[AttributeUsage(AttributeTargets.Property)]
public class AutoValidateIgnoreAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property)]
public class AutoValidateRequiredAttribute : Attribute
{
    public string ErrorMessage { get; }

    public AutoValidateRequiredAttribute(string errorMessage = "This field is required.")
        => ErrorMessage = errorMessage;
}

[AttributeUsage(AttributeTargets.Property)]
public class AutoValidateStringRequiredAttribute : Attribute
{
    public string ErrorMessage { get; }

    public AutoValidateStringRequiredAttribute(string errorMessage = "This field cannot be empty.")
        => ErrorMessage = errorMessage;
}

[AttributeUsage(AttributeTargets.Property)]
public class AutoValidateEmailAttribute : Attribute
{
    public string ErrorMessage { get; }

    public AutoValidateEmailAttribute(string errorMessage = "Invalid email address.")
        => ErrorMessage = errorMessage;
}

[AttributeUsage(AttributeTargets.Property)]
public class AutoValidatePhoneAttribute : Attribute
{
    public string ErrorMessage { get; }

    public AutoValidatePhoneAttribute(
        string errorMessage = "Invalid phone number. Expected format: +<country><number>.")
        => ErrorMessage = errorMessage;
}

[AttributeUsage(AttributeTargets.Property)]
public class AutoValidateRangeAttribute : Attribute
{
    public double Min { get; }
    public double Max { get; }
    public string ErrorMessage { get; }

    public AutoValidateRangeAttribute(double min, double max, string errorMessage = "")
    {
        Min = min;
        Max = max;
        ErrorMessage = string.IsNullOrEmpty(errorMessage)
            ? $"Value must be between {min} and {max}."
            : errorMessage;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class AutoValidateStringLengthAttribute : Attribute
{
    public int Min { get; }
    public int Max { get; }
    public string ErrorMessage { get; }

    public AutoValidateStringLengthAttribute(int min, int max, string errorMessage = "")
    {
        Min = min;
        Max = max;
        ErrorMessage = string.IsNullOrEmpty(errorMessage)
            ? $"Length must be between {min} and {max} characters."
            : errorMessage;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class AutoValidateCustomAttribute : Attribute
{
    public Type CheckerType { get; }
    public string MethodName { get; }
    public string ErrorMessage { get; }

    public AutoValidateCustomAttribute(Type checkerType, string methodName, string errorMessage = "")
    {
        CheckerType = checkerType;
        MethodName = methodName;
        ErrorMessage = errorMessage;
    }
}