using System.Data;
using System.Data.Common;
using System.Text.Json;
using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Landscape.Tsi.Infrastructure.Catalogs;

public sealed class BuildingBlockTechnologyMappingService(IdentityDbContext dbContext, IAuditTrailService auditTrail) : IBuildingBlockTechnologyMappingService
{
    public async Task<TechnologyMappingPage> ListAsync(TechnologyMappingQuery query, CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 10, 100);
        var page = Math.Max(1, query.Page);
        var search = $"%{query.Search?.Trim() ?? string.Empty}%";
        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);
        var state = query.OnlyPending ? "Unmapped" : query.MappingState ?? "";
        var direction = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        var order = query.SortBy?.ToLowerInvariant() switch
        {
            "domain" => $"d.dominio {direction},b.nombreBuildingBlock ASC",
            "phase" => $"p.nombreFaseAdopcion {direction},b.nombreBuildingBlock ASC",
            "technologies" => $"TechnologyCount {direction},b.nombreBuildingBlock ASC",
            "status" => $"TechnologyCount {direction},b.nombreBuildingBlock ASC",
            _ => $"b.nombreBuildingBlock {direction},b.idBuildingBlock {direction}"
        };
        const string filters = @"
WHERE (@search = N'%%' OR b.nombreBuildingBlock LIKE @search OR EXISTS (
    SELECT 1 FROM dbo.TBuildingBlockVsTTecnologiaTSI searchBridge
    JOIN dbo.TTecnologiaTSI searchTechnology ON searchTechnology.idTecnologiaTSI=searchBridge.idTecnologiaTSI
    WHERE searchBridge.idBuildingBlock=b.idBuildingBlock
      AND (COALESCE(searchTechnology.[nombreTecnologiaAlternativa1-Corporativo],N'') LIKE @search
        OR COALESCE(searchTechnology.[nombreTecnologiaAlternativa2-Local],N'') LIKE @search)))
AND (@familyId IS NULL OR EXISTS (
    SELECT 1 FROM dbo.TBuildingBlockVsTTecnologiaTSI familyBridge
    JOIN dbo.TTecnologiaTSI familyTechnology ON familyTechnology.idTecnologiaTSI=familyBridge.idTecnologiaTSI
    WHERE familyBridge.idBuildingBlock=b.idBuildingBlock AND familyTechnology.idFamilia=@familyId))
AND (@domainId IS NULL OR b.idDominio=@domainId)
AND (@phaseId IS NULL OR b.idEstadoFaseDeAdopcionBuildingBlock=@phaseId)
AND (@state = N'' OR (@state = N'Mapped' AND EXISTS (SELECT 1 FROM dbo.TBuildingBlockVsTTecnologiaTSI x WHERE x.idBuildingBlock=b.idBuildingBlock))
 OR (@state = N'Unmapped' AND NOT EXISTS (SELECT 1 FROM dbo.TBuildingBlockVsTTecnologiaTSI x WHERE x.idBuildingBlock=b.idBuildingBlock)))";
        var total = await ScalarAsync(connection, null, "SELECT COUNT(*) FROM dbo.TBuildingBlock b " + filters,
            cancellationToken, ("@search", search), ("@familyId", (object?)query.FamilyId ?? DBNull.Value),
            ("@domainId", (object?)query.DomainId ?? DBNull.Value), ("@phaseId", (object?)query.PhaseId ?? DBNull.Value), ("@state", state));

        var rows = new List<BuildingBlockMappingRow>();
        await using var command = Create(connection, null, @"
SELECT b.idBuildingBlock,b.nombreBuildingBlock,d.dominio,p.nombreFaseAdopcion,
(SELECT COUNT(*) FROM dbo.TBuildingBlockVsTTecnologiaTSI x WHERE x.idBuildingBlock=b.idBuildingBlock) AS TechnologyCount
FROM dbo.TBuildingBlock b
LEFT JOIN dbo.TMDominio d ON d.iddominio=b.idDominio
LEFT JOIN dbo.TEstadoFaseAdopcion p ON p.idEstadoFaseAdopcion=b.idEstadoFaseDeAdopcionBuildingBlock " + filters + $@"
ORDER BY {order} OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY");
            Add(command, "@search", search);
            Add(command, "@familyId", (object?)query.FamilyId ?? DBNull.Value);
            Add(command, "@domainId", (object?)query.DomainId ?? DBNull.Value);
            Add(command, "@phaseId", (object?)query.PhaseId ?? DBNull.Value);
            Add(command, "@state", state);
            Add(command, "@offset", (page - 1) * pageSize);
            Add(command, "@pageSize", pageSize);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new BuildingBlockMappingRow(reader.GetInt32(0), reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3),
                [], reader.GetInt32(4) > 0, string.Empty, string.Empty));
        }
        await reader.DisposeAsync();
        if (rows.Count > 0)
        {
            var parameterNames = rows.Select((_, index) => $"@building{index}").ToArray();
            await using var technologyCommand = Create(connection, null, $@"
SELECT bridge.idBuildingBlock,t.idTecnologiaTSI,COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo],N''),f.nombreFamilia,a.nombreEstadoAdopcionTSI
FROM dbo.TBuildingBlockVsTTecnologiaTSI bridge
JOIN dbo.TTecnologiaTSI t ON t.idTecnologiaTSI=bridge.idTecnologiaTSI
LEFT JOIN dbo.TMFamilia f ON f.idFamilia=t.idFamilia
LEFT JOIN dbo.TMEstadoAdopcionTSI a ON a.idEstadoAdopcionTSI=t.idEstadoAdopcionTSI
WHERE bridge.idBuildingBlock IN ({string.Join(',', parameterNames)})
ORDER BY bridge.idBuildingBlock,t.[nombreTecnologiaAlternativa1-Corporativo],t.idTecnologiaTSI");
            for (var index = 0; index < rows.Count; index++) Add(technologyCommand, parameterNames[index], rows[index].BuildingBlockId);
            var technologies = rows.ToDictionary(row => row.BuildingBlockId, _ => new List<TechnologyOption>());
            await using var technologyReader = await technologyCommand.ExecuteReaderAsync(cancellationToken);
            while (await technologyReader.ReadAsync(cancellationToken))
            {
                technologies[technologyReader.GetInt32(0)].Add(new TechnologyOption(
                    technologyReader.GetInt32(1), technologyReader.GetString(2),
                    technologyReader.IsDBNull(3) ? null : technologyReader.GetString(3),
                    technologyReader.IsDBNull(4) ? null : technologyReader.GetString(4), true, string.Empty));
            }
            rows = rows.Select(row => row with { Technologies = technologies[row.BuildingBlockId] }).ToList();
        }
        var kpis = await GetKpisAsync(connection, cancellationToken);
        var families = await ReadFamiliesAsync(connection, cancellationToken);
        var domains = await ReadDomainsAsync(connection, cancellationToken);
        var phases = await ReadPhasesAsync(connection, cancellationToken);
        return new TechnologyMappingPage(rows, kpis, families, domains, phases, page, pageSize, total);
    }

    public async Task<TechnologyRelationResult?> GetTechnologyRelationsAsync(int technologyId, CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection(); await EnsureOpenAsync(connection, cancellationToken);
        await using var command = Create(connection, null, @"SELECT t.idTecnologiaTSI,COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo],N''),f.nombreFamilia FROM dbo.TTecnologiaTSI t LEFT JOIN dbo.TMFamilia f ON f.idFamilia=t.idFamilia WHERE t.idTecnologiaTSI=@id"); Add(command, "@id", technologyId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken); if (!await reader.ReadAsync(cancellationToken)) return null;
        var name = reader.GetString(1); var family = reader.IsDBNull(2) ? null : reader.GetString(2); await reader.DisposeAsync();
        var blocks = await ReadBlocksAsync(connection, cancellationToken, technologyId); return new TechnologyRelationResult(technologyId, name, family, blocks);
    }

    public async Task<UnassignedTechnologyPage> ListUnassignedAsync(UnassignedTechnologyQuery query, CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 10, 100); var page = Math.Max(1, query.Page);
        var search = $"%{query.Search?.Trim() ?? string.Empty}%";
        var connection = dbContext.Database.GetDbConnection(); await EnsureOpenAsync(connection, cancellationToken);
        const string where = @" WHERE NOT EXISTS (SELECT 1 FROM dbo.TBuildingBlockVsTTecnologiaTSI x WHERE x.idTecnologiaTSI=t.idTecnologiaTSI)
AND (@search=N'%%' OR COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo],N'') LIKE @search OR COALESCE(t.[nombreTecnologiaAlternativa2-Local],N'') LIKE @search)
AND (@familyId IS NULL OR t.idFamilia=@familyId)";
        var total = await ScalarAsync(connection, null, "SELECT COUNT(*) FROM dbo.TTecnologiaTSI t" + where, cancellationToken,
            ("@search", search), ("@familyId", (object?)query.FamilyId ?? DBNull.Value));
        var items = new List<TechnologyOption>();
        await using var command = Create(connection, null, @"SELECT t.idTecnologiaTSI,COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo],N''),f.nombreFamilia,a.nombreEstadoAdopcionTSI
FROM dbo.TTecnologiaTSI t LEFT JOIN dbo.TMFamilia f ON f.idFamilia=t.idFamilia
LEFT JOIN dbo.TMEstadoAdopcionTSI a ON a.idEstadoAdopcionTSI=t.idEstadoAdopcionTSI" + where + @"
ORDER BY t.[nombreTecnologiaAlternativa1-Corporativo],t.idTecnologiaTSI OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY");
        Add(command, "@search", search); Add(command, "@familyId", (object?)query.FamilyId ?? DBNull.Value);
        Add(command, "@offset", (page - 1) * pageSize); Add(command, "@pageSize", pageSize);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) items.Add(new TechnologyOption(reader.GetInt32(0), reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3), false, string.Empty));
        await reader.DisposeAsync();
        return new UnassignedTechnologyPage(items, await ReadFamiliesAsync(connection, cancellationToken),
            await ReadBlocksAsync(connection, cancellationToken), page, pageSize, total);
    }

    public async Task<BuildingTechnologyRelationResult?> GetBuildingBlockRelationsAsync(int buildingBlockId, string? search = null, int? familyId = null, CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection(); await EnsureOpenAsync(connection, cancellationToken);
        await using var nameCommand = Create(connection, null, @"SELECT b.nombreBuildingBlock,d.dominio FROM dbo.TBuildingBlock b LEFT JOIN dbo.TMDominio d ON d.iddominio=b.idDominio WHERE b.idBuildingBlock=@id"); Add(nameCommand, "@id", buildingBlockId);
        await using var nameReader = await nameCommand.ExecuteReaderAsync(cancellationToken);
        if (!await nameReader.ReadAsync(cancellationToken)) return null;
        var name = nameReader.GetString(0); var domain = nameReader.IsDBNull(1) ? null : nameReader.GetString(1);
        await nameReader.DisposeAsync();
        return new BuildingTechnologyRelationResult(buildingBlockId, name, domain, await ReadTechnologyOptionsAsync(connection, buildingBlockId, search, familyId, cancellationToken));
    }

    public async Task<IReadOnlyList<TechnologyOption>> GetTechnologyOptionsAsync(int buildingBlockId, string? search = null, int? familyId = null, CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection(); await EnsureOpenAsync(connection, cancellationToken);
        return await ReadTechnologyOptionsAsync(connection, buildingBlockId, search, familyId, cancellationToken);
    }

    public async Task<FamilyBuildingBlockReport> GetFamilyBuildingBlockReportAsync(int? familyId = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        var buckets = new List<FamilyBuildingBlockBucket>();
        await using (var bucketCommand = Create(connection, null, @"
SELECT f.idFamilia, f.nombreFamilia, COUNT(DISTINCT bridge.idBuildingBlock)
FROM dbo.TMFamilia f
JOIN dbo.TTecnologiaTSI technology ON technology.idFamilia = f.idFamilia
JOIN dbo.TBuildingBlockVsTTecnologiaTSI bridge ON bridge.idTecnologiaTSI = technology.idTecnologiaTSI
GROUP BY f.idFamilia, f.nombreFamilia
ORDER BY f.nombreFamilia"))
        await using (var bucketReader = await bucketCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await bucketReader.ReadAsync(cancellationToken))
                buckets.Add(new FamilyBuildingBlockBucket(bucketReader.GetInt32(0), bucketReader.GetString(1), bucketReader.GetInt32(2)));
        }

        var withoutTechnology = await ScalarAsync(connection, null, "SELECT COUNT(*) FROM dbo.TBuildingBlock b WHERE NOT EXISTS (SELECT 1 FROM dbo.TBuildingBlockVsTTecnologiaTSI bridge WHERE bridge.idBuildingBlock = b.idBuildingBlock)", cancellationToken);
        var withoutBuildingBlock = await ScalarAsync(connection, null, "SELECT COUNT(*) FROM dbo.TTecnologiaTSI technology WHERE NOT EXISTS (SELECT 1 FROM dbo.TBuildingBlockVsTTecnologiaTSI bridge WHERE bridge.idTecnologiaTSI = technology.idTecnologiaTSI)", cancellationToken);
        var multipleFamilies = await ScalarAsync(connection, null, @"
SELECT COUNT(*) FROM (
    SELECT bridge.idBuildingBlock
    FROM dbo.TBuildingBlockVsTTecnologiaTSI bridge
    JOIN dbo.TTecnologiaTSI technology ON technology.idTecnologiaTSI = bridge.idTecnologiaTSI
    WHERE technology.idFamilia IS NOT NULL
    GROUP BY bridge.idBuildingBlock
    HAVING COUNT(DISTINCT technology.idFamilia) > 1
) blocks", cancellationToken);

        string? selectedFamilyName = null;
        if (familyId.HasValue)
        {
            await using var familyCommand = Create(connection, null, "SELECT nombreFamilia FROM dbo.TMFamilia WHERE idFamilia=@familyId");
            Add(familyCommand, "@familyId", familyId.Value);
            selectedFamilyName = Convert.ToString(await familyCommand.ExecuteScalarAsync(cancellationToken));
        }

        var rows = new List<FamilyBuildingBlockRow>();
        if (familyId.HasValue && selectedFamilyName is not null)
        {
            await using var rowCommand = Create(connection, null, @"
WITH distinctRelations AS (
    SELECT DISTINCT bridge.idBuildingBlock, b.nombreBuildingBlock,
           technology.idTecnologiaTSI,
           COALESCE(technology.[nombreTecnologiaAlternativa1-Corporativo], N'') AS technologyName
    FROM dbo.TBuildingBlockVsTTecnologiaTSI bridge
    JOIN dbo.TBuildingBlock b ON b.idBuildingBlock = bridge.idBuildingBlock
    JOIN dbo.TTecnologiaTSI technology ON technology.idTecnologiaTSI = bridge.idTecnologiaTSI
    WHERE technology.idFamilia = @familyId
), grouped AS (
    SELECT idBuildingBlock, nombreBuildingBlock,
           COUNT(*) AS technologyCount,
           STRING_AGG(CONVERT(nvarchar(max), technologyName), N'||') WITHIN GROUP (ORDER BY technologyName) AS technologyNames
    FROM distinctRelations
    GROUP BY idBuildingBlock, nombreBuildingBlock
)
SELECT idBuildingBlock, nombreBuildingBlock, technologyCount, technologyNames
FROM grouped
ORDER BY nombreBuildingBlock
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY");
            Add(rowCommand, "@familyId", familyId.Value);
            Add(rowCommand, "@offset", (page - 1) * pageSize);
            Add(rowCommand, "@pageSize", pageSize);
            await using var rowReader = await rowCommand.ExecuteReaderAsync(cancellationToken);
            while (await rowReader.ReadAsync(cancellationToken))
            {
                var names = rowReader.IsDBNull(3) ? [] : rowReader.GetString(3).Split("||", StringSplitOptions.RemoveEmptyEntries).ToArray();
                rows.Add(new FamilyBuildingBlockRow(rowReader.GetInt32(0), rowReader.GetString(1), names, rowReader.GetInt32(2), string.Empty));
            }
        }

        var selectedTotal = familyId.HasValue ? await TotalForFamilyAsync(connection, familyId.Value, cancellationToken) : 0;
        return new FamilyBuildingBlockReport(buckets, rows, new FamilyBuildingBlockIndicators(withoutTechnology, withoutBuildingBlock, multipleFamilies), familyId, selectedFamilyName, page, pageSize, selectedTotal);
    }

    private static async Task<int> TotalForFamilyAsync(DbConnection connection, int familyId, CancellationToken cancellationToken)
        => await ScalarAsync(connection, null, @"
SELECT COUNT(*) FROM (
    SELECT bridge.idBuildingBlock
    FROM dbo.TBuildingBlockVsTTecnologiaTSI bridge
    JOIN dbo.TTecnologiaTSI technology ON technology.idTecnologiaTSI = bridge.idTecnologiaTSI
    WHERE technology.idFamilia = @familyId
    GROUP BY bridge.idBuildingBlock
) blocks", cancellationToken, ("@familyId", familyId));

    public Task SaveTechnologyRelationsAsync(int technologyId, IReadOnlyCollection<int> buildingBlockIds, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => SaveAsync(technologyId, buildingBlockIds, true, actorUserId, correlationId, cancellationToken);
    public Task SaveBuildingBlockRelationsAsync(int buildingBlockId, IReadOnlyCollection<int> technologyIds, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => SaveAsync(buildingBlockId, technologyIds, false, actorUserId, correlationId, cancellationToken);
    public Task AssociateAsync(int buildingBlockId, int technologyId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
        => ChangeSingleRelationAsync(buildingBlockId, technologyId, true, actorUserId, correlationId, cancellationToken);
    public Task DisassociateAsync(int buildingBlockId, int technologyId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
        => ChangeSingleRelationAsync(buildingBlockId, technologyId, false, actorUserId, correlationId, cancellationToken);

    private async Task ChangeSingleRelationAsync(int buildingBlockId, int technologyId, bool associate, Guid actorUserId, string correlationId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var connection = dbContext.Database.GetDbConnection(); await EnsureOpenAsync(connection, cancellationToken);
        var buildingExists = await ScalarAsync(connection, transaction.GetDbTransaction(), "SELECT COUNT(*) FROM dbo.TBuildingBlock WHERE idBuildingBlock=@id", cancellationToken, ("@id", buildingBlockId));
        var technologyExists = await ScalarAsync(connection, transaction.GetDbTransaction(), "SELECT COUNT(*) FROM dbo.TTecnologiaTSI WHERE idTecnologiaTSI=@id", cancellationToken, ("@id", technologyId));
        if (buildingExists != 1 || technologyExists != 1) throw new TechnologyMappingConflictException("El Building Block o la Tecnología TSI no existe.");
        var count = await ScalarAsync(connection, transaction.GetDbTransaction(), "SELECT COUNT(*) FROM dbo.TBuildingBlockVsTTecnologiaTSI WHERE idBuildingBlock=@building AND idTecnologiaTSI=@technology", cancellationToken, ("@building", buildingBlockId), ("@technology", technologyId));
        if (count > 1) throw new TechnologyMappingConflictException("Existen relaciones duplicadas. La operación fue bloqueada sin modificar datos.");
        if ((associate && count == 1) || (!associate && count == 0)) { await transaction.CommitAsync(cancellationToken); return; }
        await using var command = Create(connection, transaction.GetDbTransaction(), associate
            ? "INSERT INTO dbo.TBuildingBlockVsTTecnologiaTSI (idBuildingBlock,idTecnologiaTSI) VALUES (@building,@technology)"
            : "DELETE FROM dbo.TBuildingBlockVsTTecnologiaTSI WHERE idBuildingBlock=@building AND idTecnologiaTSI=@technology");
        Add(command, "@building", buildingBlockId); Add(command, "@technology", technologyId); await command.ExecuteNonQueryAsync(cancellationToken);
        AddAudit(actorUserId, buildingBlockId, technologyId, associate ? "BuildingBlockTechnology.Associated" : "BuildingBlockTechnology.Disassociated", correlationId);
        await auditTrail.RecordRelationAsync(associate ? "RELATION_ADD" : "RELATION_REMOVE", "bridge-building-technology", "TBuildingBlockVsTTecnologiaTSI", buildingBlockId, null, actorUserId, correlationId,
            associate ? "Asociación Building Block/Tecnología creada." : "Asociación Building Block/Tecnología eliminada.", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
    }

    private async Task SaveAsync(int rootId, IReadOnlyCollection<int> requestedIds, bool rootIsTechnology, Guid actorUserId, string correlationId, CancellationToken cancellationToken)
    {
        try
        {
            var desired = requestedIds.Distinct().ToHashSet();
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var connection = dbContext.Database.GetDbConnection(); await EnsureOpenAsync(connection, cancellationToken);
            var existing = new HashSet<int>();
            await using (var command = Create(connection, transaction.GetDbTransaction(), rootIsTechnology ? "SELECT idBuildingBlock FROM dbo.TBuildingBlockVsTTecnologiaTSI WHERE idTecnologiaTSI=@id" : "SELECT idTecnologiaTSI FROM dbo.TBuildingBlockVsTTecnologiaTSI WHERE idBuildingBlock=@id"))
            {
                Add(command, "@id", rootId);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    if (!existing.Add(reader.GetInt32(0)))
                    {
                        throw new TechnologyMappingConflictException(
                            "Existen relaciones duplicadas. La operación fue bloqueada sin modificar datos.");
                    }
                }
            }
            foreach (var id in desired.Except(existing))
            {
                await using var command = Create(connection, transaction.GetDbTransaction(), "INSERT INTO dbo.TBuildingBlockVsTTecnologiaTSI (idBuildingBlock,idTecnologiaTSI) VALUES (@building,@technology)");
                Add(command, "@building", rootIsTechnology ? id : rootId); Add(command, "@technology", rootIsTechnology ? rootId : id); await command.ExecuteNonQueryAsync(cancellationToken);
                AddAudit(actorUserId, rootIsTechnology ? id : rootId, rootIsTechnology ? rootId : id, "BuildingBlockTechnology.Associated", correlationId);
                await auditTrail.RecordRelationAsync("RELATION_ADD", "bridge-building-technology", "TBuildingBlockVsTTecnologiaTSI",
                    rootIsTechnology ? id : rootId, null, actorUserId, correlationId,
                    $"Asociación Building Block/Tecnología creada: {(rootIsTechnology ? id : rootId)}:{(rootIsTechnology ? rootId : id)}.", cancellationToken);
            }
            foreach (var id in existing.Except(desired))
            {
                await using var command = Create(connection, transaction.GetDbTransaction(), "DELETE FROM dbo.TBuildingBlockVsTTecnologiaTSI WHERE idBuildingBlock=@building AND idTecnologiaTSI=@technology");
                Add(command, "@building", rootIsTechnology ? id : rootId); Add(command, "@technology", rootIsTechnology ? rootId : id); await command.ExecuteNonQueryAsync(cancellationToken);
                AddAudit(actorUserId, rootIsTechnology ? id : rootId, rootIsTechnology ? rootId : id, "BuildingBlockTechnology.Disassociated", correlationId);
                await auditTrail.RecordRelationAsync("RELATION_REMOVE", "bridge-building-technology", "TBuildingBlockVsTTecnologiaTSI",
                    rootIsTechnology ? id : rootId, null, actorUserId, correlationId,
                    $"Asociación Building Block/Tecnología eliminada: {(rootIsTechnology ? id : rootId)}:{(rootIsTechnology ? rootId : id)}.", cancellationToken);
            }
            await dbContext.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        }
        catch (DbException exception)
        {
            throw new TechnologyMappingConflictException(
                "La relación cambió mientras se procesaba la solicitud. Recargue e inténtelo nuevamente.", exception);
        }
        catch (DbUpdateException exception)
        {
            throw new TechnologyMappingConflictException(
                "La relación cambió mientras se procesaba la solicitud. Recargue e inténtelo nuevamente.", exception);
        }
    }

    private void AddAudit(Guid actor, int buildingBlockId, int technologyId, string type, string correlation) => dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion { ActorUserId = actor, EventType = type, Result = "Succeeded", PermissionCode = Permissions.CatalogEdit, ResourceType = "TBuildingBlockVsTTecnologiaTSI", ResourceId = $"{buildingBlockId}:{technologyId}", AfterJson = JsonSerializer.Serialize(new { buildingBlockId, technologyId }), CorrelationId = correlation, Justification = "Administración del mapeo Building Block/Tecnología TSI." });

    private static async Task<TechnologyMappingKpis> GetKpisAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var total = await ScalarAsync(connection, null, "SELECT COUNT(*) FROM dbo.TTecnologiaTSI", cancellationToken);
        var mapped = await ScalarAsync(connection, null, "SELECT COUNT(DISTINCT idTecnologiaTSI) FROM dbo.TBuildingBlockVsTTecnologiaTSI", cancellationToken);
        var bbTotal = await ScalarAsync(connection, null, "SELECT COUNT(*) FROM dbo.TBuildingBlock", cancellationToken);
        var bbMapped = await ScalarAsync(connection, null, "SELECT COUNT(DISTINCT idBuildingBlock) FROM dbo.TBuildingBlockVsTTecnologiaTSI", cancellationToken);
        var relations = await ScalarAsync(connection, null, "SELECT COUNT(*) FROM dbo.TBuildingBlockVsTTecnologiaTSI", cancellationToken);
        return new TechnologyMappingKpis(total, mapped, total - mapped, bbTotal, bbMapped, bbTotal - bbMapped, relations, bbTotal == 0 ? 0 : Math.Round(bbMapped * 100m / bbTotal, 2));
    }

    private static async Task<List<BuildingBlockOption>> ReadBlocksAsync(DbConnection connection, CancellationToken cancellationToken, int? technologyId = null)
    {
        var list = new List<BuildingBlockOption>(); await using var command = Create(connection, null, "SELECT b.idBuildingBlock,b.nombreBuildingBlock,CASE WHEN @technologyId IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.TBuildingBlockVsTTecnologiaTSI x WHERE x.idBuildingBlock=b.idBuildingBlock AND x.idTecnologiaTSI=@technologyId) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END FROM dbo.TBuildingBlock b ORDER BY b.nombreBuildingBlock"); Add(command, "@technologyId", (object?)technologyId ?? DBNull.Value); await using var reader = await command.ExecuteReaderAsync(cancellationToken); while (await reader.ReadAsync(cancellationToken)) list.Add(new BuildingBlockOption(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2), string.Empty)); return list;
    }
    private static async Task<List<FamilyOption>> ReadFamiliesAsync(DbConnection connection, CancellationToken cancellationToken) { var list = new List<FamilyOption>(); await using var command = Create(connection, null, "SELECT idFamilia,nombreFamilia FROM dbo.TMFamilia ORDER BY nombreFamilia"); await using var reader = await command.ExecuteReaderAsync(cancellationToken); while (await reader.ReadAsync(cancellationToken)) list.Add(new FamilyOption(reader.GetInt32(0), reader.GetString(1))); return list; }
    private static async Task<List<DomainOption>> ReadDomainsAsync(DbConnection connection, CancellationToken cancellationToken) { var list = new List<DomainOption>(); await using var command = Create(connection, null, "SELECT iddominio,dominio FROM dbo.TMDominio ORDER BY dominio"); await using var reader = await command.ExecuteReaderAsync(cancellationToken); while (await reader.ReadAsync(cancellationToken)) list.Add(new DomainOption(reader.GetInt32(0), reader.GetString(1))); return list; }
    private static async Task<List<PhaseOption>> ReadPhasesAsync(DbConnection connection, CancellationToken cancellationToken) { var list = new List<PhaseOption>(); await using var command = Create(connection, null, "SELECT idEstadoFaseAdopcion,nombreFaseAdopcion FROM dbo.TEstadoFaseAdopcion ORDER BY nombreFaseAdopcion"); await using var reader = await command.ExecuteReaderAsync(cancellationToken); while (await reader.ReadAsync(cancellationToken)) list.Add(new PhaseOption(reader.GetInt32(0), reader.GetString(1))); return list; }
    private static async Task<List<TechnologyOption>> ReadTechnologyOptionsAsync(DbConnection connection, int buildingId, string? search, int? familyId, CancellationToken cancellationToken) { var list = new List<TechnologyOption>(); await using var command = Create(connection, null, "SELECT t.idTecnologiaTSI,COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo],N''),f.nombreFamilia,a.nombreEstadoAdopcionTSI,CASE WHEN EXISTS (SELECT 1 FROM dbo.TBuildingBlockVsTTecnologiaTSI x WHERE x.idBuildingBlock=@building AND x.idTecnologiaTSI=t.idTecnologiaTSI) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END FROM dbo.TTecnologiaTSI t LEFT JOIN dbo.TMFamilia f ON f.idFamilia=t.idFamilia LEFT JOIN dbo.TMEstadoAdopcionTSI a ON a.idEstadoAdopcionTSI=t.idEstadoAdopcionTSI WHERE (@search=N'%%' OR COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo],N'') LIKE @search) AND (@family IS NULL OR t.idFamilia=@family) ORDER BY t.[nombreTecnologiaAlternativa1-Corporativo]"); Add(command, "@building", buildingId); Add(command, "@search", $"%{search?.Trim() ?? string.Empty}%"); Add(command, "@family", (object?)familyId ?? DBNull.Value); await using var reader = await command.ExecuteReaderAsync(cancellationToken); while (await reader.ReadAsync(cancellationToken)) list.Add(new TechnologyOption(reader.GetInt32(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3), reader.GetBoolean(4), string.Empty)); return list; }
    private static DbCommand Create(DbConnection connection, DbTransaction? transaction, string sql, CancellationToken cancellationToken = default) { var command = connection.CreateCommand(); command.CommandText = sql; command.Transaction = transaction; return command; }
    private static void Add(DbCommand command, string name, object value) { var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value; command.Parameters.Add(parameter); }
    private static async Task<int> ScalarAsync(DbConnection connection, DbTransaction? transaction, string sql, CancellationToken cancellationToken, params (string Name, object Value)[] parameters) { await using var command = Create(connection, transaction, sql); foreach (var parameter in parameters) Add(command, parameter.Name, parameter.Value); return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)); }
    private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken cancellationToken) { if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken); }
}
