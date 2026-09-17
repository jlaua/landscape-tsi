namespace Landscape.Tsi.Domain.Identity;

public sealed class AuditDeletedRecordSnapshot
{
    public long SnapshotId { get; set; }
    public Guid OperationId { get; set; }
    public AuditOperation Operation { get; set; } = null!;
    public string EntityCode { get; set; } = string.Empty;
    public string PhysicalTableName { get; set; } = string.Empty;
    public string PrimaryKeyJson { get; set; } = string.Empty;
    public string ForeignKeysJson { get; set; } = string.Empty;
    public string RowDataJson { get; set; } = string.Empty;
    public int DeleteOrder { get; set; }
    public int RestoreOrder { get; set; }
    public bool IsRoot { get; set; }
    public string? DisplayName { get; set; }
}