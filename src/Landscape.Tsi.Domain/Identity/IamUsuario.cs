using Microsoft.AspNetCore.Identity;

namespace Landscape.Tsi.Domain.Identity;

public sealed class IamUsuario : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
    public bool BootstrapManaged { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidUntilUtc { get; set; }

    public bool CanAuthenticateAt(DateTime utcNow) =>
        IsActive &&
        (ValidFromUtc is null || ValidFromUtc <= utcNow) &&
        (ValidUntilUtc is null || ValidUntilUtc > utcNow);
}
