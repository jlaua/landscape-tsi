using Landscape.Tsi.Application.Identity;

namespace Landscape.Tsi.Web.Models;

public sealed class AuditQueryViewModel
{
    public int? EmpresaSubsidiariaId { get; init; }
    public IReadOnlyList<AuthorizationAuditSummary> Events { get; init; } = [];
}
