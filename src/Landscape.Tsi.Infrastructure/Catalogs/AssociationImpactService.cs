using System.Data;
using System.Data.Common;

using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Catalogs;

public sealed class AssociationImpactService(IdentityDbContext dbContext) : IAssociationImpactService
{
    public async Task<CapabilityReassignmentImpactDto?> PreviewCapabilityReassignmentAsync(
        int capabilityId,
        int targetBuildingBlockId,
        CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        // 1. Fetch capability
        const string capSql = @"
SELECT c.idCapacidad, c.nombreCapacidad, c.idBuildingBlock, c.idEstadoCapacidad, b.nombreBuildingBlock
FROM dbo.TCapacidadDeSeguridad c
LEFT JOIN dbo.TBuildingBlock b ON b.idBuildingBlock = c.idBuildingBlock
WHERE c.idCapacidad = @id";
        await using var capCommand = Create(connection, capSql, ("@id", capabilityId));
        await using var capReader = await capCommand.ExecuteReaderAsync(cancellationToken);
        if (!await capReader.ReadAsync(cancellationToken)) return null;

        var capName = capReader.GetString(1);
        var sourceBbId = capReader.IsDBNull(2) ? (int?)null : capReader.GetInt32(2);
        var stateId = capReader.IsDBNull(3) ? (int?)null : capReader.GetInt32(3);
        var sourceBbName = capReader.IsDBNull(4) ? null : capReader.GetString(4);
        await capReader.CloseAsync();

        // 2. Fetch target building block
        const string targetBbSql = "SELECT nombreBuildingBlock FROM dbo.TBuildingBlock WHERE idBuildingBlock = @id";
        await using var targetBbCommand = Create(connection, targetBbSql, ("@id", targetBuildingBlockId));
        var targetBbNameObj = await targetBbCommand.ExecuteScalarAsync(cancellationToken);
        if (targetBbNameObj is null or DBNull) return null;
        var targetBbName = (string)targetBbNameObj;

        // 3. Child functionalities
        const string childFuncSql = "SELECT nombreFuncionalidad FROM dbo.TFuncionalidad WHERE idCapacidad = @id ORDER BY nombreFuncionalidad";
        await using var childCommand = Create(connection, childFuncSql, ("@id", capabilityId));
        await using var childReader = await childCommand.ExecuteReaderAsync(cancellationToken);
        var childFuncs = new List<string>();
        while (await childReader.ReadAsync(cancellationToken))
        {
            childFuncs.Add(childReader.GetString(0));
        }
        await childReader.CloseAsync();

        // 4. Source BB related technologies
        var relatedTechs = new List<string>();
        if (sourceBbId.HasValue)
        {
            const string techSql = @"
SELECT DISTINCT COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo], N'')
FROM dbo.TBuildingBlockVsTTecnologiaTSI bridge
JOIN dbo.TTecnologiaTSI t ON t.idTecnologiaTSI = bridge.idTecnologiaTSI
WHERE bridge.idBuildingBlock = @sourceBbId
ORDER BY 1";
            await using var techCommand = Create(connection, techSql, ("@sourceBbId", sourceBbId.Value));
            await using var techReader = await techCommand.ExecuteReaderAsync(cancellationToken);
            while (await techReader.ReadAsync(cancellationToken))
            {
                var techName = techReader.GetString(0);
                if (!string.IsNullOrWhiteSpace(techName)) relatedTechs.Add(techName);
            }
            await techReader.CloseAsync();
        }

        // 5. Warnings
        var warnings = new List<string>();
        if (sourceBbId == targetBuildingBlockId)
        {
            warnings.Add("La Capacidad de Seguridad ya se encuentra asignada a este Building Block.");
        }
        if (childFuncs.Count > 0)
        {
            warnings.Add($"Se trasladará indirectamente el contexto de {childFuncs.Count} funcionalidad(es) dependiente(s).");
        }
        if (relatedTechs.Count > 0)
        {
            warnings.Add($"El Building Block origen posee {relatedTechs.Count} tecnología(s) TSI asociadas.");
        }

        var token = AssignmentConcurrencyHelper.CreateCapabilityToken(capabilityId, sourceBbId, capName, stateId);

        return new CapabilityReassignmentImpactDto(
            capabilityId,
            capName,
            sourceBbId,
            sourceBbName,
            targetBuildingBlockId,
            targetBbName,
            childFuncs.Count,
            childFuncs,
            relatedTechs,
            warnings,
            token);
    }

    public async Task<FunctionalityReassignmentImpactDto?> PreviewFunctionalityReassignmentAsync(
        int functionalityId,
        int targetCapabilityId,
        CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        // 1. Fetch functionality
        const string funcSql = @"
SELECT f.idFuncionalidad, f.nombreFuncionalidad, f.idCapacidad, f.idEstadoCoberturaFuncionalidad,
       c.nombreCapacidad, c.idBuildingBlock, b.nombreBuildingBlock
FROM dbo.TFuncionalidad f
LEFT JOIN dbo.TCapacidadDeSeguridad c ON c.idCapacidad = f.idCapacidad
LEFT JOIN dbo.TBuildingBlock b ON b.idBuildingBlock = c.idBuildingBlock
WHERE f.idFuncionalidad = @id";
        await using var funcCommand = Create(connection, funcSql, ("@id", functionalityId));
        await using var funcReader = await funcCommand.ExecuteReaderAsync(cancellationToken);
        if (!await funcReader.ReadAsync(cancellationToken)) return null;

        var funcName = funcReader.GetString(1);
        var sourceCapId = funcReader.IsDBNull(2) ? (int?)null : funcReader.GetInt32(2);
        var stateId = funcReader.IsDBNull(3) ? (int?)null : funcReader.GetInt32(3);
        var sourceCapName = funcReader.IsDBNull(4) ? null : funcReader.GetString(4);
        var sourceBbId = funcReader.IsDBNull(5) ? (int?)null : funcReader.GetInt32(5);
        var sourceBbName = funcReader.IsDBNull(6) ? null : funcReader.GetString(6);
        await funcReader.CloseAsync();

        // 2. Fetch target capability & its building block
        const string targetCapSql = @"
SELECT c.idCapacidad, c.nombreCapacidad, c.idBuildingBlock, b.nombreBuildingBlock
FROM dbo.TCapacidadDeSeguridad c
LEFT JOIN dbo.TBuildingBlock b ON b.idBuildingBlock = c.idBuildingBlock
WHERE c.idCapacidad = @id";
        await using var targetCapCommand = Create(connection, targetCapSql, ("@id", targetCapabilityId));
        await using var targetCapReader = await targetCapCommand.ExecuteReaderAsync(cancellationToken);
        if (!await targetCapReader.ReadAsync(cancellationToken)) return null;

        var targetCapName = targetCapReader.GetString(1);
        var targetBbId = targetCapReader.IsDBNull(2) ? (int?)null : targetCapReader.GetInt32(2);
        var targetBbName = targetCapReader.IsDBNull(3) ? null : targetCapReader.GetString(3);
        await targetCapReader.CloseAsync();

        var isCross = sourceBbId != targetBbId;
        var warnings = new List<string>();
        if (sourceCapId == targetCapabilityId)
        {
            warnings.Add("La funcionalidad ya se encuentra asignada a esta Capacidad de Seguridad.");
        }
        if (isCross)
        {
            warnings.Add($"La reasignación cambiará de Building Block: pasará de '{sourceBbName ?? "Sin Asignar"}' a '{targetBbName ?? "Sin Asignar"}'.");
        }
        else
        {
            warnings.Add("La reasignación se realiza dentro del mismo Building Block.");
        }

        var token = AssignmentConcurrencyHelper.CreateFunctionalityToken(functionalityId, sourceCapId, funcName, stateId);

        return new FunctionalityReassignmentImpactDto(
            functionalityId,
            funcName,
            sourceCapId,
            sourceCapName,
            sourceBbId,
            sourceBbName,
            targetCapabilityId,
            targetCapName,
            targetBbId,
            targetBbName,
            isCross,
            warnings,
            token);
    }

    public async Task<IReadOnlyList<CapabilityCandidateDto>> GetCapabilityCandidatesAsync(
        int buildingBlockId,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        var searchPattern = string.IsNullOrWhiteSpace(search) ? "%" : $"%{search.Trim()}%";
        const string sql = @"
SELECT c.idCapacidad, c.nombreCapacidad, s.nombreEstadoCapacidad, c.idBuildingBlock, b.nombreBuildingBlock
FROM dbo.TCapacidadDeSeguridad c
LEFT JOIN dbo.TMEstadoCapacidad s ON s.idEstadoCapacidad = c.idEstadoCapacidad
LEFT JOIN dbo.TBuildingBlock b ON b.idBuildingBlock = c.idBuildingBlock
WHERE (@search = N'%' OR c.nombreCapacidad LIKE @search)
ORDER BY c.nombreCapacidad ASC";

        await using var command = Create(connection, sql, ("@search", searchPattern));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var list = new List<CapabilityCandidateDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var state = reader.IsDBNull(2) ? null : reader.GetString(2);
            var currentBbId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
            var currentBbName = reader.IsDBNull(4) ? null : reader.GetString(4);

            var status = currentBbId == null
                ? HierarchyAssignmentStatus.Unassigned
                : (currentBbId == buildingBlockId
                    ? HierarchyAssignmentStatus.AssignedToCurrent
                    : HierarchyAssignmentStatus.AssignedToOther);

            list.Add(new CapabilityCandidateDto(id, name, state, currentBbId, currentBbName, status));
        }
        return list;
    }

    public async Task<IReadOnlyList<FunctionalityCandidateDto>> GetFunctionalityCandidatesAsync(
        int buildingBlockId,
        int? capabilityId = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        var searchPattern = string.IsNullOrWhiteSpace(search) ? "%" : $"%{search.Trim()}%";
        const string sql = @"
SELECT f.idFuncionalidad, f.nombreFuncionalidad, s.nombreEstadoFuncionalidad,
       f.idCapacidad, c.nombreCapacidad, c.idBuildingBlock, b.nombreBuildingBlock
FROM dbo.TFuncionalidad f
LEFT JOIN dbo.TMEstadoFuncionalidad s ON s.idEstadoCoberturaFuncionalidad = f.idEstadoCoberturaFuncionalidad
LEFT JOIN dbo.TCapacidadDeSeguridad c ON c.idCapacidad = f.idCapacidad
LEFT JOIN dbo.TBuildingBlock b ON b.idBuildingBlock = c.idBuildingBlock
WHERE (@search = N'%' OR f.nombreFuncionalidad LIKE @search)
ORDER BY f.nombreFuncionalidad ASC";

        await using var command = Create(connection, sql, ("@search", searchPattern));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var list = new List<FunctionalityCandidateDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var state = reader.IsDBNull(2) ? null : reader.GetString(2);
            var currentCapId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
            var currentCapName = reader.IsDBNull(4) ? null : reader.GetString(4);
            var currentBbId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5);
            var currentBbName = reader.IsDBNull(6) ? null : reader.GetString(6);

            HierarchyAssignmentStatus status;
            if (currentCapId == null)
            {
                status = HierarchyAssignmentStatus.Unassigned;
            }
            else if (capabilityId.HasValue)
            {
                status = currentCapId == capabilityId.Value
                    ? HierarchyAssignmentStatus.AssignedToCurrent
                    : HierarchyAssignmentStatus.AssignedToOther;
            }
            else
            {
                status = currentBbId == buildingBlockId
                    ? HierarchyAssignmentStatus.AssignedToCurrent
                    : HierarchyAssignmentStatus.AssignedToOther;
            }

            list.Add(new FunctionalityCandidateDto(id, name, state, currentCapId, currentCapName, currentBbId, currentBbName, status));
        }
        return list;
    }

    public async Task<string?> GetCapabilityConcurrencyTokenAsync(int capabilityId, CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);
        const string sql = "SELECT idCapacidad, idBuildingBlock, nombreCapacidad, idEstadoCapacidad FROM dbo.TCapacidadDeSeguridad WHERE idCapacidad = @id";
        await using var command = Create(connection, sql, ("@id", capabilityId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var id = reader.GetInt32(0);
        var bbId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
        var name = reader.GetString(2);
        var stateId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
        return AssignmentConcurrencyHelper.CreateCapabilityToken(id, bbId, name, stateId);
    }

    public async Task<string?> GetFunctionalityConcurrencyTokenAsync(int functionalityId, CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);
        const string sql = "SELECT idFuncionalidad, idCapacidad, nombreFuncionalidad, idEstadoCoberturaFuncionalidad FROM dbo.TFuncionalidad WHERE idFuncionalidad = @id";
        await using var command = Create(connection, sql, ("@id", functionalityId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var id = reader.GetInt32(0);
        var capId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
        var name = reader.GetString(2);
        var stateId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
        return AssignmentConcurrencyHelper.CreateFunctionalityToken(id, capId, name, stateId);
    }

    private static DbCommand Create(DbConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var p = command.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            command.Parameters.Add(p);
        }
        return command;
    }

    private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }
}