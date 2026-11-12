using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Alcedo.Core.Presentation.Validation;

/// <summary>
/// Validates the precision and scale of a decimal property.
/// If omitted, the default validation convention (Precision: 18, Scale: 6) will be applied.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValuePrecisionAttribute : ValidationAttribute
{
    public int Precision { get; }
    public int Scale { get; }

    public ValuePrecisionAttribute(int precision, int scale)
    {
        Precision = precision;
        Scale = scale;
    }

    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext context)
    {
        if (value is not decimal number)
            return ValidationResult.Success;

        var text = Math.Abs(number)
            .ToString(CultureInfo.InvariantCulture);

        var parts = text.Split('.');

        var integerLength = parts[0].Length;
        var scaleLength = parts.Length > 1 
            ? parts[1].Length 
            : 0;

        if (integerLength + scaleLength > Precision ||
            scaleLength > Scale)
        {
            return new ValidationResult(
                $"{context.DisplayName} precision exceeds ({Precision},{Scale}).");
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Specifies that a decimal property represents a monetary value with a default precision of (18, 2). Can change the precision but scale will always be 2
/// Equivalent to <see cref="ValuePrecisionAttribute"/>(18, 2).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PriceValueAttribute : ValuePrecisionAttribute
{
    public PriceValueAttribute(int precision) : base(precision, 2) { }
}

/// <summary>
/// Marks a string property as long text, bypassing the default string length validation convention (255 characters).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class TextValueAttribute : ValidationAttribute
{
    public int MaxLength { get; }

    public TextValueAttribute(int maxLength = 10000)
    {
        MaxLength = maxLength;
    }

    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext context)
    {
        if (value is string text && text.Length > MaxLength)
        {
            return new ValidationResult(
                $"{context.DisplayName} cannot exceed {MaxLength} characters.");
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Marks a string property as JSON content, bypassing the default string length validation convention (255 characters).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class JsonValueAttribute : ValidationAttribute
{
    public int MaxLength { get; }

    public JsonValueAttribute(int maxLength = 50000)
    {
        MaxLength = maxLength;
    }

    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext context)
    {
        if (value is string text && text.Length > MaxLength)
        {
            return new ValidationResult(
                $"{context.DisplayName} cannot exceed {MaxLength} characters.");
        }

        return ValidationResult.Success;
    }
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class AllowedExtensionsAttribute : ValidationAttribute
{
    private readonly string[] _extensions;

    public AllowedExtensionsAttribute(string[] extensions)
    {
        _extensions = extensions.Select(x => x.StartsWith(".") ? x.ToLower() : "." + x.ToLower()).ToArray();
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        if (value is IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!_extensions.Contains(extension))
            {
                return new ValidationResult(GetErrorMessage());
            }
        }

        return ValidationResult.Success;
    }

    private string GetErrorMessage()
    {
        return ErrorMessage ?? $"Only Following Formats Are Allowed: {string.Join(", ", _extensions)}";
    }
}