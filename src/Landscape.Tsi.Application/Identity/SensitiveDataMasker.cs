using System.Text.RegularExpressions;

namespace Landscape.Tsi.Application.Identity;

public static partial class SensitiveDataMasker
{
    [GeneratedRegex(@"^([^@]+)@(.+)$")]
    private static partial Regex EmailRegex();

    public static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return email;
        var match = EmailRegex().Match(email.Trim());
        if (!match.Success) return "••••••••";

        var user = match.Groups[1].Value;
        var domain = match.Groups[2].Value;

        if (user.Length <= 2)
        {
            return $"{user[0]}*@{domain}";
        }

        return $"{user[0]}***{user[^1]}@{domain}";
    }

    public static string? MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return phone;
        var digits = phone.Trim();
        if (digits.Length <= 4)
        {
            return "••••••";
        }

        var prefix = digits[..Math.Min(4, digits.Length / 2)];
        var suffix = digits[^2..];
        return $"{prefix} ••• {suffix}";
    }
}