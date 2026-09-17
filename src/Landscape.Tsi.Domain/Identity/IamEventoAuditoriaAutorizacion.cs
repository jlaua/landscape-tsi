namespace Landscape.Tsi.Domain.Identity;

public sealed class IamEventoAuditoriaAutorizacion
{
    public long Id { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? ActorUserId { get; set; }
    public Guid? BeneficiaryUserId { get; set; }
    public int? EmpresaSubsidiariaId { get; set; }
    public required string EventType { get; set; }
    public string? PermissionCode { get; set; }
    public required string Result { get; set; }
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? Justification { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public required string CorrelationId { get; set; }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(EventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(Result);
        ArgumentException.ThrowIfNullOrWhiteSpace(CorrelationId);
    }
}