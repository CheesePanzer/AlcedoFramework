using PhoneNumbers;

namespace Alcedo.Framework.Identity.Application.Helper;

internal static class PhoneNumberFormatValidator
{
    private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

    /// <summary>
    /// 核心校验函数：强制要求 [+区号][号码] 格式
    /// </summary>
    /// <param name="rawNumber">必须以 + 开头的字符串，如 +8613800138000</param>
    public static bool IsValid(string rawNumber, out string? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(rawNumber))
        {
            result = "Contact Number is empty";
            return false;
        }

        var input = rawNumber.Trim();

        if (!input.StartsWith("+"))
        {
            result = "Contact Number should start with '+'";
            return false;
        }

        if (input.Length > 20)
        {
            result = "Contact Number is too long";
            return false;
        }

        try
        {
            var numberProto = Util.Parse(input, null);

            return Util.IsValidNumber(numberProto);
        }
        catch (NumberParseException ex)
        {
            result = ex.Message;
            return false;
        }
    }
}