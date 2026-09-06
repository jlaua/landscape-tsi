using System.Text.Json;

using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Catalogs;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Landscape.Tsi.Infrastructure.Catalogs;

public sealed class DominioService(IdentityDbContext dbContext) : IDominioService
{
    public async Task<PagedResult<DominioRecord>> ListAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;
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
        var items = await query
            .OrderBy(item => item.Dominio)
            .ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new DominioRecord(
                item.Id,
                item.Dominio,
                item.DescripcionDominio,
                item.Referencias,
                item.HomologacionDimensionSegunCiber,
                item.HomologacionDimensionSegunLineamiento,
                item.SubDominioCvt,
                item.Ejemplos))
            .ToListAsync(cancellationToken);
        return new PagedResult<DominioRecord>(items, page, pageSize, totalCount);
    }

    public Task<DominioRecord?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Domains.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new DominioRecord(
                item.Id,
                item.Dominio,
                item.DescripcionDominio,
                item.Referencias,
                item.HomologacionDimensionSegunCiber,
                item.HomologacionDimensionSegunLineamiento,
                item.SubDominioCvt,
                item.Ejemplos))
            .SingleOrDefaultAsync(cancellationToken);

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
