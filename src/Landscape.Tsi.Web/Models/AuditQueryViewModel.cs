using Landscape.Tsi.Application.Identity;

namespace Landscape.Tsi.Web.Models;

public sealed class AuditQueryViewModel
{
    public string? Search { get; init; }
    public string? Action { get; init; }
    public string? Entity { get; init; }
    public Guid? ActorUserId { get; init; }
    public int? EmpresaSubsidiariaId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public int TotalCount { get; init; }
    public IReadOnlyList<AuditEventRow> DashboardEvents { get; init; } = [];
    public IReadOnlyList<AuthorizationAuditSummary> Events { get; init; } = [];
    public IReadOnlyList<AuditFilterOption> EntityOptions { get; init; } = [];
    public IReadOnlyList<AuditFilterOption> UserOptions { get; init; } = [];
    public IReadOnlyList<AuditFilterOption> SubsidiaryOptions { get; init; } = [];
    public DateOnly DateFrom { get; init; }
    public DateOnly DateTo { get; init; }
    public string? QueryError { get; init; }
}