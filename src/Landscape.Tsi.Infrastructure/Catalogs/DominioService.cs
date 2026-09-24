using System.Text.Json;

using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Catalogs;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Landscape.Tsi.Infrastructure.Catalogs;

public sealed class DominioService(IdentityDbContext dbContext, CatalogDbContext? catalogDbContext = null, IAuditTrailService? auditTrail = null) : IDominioService
{
    public async Task<PagedResult<DominioRecord>> ListAsync(
        string? search,
        int page,
        int pageSize,
        string? sort = null,
        string? dir = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize > 0 ? Math.Min(pageSize, 100000) : 10;
        var query = dbContext.Domains.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item =>
                (item.Dominio ?? string.Empty).Contains(term) ||
                (item.DescripcionDominio ?? string.Empty).Contains(term) ||
                (item.Referencias ?? string.Empty).Contains(term) ||
                (item.HomologacionDimensionSegunCiber ?? string.Empty).Contains(term) ||
                (item.HomologacionDimensionSegunLineamiento ?? string.Empty).Contains(term) ||
                (item.SubDominioCvt ?? string.Empty).Contains(term) ||
                (item.Ejemplos ?? string.Empty).Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Min(page, totalPages);

        var domainEntities = await query.ToListAsync(cancellationToken);

        Dictionary<int, int> bbCounts = new();
        if (catalogDbContext != null)
        {
            bbCounts = await catalogDbContext.BuildingBlocks.AsNoTracking()
                .Where(b => b.IdDominio.HasValue)
                .GroupBy(b => b.IdDominio!.Value)
                .Select(g => new { DomainId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.DomainId, g => g.Count, cancellationToken);
        }

        var records = domainEntities.Select(item => new DominioRecord(
            item.Id,
            item.Dominio,
            item.DescripcionDominio,
            item.Referencias,
            item.HomologacionDimensionSegunCiber,
            item.HomologacionDimensionSegunLineamiento,
            item.SubDominioCvt,
            item.Ejemplos,
            bbCounts.TryGetValue(item.Id, out var count) ? count : 0));

        var isDesc = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);
        records = (sort?.ToLowerInvariant()) switch
        {
            "descripcion" => isDesc ? records.OrderByDescending(r => r.DescripcionDominio).ThenBy(r => r.Dominio)
                                    : records.OrderBy(r => r.DescripcionDominio).ThenBy(r => r.Dominio),
            "buildingblocks" or "buildingblock" or "count" => isDesc ? records.OrderByDescending(r => r.BuildingBlockCount).ThenBy(r => r.Dominio)
                                                                     : records.OrderBy(r => r.BuildingBlockCount).ThenBy(r => r.Dominio),
            _ => isDesc ? records.OrderByDescending(r => r.Dominio).ThenBy(r => r.Id)
                        : records.OrderBy(r => r.Dominio).ThenBy(r => r.Id)
        };

        var pagedItems = records
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<DominioRecord>(pagedItems, page, pageSize, totalCount);
    }

    public async Task<DominioRecord?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.Domains.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return null;

        var count = 0;
        if (catalogDbContext != null)
        {
            count = await catalogDbContext.BuildingBlocks.AsNoTracking()
                .CountAsync(b => b.IdDominio == id, cancellationToken);
        }

        return new DominioRecord(
            item.Id,
            item.Dominio,
            item.DescripcionDominio,
            item.Referencias,
            item.HomologacionDimensionSegunCiber,
            item.HomologacionDimensionSegunLineamiento,
            item.SubDominioCvt,
            item.Ejemplos,
            count);
    }

    public async Task<int> CreateAsync(
        DominioCommand command,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            if (dbContext.Database.IsRelational())
            {
                transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            }

            var entity = new TmDominio();
            Apply(entity, command);
            dbContext.Domains.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            if (auditTrail is not null)
                await auditTrail.RecordCreateAsync("dominio", "TMDominio", entity.Id, entity.Dominio, actorUserId,
                    correlationId, "Creación de Dominio.", 1, cancellationToken);
            AddAudit("MasterCatalog.Created", Permissions.CatalogCreate, entity.Id, actorUserId,
                correlationId, null, ToRecord(entity), "Succeeded");
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return entity.Id;
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<bool> UpdateAsync(
        int id,
        DominioCommand command,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Domains.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        IDbContextTransaction? transaction = null;
        try
        {
            if (dbContext.Database.IsRelational())
            {
                transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            }

            var before = ToRecord(entity);
            Apply(entity, command);
            var after = ToRecord(entity);
            if (auditTrail is not null)
                await auditTrail.RecordUpdateAsync("dominio", "TMDominio", entity.Id, entity.Dominio, actorUserId,
                    correlationId, "Actualización de Dominio.", cancellationToken);
            AddAudit("MasterCatalog.Updated", Permissions.CatalogEdit, entity.Id, actorUserId,
                correlationId, before, after, "Succeeded");
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return true;
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private void AddAudit(
        string eventType,
        string permission,
        int resourceId,
        Guid actorUserId,
        string correlationId,
        DominioRecord? before,
        DominioRecord? after,
        string result)
    {
        dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
        {
            ActorUserId = actorUserId,
            EventType = eventType,
            PermissionCode = permission,
            Result = result,
            ResourceType = "TMDominio",
            ResourceId = resourceId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after),
            CorrelationId = correlationId
        });
    }

    private static void Apply(TmDominio entity, DominioCommand command)
    {
        entity.Dominio = Normalize(command.Dominio);
        entity.DescripcionDominio = Normalize(command.DescripcionDominio);
        entity.Referencias = Normalize(command.Referencias);
        entity.HomologacionDimensionSegunCiber = Normalize(command.HomologacionDimensionSegunCiber);
        entity.HomologacionDimensionSegunLineamiento = Normalize(command.HomologacionDimensionSegunLineamiento);
        entity.SubDominioCvt = Normalize(command.SubDominioCvt);
        entity.Ejemplos = Normalize(command.Ejemplos);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DominioRecord ToRecord(TmDominio entity) => new(
        entity.Id,
        entity.Dominio,
        entity.DescripcionDominio,
        entity.Referencias,
        entity.HomologacionDimensionSegunCiber,
        entity.HomologacionDimensionSegunLineamiento,
        entity.SubDominioCvt,
        entity.Ejemplos);
}