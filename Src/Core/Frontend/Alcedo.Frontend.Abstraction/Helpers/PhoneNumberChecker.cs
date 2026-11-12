using System.Text.RegularExpressions;

namespace Alcedo.Frontend.Abstraction.Helpers;

public static class PhoneNumberChecker
{
    public static bool IsValid(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        return Regex.IsMatch(phone, @"^\+[1-9]\d{7,14}$");
    }
}