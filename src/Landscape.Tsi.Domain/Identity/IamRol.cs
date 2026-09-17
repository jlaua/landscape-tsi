namespace Landscape.Tsi.Domain.Identity;

public sealed class IamRol
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Code { get; set; }
    public required string Name { get; set; }
    public ICollection<IamUsuarioRol> Users { get; set; } = [];
    public ICollection<IamRolPermiso> Permissions { get; set; } = [];
}

public sealed class IamPermiso
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Code { get; set; }
    public required string Description { get; set; }
    public ICollection<IamRolPermiso> Roles { get; set; } = [];
}

public sealed class IamUsuarioRol
{
    public Guid UserId { get; set; }
    public IamUsuario User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public IamRol Role { get; set; } = null!;
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ValidFromUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ValidUntilUtc { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid? ExecutedByUserId { get; set; }
    public string? Justification { get; set; }

    public bool IsEffectiveAt(DateTime utcNow) =>
        ValidFromUtc <= utcNow && (ValidUntilUtc is null || ValidUntilUtc > utcNow);
}

public sealed class IamRolPermiso
{
    public Guid RoleId { get; set; }
    public IamRol Role { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public IamPermiso Permission { get; set; } = null!;
}