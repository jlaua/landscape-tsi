using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;

namespace Landscape.Tsi.Infrastructure.Identity;

internal sealed class SeparationOfDutiesEvaluator(IdentityDbContext dbContext) : ISeparationOfDutiesEvaluator
{
    public async Task<bool> IsAuthorizedAsync(
        SegregatedOperation operation,
        SeparationOfDutiesContext context,
        CancellationToken cancellationToken = default)
    {
        var conflictingUserId = operation switch
        {
            SegregatedOperation.ApproveRequest => context.RequesterUserId,
            SegregatedOperation.ValidateCriticalEvidence => context.EvidenceSubmittedByUserId,
            SegregatedOperation.ApproveException => context.ExceptionRequestedByUserId,
            SegregatedOperation.ApproveAccessElevation => context.AccessBeneficiaryUserId,
            _ => null
        };
        if (conflictingUserId != context.ActorUserId)
        {
            return true;
        }

        dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
        {
            ActorUserId = context.ActorUserId,
            BeneficiaryUserId = context.AccessBeneficiaryUserId,
            EventType = "AuthorizationDenied",
            Result = "DeniedSeparationOfDuties",
            ResourceType = operation.ToString(),
            ResourceId = context.ResourceId,
            Justification = $"Regla de segregación aplicada: {operation}",
            CorrelationId = string.IsNullOrWhiteSpace(context.CorrelationId)
                ? Guid.NewGuid().ToString("N")
                : context.CorrelationId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return false;
    }
}