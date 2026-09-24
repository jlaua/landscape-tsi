namespace Landscape.Tsi.Application.Catalogs;

public interface IDominioService
{
    Task<PagedResult<DominioRecord>> ListAsync(
        string? search,
        int page,
        int pageSize,
        string? sort = null,
        string? dir = null,
        CancellationToken cancellationToken = default);
    Task<DominioRecord?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(DominioCommand command, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, DominioCommand command, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);
}