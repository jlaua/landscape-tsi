namespace Landscape.Tsi.Domain.Identity;

public sealed class IamUsuarioOrganizacion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public IamUsuario User { get; set; } = null!;
    public int? EmpresaSubsidiariaId { get; set; }
    public EmpresaSubsidiariaReference? EmpresaSubsidiaria { get; set; }
    public bool IsCorporateScope { get; set; }
    public DateTime ValidFromUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ValidUntilUtc { get; set; }
    public Guid ApprovedByUserId { get; set; }
    public string? Justification { get; set; }

    public bool IsEffectiveAt(DateTime utcNow) =>
        ValidFromUtc <= utcNow && (ValidUntilUtc is null || ValidUntilUtc > utcNow);

    public void Validate()
    {
        if (IsCorporateScope == EmpresaSubsidiariaId.HasValue)
        {
            throw new InvalidOperationException("El alcance debe ser corporativo o de una subsidiaria, pero no ambos.");
        }

        if (ValidUntilUtc is not null && ValidUntilUtc <= ValidFromUtc)
        {
            throw new InvalidOperationException("La vigencia del alcance no es válida.");
        }
    }
}
