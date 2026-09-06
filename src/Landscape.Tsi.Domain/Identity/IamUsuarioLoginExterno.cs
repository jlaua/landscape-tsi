using Microsoft.AspNetCore.Identity;

namespace Landscape.Tsi.Domain.Identity;

public sealed class IamUsuarioLoginExterno : IdentityUserLogin<Guid>
{
    public IamUsuario User { get; set; } = null!;
    public required string Issuer { get; set; }
    public required string Subject { get; set; }
    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(Subject);
    }
}
