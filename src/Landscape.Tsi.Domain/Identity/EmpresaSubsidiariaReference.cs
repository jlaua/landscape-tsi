namespace Landscape.Tsi.Domain.Identity;

/// <summary>
/// Read-only reference to the existing authoritative organization table.
/// Identity migrations must never create or alter this entity.
/// </summary>
public sealed class EmpresaSubsidiariaReference
{
    public int Id { get; set; }
}
