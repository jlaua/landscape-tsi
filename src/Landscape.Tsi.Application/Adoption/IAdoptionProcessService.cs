namespace Landscape.Tsi.Application.Adoption;

public interface IAdoptionProcessService
{
    Task<IReadOnlyList<AdoptionProcessSummaryDto>> ListProcessesAsync(CancellationToken cancellationToken = default);
    Task<AdoptionProcessDetailDto?> GetProcessDetailAsync(int procesoId, CancellationToken cancellationToken = default);
    Task<AdoptionProcessDetailDto?> GetProcessDetailByBuildingBlockAsync(int buildingBlockId, CancellationToken cancellationToken = default);

    Task<AdoptionResult> CreateProcessAsync(CreateAdoptionProcessCommand command, CancellationToken cancellationToken = default);
    Task<AdoptionResult> UpdateProcessStatusAsync(int procesoId, int nuevoEstadoId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);

    Task<AdoptionResult> ConveneCompanyAsync(ConveneCompanyCommand command, CancellationToken cancellationToken = default);

    Task<AdoptionResult> SetCorporateStandardAsync(SetCorporateStandardCommand command, CancellationToken cancellationToken = default);

    Task<AdoptionResult> RegisterImplementedTechnologyAsync(RegisterImplementedTechnologyCommand command, CancellationToken cancellationToken = default);
    Task<AdoptionResult> DeleteImplementedTechnologyAsync(int procesoId, int tecnologiaImplementadaId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);

    Task<AdoptionResult> SaveContractAsync(SaveContractCommand command, CancellationToken cancellationToken = default);
    Task<AdoptionResult> DeleteContractAsync(int contratoId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);

    Task<AdoptionResult> SaveDriverAsync(SaveDriverCommand command, CancellationToken cancellationToken = default);
    Task<AdoptionResult> DeleteDriverAsync(int driverId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);

    Task<AdoptionResult> SaveOperationModelAsync(SaveOperationModelCommand command, CancellationToken cancellationToken = default);

    Task<BuildingBlockCapabilitiesDto?> GetBuildingBlockCapabilitiesAsync(int buildingBlockId, CancellationToken cancellationToken = default);
    Task<AdoptionResult> BatchConveneCompaniesAsync(int procesoId, IEnumerable<ConveneCompanyInput> companies, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);
    Task<AdoptionResult> RemoveCompanyFromProcessAsync(int procesoId, int procesoEmpresaId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);
    Task<AdoptionResult> DeactivateProcessAsync(int procesoId, string motivoBaja, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);
    Task<AdoptionResult> DeleteProcessCascadeAsync(int procesoId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);
    Task<AdoptionResult> FinalizeEvaluationWithStandardAsync(FinalizeEvaluationWithStandardCommand command, CancellationToken cancellationToken = default);

    Task<EvaluationReportsDto?> GetEvaluationReportsAsync(int procesoId, CancellationToken cancellationToken = default);
}