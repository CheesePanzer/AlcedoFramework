namespace Alcedo.Frontend.Abstraction.Helpers;

public static class PasswordStrengthChecker
{
    private const int MinimumStrength = 3;

    public static int GetStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return 0;
        }

        int score = 0;
        if (password.Length >= 8)
        {
            score++;
        }

        if (password.Any(char.IsUpper))
        {
            score++;
        }

        if (password.Any(char.IsDigit))
        {
            score++;
        }

        if (password.Any(c => !char.IsLetterOrDigit(c)))
        {
            score++;
        }

        return score;
    }

    public static bool IsStrong(string password) => GetStrength(password) >= MinimumStrength;
}