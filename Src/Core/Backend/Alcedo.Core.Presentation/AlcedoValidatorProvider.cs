using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Alcedo.Core.Abstractions;
using Alcedo.Core.Presentation.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Alcedo.Core.Presentation;

public class DefaultStringLengthValidator : IModelValidator
{
    private readonly int _maxLength;

    public DefaultStringLengthValidator(int maxLength)
    {
        _maxLength = maxLength;
    }

    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        if (context.Model is string value && value.Length > _maxLength)
        {
            yield return new ModelValidationResult(
                context.ModelMetadata.PropertyName,
                $"{context.ModelMetadata.DisplayName} length can't be longer than {_maxLength}。");
        }
    }
}

public class DecimalPrecisionValidator : IModelValidator
{
    private readonly int _precision;
    private readonly int _scale;

    public DecimalPrecisionValidator(int precision, int scale)
    {
        _precision = precision;
        _scale = scale;
    }

    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        if (context.Model is decimal value)
        {
            var text = Math.Abs(value).ToString(CultureInfo.InvariantCulture);

            var parts = text.Split('.');
            var integerLength = parts[0].Length;
            var decimalLength = parts.Length > 1 ? parts[1].Length : 0;

            if (integerLength + decimalLength > _precision ||
                decimalLength > _scale)
            {
                yield return new ModelValidationResult(
                    context.ModelMetadata.PropertyName,
                    $"{context.ModelMetadata.DisplayName} precision is not ({_precision},{_scale})。");
            }
        }
    }
}

public class AlcedoValidatorProvider : IModelValidatorProvider
{
    
    public void CreateValidators(ModelValidatorProviderContext context)
    {
        var metadata = context.ModelMetadata;
        var attributes = metadata.ValidatorMetadata;


        if (metadata.ModelType == typeof(string))
        {
            bool ignored = attributes.Any(x =>
                x is MaxLengthAttribute ||
                x is StringLengthAttribute ||
                x is TextValueAttribute ||
                x is JsonValueAttribute);

            if (!ignored)
            {
                context.Results.Add(new ValidatorItem
                {
                    Validator = new DefaultStringLengthValidator(AlcedoStorageLengthConvention.DefaultStringMaxLength),
                    IsReusable = true
                });
            }
        }


        if (metadata.ModelType == typeof(decimal) ||
            metadata.ModelType == typeof(decimal?))
        {
            var custom = attributes
                .OfType<ValuePrecisionAttribute>()
                .FirstOrDefault();

            if (custom != null)
            {
                context.Results.Add(new ValidatorItem
                {
                    Validator = new DecimalPrecisionValidator(
                        custom.Precision,
                        custom.Scale),
                    IsReusable = true
                });
            }
            else
            {
                context.Results.Add(new ValidatorItem
                {
                    Validator = new DecimalPrecisionValidator(
                        AlcedoStorageLengthConvention.DefaultDecimalPrecision,
                        AlcedoStorageLengthConvention.DefaultDecimalScale),
                    IsReusable = true
                });
            }
        }
    }
}