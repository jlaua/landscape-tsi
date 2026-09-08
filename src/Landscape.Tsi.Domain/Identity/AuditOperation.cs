namespace Landscape.Tsi.Domain.Identity;

public sealed class AuditOperation
{
    public Guid OperationId { get; set; } = Guid.NewGuid();
    public string CorrelationId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string EntityCode { get; set; } = string.Empty;
    public string? PhysicalTableName { get; set; }
    public long? RootRecordId { get; set; }
    public string? RootDisplayName { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorUserNameSnapshot { get; set; }
    public int? EmpresaSubsidiariaId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public int AffectedRecordCount { get; set; }
    public Guid? ReversesOperationId { get; set; }
    public string Status { get; set; } = "Succeeded";
    public int SchemaVersion { get; set; } = 1;

    public ICollection<AuditDeletedRecordSnapshot> Snapshots { get; set; } = new List<AuditDeletedRecordSnapshot>();
    public ICollection<AuditRecordKeyMap> KeyMaps { get; set; } = new List<AuditRecordKeyMap>();
}
