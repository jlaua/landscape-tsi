namespace Landscape.Tsi.Application.Identity;

public enum SegregatedOperation
{
    ApproveRequest,
    ValidateCriticalEvidence,
    ApproveException,
    ApproveAccessElevation
}

public sealed record SeparationOfDutiesContext(
    Guid ActorUserId,
    Guid? RequesterUserId = null,
    Guid? EvidenceSubmittedByUserId = null,
    Guid? ExceptionRequestedByUserId = null,
    Guid? AccessBeneficiaryUserId = null,
    string? ResourceId = null,
    string? CorrelationId = null);

public interface ISeparationOfDutiesEvaluator
{
    Task<bool> IsAuthorizedAsync(
        SegregatedOperation operation,
        SeparationOfDutiesContext context,
        CancellationToken cancellationToken = default);
}
