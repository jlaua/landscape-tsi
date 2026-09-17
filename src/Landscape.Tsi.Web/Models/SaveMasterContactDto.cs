namespace Landscape.Tsi.Web.Models;

public sealed class SaveMasterContactDto
{
    public string? Nombre { get; set; }
    public string? Rol { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Otro { get; set; }
    public string? Notas { get; set; }
}

public sealed class ReorderCapabilitiesDto
{
    public List<int> CapabilityIds { get; set; } = new();
}

public sealed class AssignFocalDto
{
    public int? ContactoFocalId { get; set; }
}

public sealed class UpdateCapabilityStateDto
{
    public int CapacidadId { get; set; }
    public string EstadoCodigo { get; set; } = "NA";
    public string? Comentario { get; set; }
}

public sealed class UpdateCapabilityCommentDto
{
    public string? Comentario { get; set; }
}

public sealed class UpdateDriverVolumeDto
{
    public string DriverKey { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
}