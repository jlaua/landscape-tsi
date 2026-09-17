namespace Landscape.Tsi.Application.Catalogs;

public interface ICatalogManagementService
{
    Task<CatalogPageResult> ListAsync(MasterCatalogDefinition definition, string? search, int page, int pageSize, CancellationToken cancellationToken = default, string? sortColumn = null, string? sortDirection = null);
    Task<CatalogPageResult> ListRelatedAsync(MasterCatalogDefinition definition, CatalogColumnDefinition foreignKey, int parentId, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogRelationBucket>> GetRelationCountsAsync(MasterCatalogDefinition parent, MasterCatalogDefinition child, CatalogColumnDefinition foreignKey, CancellationToken cancellationToken = default);
    Task<int> GetRelatedCountAsync(MasterCatalogDefinition child, CatalogColumnDefinition foreignKey, int parentId, CancellationToken cancellationToken = default);
    Task<CatalogRow?> GetAsync(MasterCatalogDefinition definition, int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>>> GetOptionsAsync(MasterCatalogDefinition definition, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(MasterCatalogDefinition definition, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(MasterCatalogDefinition definition, int id, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, int>> GetVendorTechnologyCountsAsync(IEnumerable<int> vendorIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VendorTechnologyDto>> GetVendorTechnologiesAsync(int vendorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, int>> GetVendorContactCountsAsync(IEnumerable<int> vendorIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<int, int>>(new Dictionary<int, int>());
    Task<IReadOnlyDictionary<int, int>> GetPartnerContactCountsAsync(IEnumerable<int> partnerIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<int, int>>(new Dictionary<int, int>());
    Task<IReadOnlyDictionary<int, int>> GetCompanyContactCountsAsync(IEnumerable<int> companyIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<int, int>>(new Dictionary<int, int>());
}

public sealed record CatalogRelationBucket(int ParentId, string ParentName, int Total);

public sealed record VendorTechnologyDto(
    int Id,
    string NombreCorporativo,
    string? NombreLocal,
    string? Familia,
    string? EstadoAdopcion,
    string? Licenciamiento,
    string? Entorno
);