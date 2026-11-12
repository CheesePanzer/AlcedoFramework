using EmailValidation;

namespace Alcedo.Framework.Identity.Application.Helper;

internal static class EmailFormatChecker
{
    public static bool IsValid(string? email, out string? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(email))
        {
            result = "email is empty";
            return false;
        }

        if (email.Length > 255)
        {
            result = "email is too long";
            return false;
        }

        var trimmedEmail = email.Trim();

        if (!EmailValidator.Validate(trimmedEmail, allowTopLevelDomains: true))
        {
            result = "email is invalid";
            return false;
        }

        return true;
    }
}