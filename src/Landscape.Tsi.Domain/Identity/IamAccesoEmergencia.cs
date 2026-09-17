namespace Landscape.Tsi.Domain.Identity;

public sealed class IamAccesoEmergencia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public IamUsuario User { get; set; } = null!;
    public int? EmpresaSubsidiariaId { get; set; }
    public EmpresaSubsidiariaReference? EmpresaSubsidiaria { get; set; }
    public required string IncidentReference { get; set; }
    public required string Justification { get; set; }
    public Guid ApprovedByUserId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }

    public bool IsActiveAt(DateTime utcNow) => StartsAtUtc <= utcNow && ExpiresAtUtc > utcNow;

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(IncidentReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Justification);
        if (ApprovedByUserId == UserId)
        {
            throw new InvalidOperationException("El beneficiario no puede aprobar su propio acceso de emergencia.");
        }

        if (ExpiresAtUtc <= StartsAtUtc)
        {
            throw new InvalidOperationException("El acceso de emergencia debe tener una vigencia positiva.");
        }
    }
}