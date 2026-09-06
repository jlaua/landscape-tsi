using System.Security.Cryptography;
using System.Text;

namespace Landscape.Tsi.Infrastructure.Identity;

public static class AuditIdentifier
{
    public static string? ProtectExternalSubject(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(subject));
        return $"OIDC:{Convert.ToHexString(digest.AsSpan(0, 12))}";
    }
}
