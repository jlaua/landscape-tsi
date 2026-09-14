namespace Landscape.Tsi.Application.Adoption;

public interface IServiceManagementService
{
    Task<IReadOnlyList<ServiceTypeDto>> GetServiceTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportActivityDto>> GetSupportActivitiesAsync(string? nivel = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceDetailDto>> GetServicesByProcessAsync(int procesoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceDetailDto>> GetServicesByTechnologyAsync(int tecnologiaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceDetailDto>> GetServicesByImplementationAsync(int implementadaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceDetailDto>> GetServicesByCompanyAsync(int empresaId, CancellationToken cancellationToken = default);
    Task<ServiceDetailDto?> GetServiceByIdAsync(int servicioId, CancellationToken cancellationToken = default);

    Task<ServiceResult> SaveServiceHeaderAsync(SaveServiceHeaderCommand command, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteServiceAsync(int servicioId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);

    Task<ServiceResult> SaveProjectRateCardAsync(SaveProjectRateCardCommand command, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteProjectRateCardAsync(int rateCardId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);

    Task<ServiceResult> SaveOperationRateCardAsync(SaveOperationRateCardCommand command, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteOperationRateCardAsync(int rateCardId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);
}