using System.Reflection;
using Alcedo.Frontend.Abstraction.Attributes;

namespace Alcedo.Frontend.Abstraction.Helpers;

public static class AutoValidator
{
    public static List<string> Validate<T>(T model) where T : class
    {
        var errors = new List<string>();
        var nullability = new NullabilityInfoContext();

        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead)
            {
                continue;
            }

            if (prop.GetCustomAttribute<AutoValidateIgnoreAttribute>() is not null)
            {
                continue;
            }

            var value = prop.GetValue(model);
            var propType = prop.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;
            var isNullable = Nullable.GetUnderlyingType(propType) is not null
                             || nullability.Create(prop).WriteState == NullabilityState.Nullable;

            if (isNullable && (value is null || string.IsNullOrWhiteSpace(value as string)))
            {
                continue;
            }

            if (prop.GetCustomAttribute<AutoValidateRequiredAttribute>() is { } required)
            {
                if (value is null)
                {
                    errors.Add(required.ErrorMessage);
                }
            }

            if (prop.GetCustomAttribute<AutoValidateStringRequiredAttribute>() is { } strRequired)
            {
                if (string.IsNullOrWhiteSpace(value as string))
                {
                    errors.Add(strRequired.ErrorMessage);
                }
            }

            if (prop.GetCustomAttribute<AutoValidateEmailAttribute>() is { } email)
            {
                if (!EmailChecker.IsValid(value as string))
                {
                    errors.Add(email.ErrorMessage);
                }
            }

            if (prop.GetCustomAttribute<AutoValidatePhoneAttribute>() is { } phone)
            {
                if (!PhoneNumberChecker.IsValid(value as string))
                {
                    errors.Add(phone.ErrorMessage);
                }
            }

            if (prop.GetCustomAttribute<AutoValidateRangeAttribute>() is { } range)
            {
                var numeric = value is null ? (double?)null : Convert.ToDouble(value);
                if (numeric is null || numeric < range.Min || numeric > range.Max)
                {
                    errors.Add(range.ErrorMessage);
                }
            }

            if (prop.GetCustomAttribute<AutoValidateStringLengthAttribute>() is { } strLen)
            {
                var len = (value as string)?.Length ?? 0;
                if (len < strLen.Min || len > strLen.Max)
                {
                    errors.Add(strLen.ErrorMessage);
                }
            }

            if (prop.GetCustomAttribute<AutoValidateCustomAttribute>() is { } custom)
            {
                var method = custom.CheckerType.GetMethod(custom.MethodName,
                    BindingFlags.Public | BindingFlags.Static);
                if (method is not null)
                {
                    var result = method.Invoke(null, [value]) as string;
                    var msg = string.IsNullOrEmpty(custom.ErrorMessage) ? result : custom.ErrorMessage;
                    if (!string.IsNullOrEmpty(msg))
                    {
                        errors.Add(msg!);
                    }
                }
            }

            var hasExplicit = prop.GetCustomAttributes()
                .Any(a => a is AutoValidateRequiredAttribute
                    or AutoValidateStringRequiredAttribute
                    or AutoValidateEmailAttribute
                    or AutoValidatePhoneAttribute
                    or AutoValidateRangeAttribute
                    or AutoValidateStringLengthAttribute
                    or AutoValidateCustomAttribute);

            if (!hasExplicit)
            {
                if (propType == typeof(string) && !isNullable)
                {
                    if (string.IsNullOrWhiteSpace(value as string))
                    {
                        errors.Add($"{prop.Name} cannot be empty.");
                    }
                }
                else if (propType == typeof(Guid))
                {
                    if (value is Guid g && g == Guid.Empty)
                    {
                        errors.Add($"{prop.Name} is required.");
                    }
                }
                else if (!propType.IsValueType && !isNullable && value is null)
                {
                    errors.Add($"{prop.Name} is required.");
                }
            }
        }

        return errors;
    }
}