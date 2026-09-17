using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Identity;

internal sealed class BreakGlassService(IdentityDbContext dbContext) : IBreakGlassService
{
    public async Task<Guid> ActivateAsync(BreakGlassRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CorrelationId);
        var access = new IamAccesoEmergencia
        {
            UserId = request.UserId,
            ApprovedByUserId = request.ApprovedByUserId,
            EmpresaSubsidiariaId = request.EmpresaSubsidiariaId,
            IncidentReference = request.IncidentReference,
            Justification = request.Justification,
            StartsAtUtc = request.StartsAtUtc,
            ExpiresAtUtc = request.ExpiresAtUtc
        };
        access.Validate();

        if (!await dbContext.Users.AnyAsync(user => user.Id == request.UserId && user.IsActive, cancellationToken))
        {
            throw new InvalidOperationException("El beneficiario no es un usuario activo.");
        }

        dbContext.EmergencyAccess.Add(access);
        dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
        {
            ActorUserId = request.ApprovedByUserId,
            BeneficiaryUserId = request.UserId,
            EmpresaSubsidiariaId = request.EmpresaSubsidiariaId,
            EventType = "BreakGlassActivated",
            PermissionCode = "BREAK_GLASS",
            Result = "AlertRaised",
            ResourceType = nameof(IamAccesoEmergencia),
            ResourceId = access.Id.ToString(),
            Justification = $"{request.IncidentReference}: {request.Justification}",
            ApprovedByUserId = request.ApprovedByUserId,
            CorrelationId = request.CorrelationId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return access.Id;
    }

    public Task<bool> IsActiveAsync(Guid accessId, DateTime utcNow, CancellationToken cancellationToken = default) =>
        dbContext.EmergencyAccess.AnyAsync(access =>
            access.Id == accessId && access.StartsAtUtc <= utcNow && access.ExpiresAtUtc > utcNow,
            cancellationToken);
}