namespace Landscape.Tsi.Domain.Identity;

public sealed class AuditRecordKeyMap
{
    public long Id { get; set; }
    public Guid OperationId { get; set; }
    public AuditOperation Operation { get; set; } = null!;
    public string PhysicalTableName { get; set; } = string.Empty;
    public string OldPrimaryKeyJson { get; set; } = string.Empty;
    public string NewPrimaryKeyJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
