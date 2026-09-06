using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Identity;

internal sealed class AuthorizationAuditQuery(
    IdentityDbContext dbContext,
    IEffectiveAccessService effectiveAccess) : IAuthorizationAuditQuery
{
    public async Task<IReadOnlyList<AuthorizationAuditSummary>> ListAsync(
        Guid actorUserId,
        int empresaSubsidiariaId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        if (!await effectiveAccess.IsAuthorizedAsync(
            actorUserId, Permissions.AuditView, empresaSubsidiariaId, cancellationToken))
        {
            throw new UnauthorizedAccessException("No autorizado.");
        }

        var events = await dbContext.AuthorizationAuditEvents.AsNoTracking()
            .Where(item => item.EmpresaSubsidiariaId == empresaSubsidiariaId)
            .OrderByDescending(item => item.OccurredAtUtc)
            .Select(item => new AuthorizationAuditSummary(
                item.Id, item.OccurredAtUtc, item.EventType, item.Result,
                item.ActorUserId, item.EmpresaSubsidiariaId))
            .ToListAsync(cancellationToken);

        dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
        {
            ActorUserId = actorUserId,
            EmpresaSubsidiariaId = empresaSubsidiariaId,
            EventType = "AuditViewed",
            PermissionCode = Permissions.AuditView,
            Result = "Succeeded",
            ResourceType = nameof(IamEventoAuditoriaAutorizacion),
            Justification = "Consulta autorizada dentro del alcance.",
            CorrelationId = correlationId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return events;
    }
}
