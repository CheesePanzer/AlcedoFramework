using System.Text.RegularExpressions;

namespace Alcedo.Frontend.Abstraction.Helpers;

public static class EmailChecker
{
    public static bool IsValid(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}