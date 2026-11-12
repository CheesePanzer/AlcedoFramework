namespace Alcedo.Framework.Identity.Application.Helper;

internal static class PasswordStrengthChecker
{
    private const int MinimumStrength = 3;

    private static int GetStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return 0;
        }

        var result = Zxcvbn.Core.EvaluatePassword(password);
        int score = result.Score;

        return score;
    }

    public static bool IsStrong(string password)
    {
        return GetStrength(password) >= MinimumStrength;
    }
}