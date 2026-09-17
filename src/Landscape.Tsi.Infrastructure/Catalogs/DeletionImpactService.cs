using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;

using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace Landscape.Tsi.Infrastructure.Catalogs;

public sealed class DeletionImpactService(IdentityDbContext dbContext, IConfiguration configuration, IAuditTrailService auditTrail) : IDeletionImpactService
{
    private const string DomainCode = "dominio";
    private const string DomainTable = "TMDominio";
    private const string BuildingBlockCode = "building-block";
    private const string BuildingBlockTable = "TBuildingBlock";
    private const string FunctionalityCode = "funcionalidad";
    private const string FunctionalityTable = "TFuncionalidad";
    private const string CapacityCode = "capacidad-seguridad";
    private const string CapacityTable = "TCapacidadDeSeguridad";
    private readonly int typedConfirmationThreshold = configuration.GetValue("DeletionSafety:RequireTypedConfirmationAbove", 50);

    public async Task<DeletionImpactResult?> PreviewAsync(string entityCode, int rootId, CancellationToken cancellationToken = default)
    {
        EnsureSupported(entityCode);
        await OpenConnectionAsync(cancellationToken);
        try
        {
            var displayName = await ReadRootNameAsync(entityCode, rootId, cancellationToken);
            if (displayName is null) return null;
            return await BuildImpactAsync(entityCode, rootId, displayName, cancellationToken);
        }
        finally
        {
            await CloseConnectionAsync();
        }
    }

    public async Task<DeletionExecutionResult> DeleteAsync(string entityCode, int rootId, string? confirmation, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        EnsureSupported(entityCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        await OpenConnectionAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var displayName = await ReadRootNameAsync(entityCode, rootId, cancellationToken);
            if (displayName is null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return new DeletionExecutionResult(false, 0, "El registro ya no existe.");
            }

            var impact = await BuildImpactAsync(entityCode, rootId, displayName, cancellationToken);
            if (!impact.CanDelete)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return new DeletionExecutionResult(false, 0, impact.BlockingReason);
            }
            if (impact.RequiresTypedConfirmation && !string.Equals(confirmation, "ELIMINAR", StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return new DeletionExecutionResult(false, 0, "Se requiere escribir ELIMINAR para confirmar una operación de alto impacto.");
            }

            var definition = MasterCatalogRegistry.GetByCode(entityCode)!;
            var operation = await auditTrail.BeginDeleteAsync(
                entityCode,
                definition.PhysicalTable,
                rootId,
                displayName,
                actorUserId,
                correlationId,
                impact.TotalRecordsToDelete,
                $"{definition.Name} {displayName} eliminado. Impacto: {impact.TotalRecordsToDelete} registros.",
                cancellationToken);
            await CaptureDeleteSnapshotsAsync(operation, entityCode, rootId, displayName, cancellationToken);

            var deleted = 0;
            if (entityCode is DomainCode or BuildingBlockCode or CapacityCode or FunctionalityCode)
            {
                if (entityCode == FunctionalityCode)
                {
                    deleted += await ExecuteAsync("DELETE FROM [dbo].[TFuncionalidad] WHERE [idFuncionalidad] = @rootId", rootId, cancellationToken);
                }
                else if (entityCode == CapacityCode)
                {
                    deleted += await ExecuteAsync("DELETE FROM [dbo].[TFuncionalidad] WHERE [idCapacidad] = @rootId", rootId, cancellationToken);
                    deleted += await ExecuteAsync("DELETE FROM [dbo].[TCapacidadDeSeguridad] WHERE [idCapacidad] = @rootId", rootId, cancellationToken);
                }
                else if (entityCode == DomainCode)
                {
                    deleted += await ExecuteAsync("DELETE f FROM [dbo].[TFuncionalidad] f INNER JOIN [dbo].[TCapacidadDeSeguridad] c ON c.[idCapacidad] = f.[idCapacidad] INNER JOIN [dbo].[TBuildingBlock] b ON b.[idBuildingBlock] = c.[idBuildingBlock] WHERE b.[idDominio] = @rootId", rootId, cancellationToken);
                    deleted += await ExecuteAsync("DELETE c FROM [dbo].[TCapacidadDeSeguridad] c INNER JOIN [dbo].[TBuildingBlock] b ON b.[idBuildingBlock] = c.[idBuildingBlock] WHERE b.[idDominio] = @rootId", rootId, cancellationToken);
                    deleted += await ExecuteAsync("DELETE bridge FROM [dbo].[TBuildingBlockVsTTecnologiaTSI] bridge INNER JOIN [dbo].[TBuildingBlock] b ON b.[idBuildingBlock] = bridge.[idBuildingBlock] WHERE b.[idDominio] = @rootId", rootId, cancellationToken);
                    deleted += await ExecuteAsync("DELETE b FROM [dbo].[TBuildingBlock] b WHERE b.[idDominio] = @rootId", rootId, cancellationToken);
                    deleted += await ExecuteAsync("DELETE FROM [dbo].[TMDominio] WHERE [iddominio] = @rootId", rootId, cancellationToken);
                }
                else
                {
                    deleted += await ExecuteAsync("DELETE f FROM [dbo].[TFuncionalidad] f INNER JOIN [dbo].[TCapacidadDeSeguridad] c ON c.[idCapacidad] = f.[idCapacidad] WHERE c.[idBuildingBlock] = @rootId", rootId, cancellationToken);
                    deleted += await ExecuteAsync("DELETE c FROM [dbo].[TCapacidadDeSeguridad] c WHERE c.[idBuildingBlock] = @rootId", rootId, cancellationToken);
                    deleted += await ExecuteAsync("DELETE bridge FROM [dbo].[TBuildingBlockVsTTecnologiaTSI] bridge WHERE bridge.[idBuildingBlock] = @rootId", rootId, cancellationToken);
                    deleted += await ExecuteAsync("DELETE FROM [dbo].[TBuildingBlock] WHERE [idBuildingBlock] = @rootId", rootId, cancellationToken);
                }
            }
            else
            {
                deleted = await ExecuteGenericDeleteAsync(definition, rootId, cancellationToken);
            }

            dbContext.AuthorizationAuditEvents.Add(new Domain.Identity.IamEventoAuditoriaAutorizacion
            {
                ActorUserId = actorUserId,
                EventType = "Catalog.DeletedCascade",
                PermissionCode = Permissions.CatalogDelete,
                Result = "Succeeded",
                ResourceType = entityCode,
                ResourceId = rootId.ToString(CultureInfo.InvariantCulture),
                AfterJson = JsonSerializer.Serialize(new { RootDisplayName = displayName, TotalRecordsDeleted = deleted, RootTable = definition.PhysicalTable }),
                CorrelationId = correlationId
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new DeletionExecutionResult(true, deleted);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            await CloseConnectionAsync();
        }
    }

    private async Task<DeletionImpactResult> BuildImpactAsync(string entityCode, int rootId, string displayName, CancellationToken cancellationToken)
    {
        if (entityCode is DomainCode or BuildingBlockCode or CapacityCode or FunctionalityCode)
        {
            var counts = await ReadCountsAsync(entityCode, rootId, cancellationToken);
            return BuildLegacyImpact(entityCode, rootId, displayName, counts);
        }

        var definition = MasterCatalogRegistry.GetByCode(entityCode)!;
        var dependents = await DiscoverDependentsAsync(definition, rootId, cancellationToken);
        return DeletionImpactCalculator.Calculate(definition.Code, definition.PhysicalTable, rootId, displayName, dependents, typedConfirmationThreshold);
    }

    private DeletionImpactResult BuildLegacyImpact(string entityCode, int rootId, string displayName, DependencyCounts counts)
    {
        var functionalityNode = new DeletionDependencyNode("Funcionalidad", "TFuncionalidad", counts.Functionalities, 3, "Capacidad de Seguridad → Funcionalidad", []);
        var capacity = new DeletionDependencyNode("Capacidad de Seguridad", "TCapacidadDeSeguridad", counts.Capacities, 2, "Building Block → Capacidad de Seguridad", [functionalityNode]);
        if (entityCode == BuildingBlockCode)
        {
            var bridgeForBuilding = new DeletionDependencyNode("Relaciones Building Block / Tecnología TSI", "TBuildingBlockVsTTecnologiaTSI", counts.BridgeRows, 1, "Building Block → tabla puente", []);
            return DeletionImpactCalculator.Calculate(entityCode, BuildingBlockTable, rootId, displayName, [capacity, bridgeForBuilding], typedConfirmationThreshold);
        }

        if (entityCode == FunctionalityCode)
        {
            return DeletionImpactCalculator.Calculate(FunctionalityCode, FunctionalityTable, rootId, displayName, [], typedConfirmationThreshold);
        }

        if (entityCode == CapacityCode)
        {
            var capacityFunctionalityNode = new DeletionDependencyNode("Funcionalidad", FunctionalityTable, counts.Functionalities, 1, "Capacidad de Seguridad → Funcionalidad", []);
            return DeletionImpactCalculator.Calculate(CapacityCode, CapacityTable, rootId, displayName, [capacityFunctionalityNode], typedConfirmationThreshold);
        }

        var building = new DeletionDependencyNode("Building Block", BuildingBlockTable, counts.BuildingBlocks, 1, "Dominio → Building Block", [capacity]);
        var bridge = new DeletionDependencyNode("Relaciones Building Block / Tecnología TSI", "TBuildingBlockVsTTecnologiaTSI", counts.BridgeRows, 1, "Building Block → tabla puente", []);
        return DeletionImpactCalculator.Calculate(DomainCode, DomainTable, rootId, displayName, [building, bridge], typedConfirmationThreshold);
    }

    private async Task<string?> ReadRootNameAsync(string entityCode, int rootId, CancellationToken cancellationToken)
    {
        var definition = MasterCatalogRegistry.GetByCode(entityCode);
        if (definition is null) return null;

        if (entityCode == "tecnologia-tsi-implementada")
        {
            var sqlImpl = """
                SELECT CONCAT(
                    COALESCE(e.[nombreEmpresa], 'Empresa'),
                    ' — ',
                    COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo], 'Tecnología'),
                    CASE WHEN i.[versionDesplegada] IS NOT NULL AND RTRIM(LTRIM(i.[versionDesplegada])) <> '' THEN CONCAT(' (', i.[versionDesplegada], ')') ELSE '' END
                )
                FROM [dbo].[TTecnologiaTSIimplementadaSubsidiaria] i
                LEFT JOIN [dbo].[TEmpresaSubsidiaria] e ON e.[idEmpresaSubsidiaria] = i.[idEmpresaSubsidiaria]
                LEFT JOIN [dbo].[TTecnologiaTSI] t ON t.[idTecnologiaTSI] = i.[idTecnologiaTSI]
                WHERE i.[idTecnologiaTSIimplementadaSubsidiaria] = @rootId
                """;
            await using var cmdImpl = CreateCommand(sqlImpl, rootId);
            try
            {
                var resImpl = await cmdImpl.ExecuteScalarAsync(cancellationToken);
                if (resImpl is not null && resImpl is not DBNull)
                {
                    var text = resImpl.ToString();
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }
            catch
            {
                // Degradar al flujo estándar si las tablas relacionadas o columnas no coinciden
            }
        }

        var sql = $"SELECT [{definition.DisplayColumn.PhysicalName}] FROM [dbo].[{definition.PhysicalTable}] WHERE [{definition.PrimaryKeyColumn}] = @rootId";
        await using var command = CreateCommand(sql, rootId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        if (!reader.IsDBNull(0))
        {
            var display = reader.GetValue(0)?.ToString();
            if (!string.IsNullOrWhiteSpace(display)) return display;
        }

        return $"{definition.Name} #{rootId}";
    }

    private async Task<List<DeletionDependencyNode>> DiscoverDependentsAsync(MasterCatalogDefinition definition, int rootId, CancellationToken cancellationToken)
    {
        var result = new List<DeletionDependencyNode>();

        if (definition.Code == "tecnologia-tsi")
        {
            var bridgeCount = await ReadCountAsync("SELECT COUNT_BIG(*) FROM [dbo].[TBuildingBlockVsTTecnologiaTSI] WHERE [idTecnologiaTSI] = @rootId", rootId, cancellationToken);
            if (bridgeCount > 0)
            {
                result.Add(new DeletionDependencyNode("Relaciones Building Block / Tecnología TSI", "TBuildingBlockVsTTecnologiaTSI", bridgeCount, 1, "Tecnología TSI → tabla puente", []));
            }
        }

        var selfRefCol = definition.Columns.FirstOrDefault(c => c.Type == CatalogFieldType.ForeignKey && string.Equals(c.ReferenceCatalogCode, definition.Code, StringComparison.OrdinalIgnoreCase));
        if (selfRefCol is not null)
        {
            var selfCount = await ReadCountAsync($"SELECT COUNT_BIG(*) FROM [dbo].[{definition.PhysicalTable}] WHERE [{selfRefCol.PhysicalName}] = @rootId", rootId, cancellationToken);
            if (selfCount > 0)
            {
                result.Add(new DeletionDependencyNode($"{definition.Name} (Adendas / Sub-registros)", definition.PhysicalTable, selfCount, 1, $"{definition.Name} → {definition.Name}", []));
            }
        }

        foreach (var childDef in MasterCatalogRegistry.Catalogs.Where(c => c.Code != definition.Code))
        {
            var fkCols = childDef.Columns.Where(c => c.Type == CatalogFieldType.ForeignKey && string.Equals(c.ReferenceCatalogCode, definition.Code, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var fkCol in fkCols)
            {
                var count = await ReadCountAsync($"SELECT COUNT_BIG(*) FROM [dbo].[{childDef.PhysicalTable}] WHERE [{fkCol.PhysicalName}] = @rootId", rootId, cancellationToken);
                if (count > 0)
                {
                    var grandChildren = new List<DeletionDependencyNode>();
                    if (childDef.Code == "servicio-tecnologia")
                    {
                        var projectHours = await ReadCountAsync($"SELECT COUNT_BIG(*) FROM [dbo].[TTarifarioProyectoHoras] WHERE [idServicio] IN (SELECT [idServicio] FROM [dbo].[TServicioTecnologia] WHERE [{fkCol.PhysicalName}] = @rootId)", rootId, cancellationToken);
                        if (projectHours > 0)
                            grandChildren.Add(new DeletionDependencyNode("Tarifario de Proyecto (Horas)", "TTarifarioProyectoHoras", projectHours, 2, "Servicio → Tarifario Proyecto", []));

                        var operationTariff = await ReadCountAsync($"SELECT COUNT_BIG(*) FROM [dbo].[TTarifarioOperacion] WHERE [idServicio] IN (SELECT [idServicio] FROM [dbo].[TServicioTecnologia] WHERE [{fkCol.PhysicalName}] = @rootId)", rootId, cancellationToken);
                        if (operationTariff > 0)
                            grandChildren.Add(new DeletionDependencyNode("Tarifario de Operación", "TTarifarioOperacion", operationTariff, 2, "Servicio → Tarifario Operación", []));
                    }
                    else if (childDef.Code == "tecnologia-tsi-implementada")
                    {
                        var contracts = await ReadCountAsync($"SELECT COUNT_BIG(*) FROM [dbo].[TContratoTecnologia] WHERE [idTecnologiaTSIimplementadaSubsidiaria] IN (SELECT [idTecnologiaTSIimplementadaSubsidiaria] FROM [dbo].[TTecnologiaTSIimplementadaSubsidiaria] WHERE [{fkCol.PhysicalName}] = @rootId)", rootId, cancellationToken);
                        if (contracts > 0)
                            grandChildren.Add(new DeletionDependencyNode("Contratos de Tecnología", "TContratoTecnologia", contracts, 2, "Implementada → Contratos", []));

                        var drivers = await ReadCountAsync($"SELECT COUNT_BIG(*) FROM [dbo].[TDriver] WHERE [idTecnologiaTSIimplementadaSubsidiaria] IN (SELECT [idTecnologiaTSIimplementadaSubsidiaria] FROM [dbo].[TTecnologiaTSIimplementadaSubsidiaria] WHERE [{fkCol.PhysicalName}] = @rootId)", rootId, cancellationToken);
                        if (drivers > 0)
                            grandChildren.Add(new DeletionDependencyNode("Drivers Operativos", "TDriver", drivers, 2, "Implementada → Drivers", []));

                        var models = await ReadCountAsync($"SELECT COUNT_BIG(*) FROM [dbo].[TModeloDeOperacion] WHERE [idTecnologiaTSIimplementadaSubsidiaria] IN (SELECT [idTecnologiaTSIimplementadaSubsidiaria] FROM [dbo].[TTecnologiaTSIimplementadaSubsidiaria] WHERE [{fkCol.PhysicalName}] = @rootId)", rootId, cancellationToken);
                        if (models > 0)
                            grandChildren.Add(new DeletionDependencyNode("Modelos de Operación", "TModeloDeOperacion", models, 2, "Implementada → Modelo Operación", []));
                    }

                    result.Add(new DeletionDependencyNode(childDef.Name, childDef.PhysicalTable, count, 1, $"{definition.Name} → {childDef.Name}", grandChildren));
                }
            }
        }

        return result;
    }

    private async Task<int> ReadCountAsync(string sql, int rootId, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = CreateCommand(sql, rootId);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is null || result is DBNull ? 0 : Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> ExecuteGenericDeleteAsync(MasterCatalogDefinition definition, int rootId, CancellationToken cancellationToken)
    {
        var totalDeleted = 0;

        if (definition.Code == "tecnologia-tsi")
        {
            // 1. Relaciones en tabla puente
            totalDeleted += await ExecuteAsync("DELETE FROM [dbo].[TBuildingBlockVsTTecnologiaTSI] WHERE [idTecnologiaTSI] = @rootId", rootId, cancellationToken);

            // 2. Tablas nietas de TTecnologiaTSIimplementadaSubsidiaria
            totalDeleted += await ExecuteAsync("DELETE FROM [dbo].[TContratoTecnologia] WHERE [idTecnologiaTSIimplementadaSubsidiaria] IN (SELECT [idTecnologiaTSIimplementadaSubsidiaria] FROM [dbo].[TTecnologiaTSIimplementadaSubsidiaria] WHERE [idTecnologiaTSI] = @rootId) AND [idContratoPadre] IS NOT NULL", rootId, cancellationToken);
            totalDeleted += await ExecuteAsync("DELETE FROM [dbo].[TContratoTecnologia] WHERE [idTecnologiaTSIimplementadaSubsidiaria] IN (SELECT [idTecnologiaTSIimplementadaSubsidiaria] FROM [dbo].[TTecnologiaTSIimplementadaSubsidiaria] WHERE [idTecnologiaTSI] = @rootId)", rootId, cancellationToken);
            totalDeleted += await ExecuteAsync("DELETE FROM [dbo].[TDriver] WHERE [idTecnologiaTSIimplementadaSubsidiaria] IN (SELECT [idTecnologiaTSIimplementadaSubsidiaria] FROM [dbo].[TTecnologiaTSIimplementadaSubsidiaria] WHERE [idTecnologiaTSI] = @rootId)", rootId, cancellationToken);
            totalDeleted += await ExecuteAsync("DELETE FROM [dbo].[TModeloDeOperacion] WHERE [idTecnologiaTSIimplementadaSubsidiaria] IN (SELECT [idTecnologiaTSIimplementadaSubsidiaria] FROM [dbo].[TTecnologiaTSIimplementadaSubsidiaria] WHERE [idTecnologiaTSI] = @rootId)", rootId, cancellationToken);

            // 3. Tablas nietas de TServicioTecnologia (Tarifarios)
            totalDeleted += await ExecuteAsync("DELETE p FROM [dbo].[TTarifarioProyectoHoras] p INNER JOIN [dbo].[TServicioTecnologia] s ON s.[idServicio] = p.[idServicio] WHERE s.[idTecnologiaTSI] = @rootId", rootId, cancellationToken);
            totalDeleted += await ExecuteAsync("DELETE o FROM [dbo].[TTarifarioOperacion] o INNER JOIN [dbo].[TServicioTecnologia] s ON s.[idServicio] = o.[idServicio] WHERE s.[idTecnologiaTSI] = @rootId", rootId, cancellationToken);

            // 4. Casos de Uso
            totalDeleted += await ExecuteAsync("DELETE FROM [dbo].[TCasosDeUso] WHERE [idTecnologiaTSI] = @rootId OR [idEstandarTecnologia] IN (SELECT [idEstandarTecnologia] FROM [dbo].[TEstandarTecnologiaHistorico] WHERE [idTecnologiaTSI] = @rootId)", rootId, cancellationToken);

            // 5. Histórico de Estándares Tecnológicos
            totalDeleted += await ExecuteAsync("DELETE FROM [dbo].[TEstandarTecnologiaHistorico] WHERE [idTecnologiaTSI] = @rootId", rootId, cancellationToken);

            // 6. Desvincular Vendors y Partners (no eliminar la entidad de empresa proveedora)
            totalDeleted += await ExecuteAsync("UPDATE [dbo].[TVendor] SET [idTecnologiaTSI] = NULL WHERE [idTecnologiaTSI] = @rootId", rootId, cancellationToken);
            totalDeleted += await ExecuteAsync("UPDATE [dbo].[TPartner] SET [idTecnologiaTSI] = NULL WHERE [idTecnologiaTSI] = @rootId", rootId, cancellationToken);

            // 7. Tablas hijas directas
            totalDeleted += await ExecuteAsync("DELETE FROM [dbo].[TTecnologiaTSIimplementadaSubsidiaria] WHERE [idTecnologiaTSI] = @rootId", rootId, cancellationToken);
            totalDeleted += await ExecuteAsync("DELETE FROM [dbo].[TServicioTecnologia] WHERE [idTecnologiaTSI] = @rootId", rootId, cancellationToken);

            // 8. Registro raíz
            totalDeleted += await ExecuteAsync("DELETE FROM [dbo].[TTecnologiaTSI] WHERE [idTecnologiaTSI] = @rootId", rootId, cancellationToken);
            return totalDeleted;
        }

        if (definition.Code == "vendor")
        {
            totalDeleted += await ExecuteAsync("DELETE FROM [dbo].[TContactoVendor] WHERE [idVendor] = @rootId", rootId, cancellationToken);
            totalDeleted += await ExecuteAsync("DELETE FROM [dbo].[TContactoPartner] WHERE [idVendor] = @rootId", rootId, cancellationToken);
            totalDeleted += await ExecuteAsync("UPDATE [dbo].[TPartner] SET [idVendor] = NULL WHERE [idVendor] = @rootId", rootId, cancellationToken);
            totalDeleted += await ExecuteAsync("UPDATE [dbo].[TServicioTecnologia] SET [idVendor] = NULL WHERE [idVendor] = @rootId", rootId, cancellationToken);
        }
        else if (definition.Code == "partner")
        {
            totalDeleted += await ExecuteAsync("DELETE FROM [dbo].[TContactoPartner] WHERE [idPartner] = @rootId", rootId, cancellationToken);
        }

        foreach (var childDef in MasterCatalogRegistry.Catalogs.Where(c => c.Code != definition.Code))
        {
            var fkCols = childDef.Columns.Where(c => c.Type == CatalogFieldType.ForeignKey && string.Equals(c.ReferenceCatalogCode, definition.Code, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var fkCol in fkCols)
            {
                if (childDef.Code == "servicio-tecnologia")
                {
                    totalDeleted += await ExecuteAsync($"DELETE p FROM [dbo].[TTarifarioProyectoHoras] p INNER JOIN [dbo].[TServicioTecnologia] s ON s.[idServicio] = p.[idServicio] WHERE s.[{fkCol.PhysicalName}] = @rootId", rootId, cancellationToken);
                    totalDeleted += await ExecuteAsync($"DELETE o FROM [dbo].[TTarifarioOperacion] o INNER JOIN [dbo].[TServicioTecnologia] s ON s.[idServicio] = o.[idServicio] WHERE s.[{fkCol.PhysicalName}] = @rootId", rootId, cancellationToken);
                }
                else if (childDef.Code == "contrato-tecnologia")
                {
                    totalDeleted += await ExecuteAsync($"DELETE FROM [dbo].[TContratoTecnologia] WHERE [{fkCol.PhysicalName}] = @rootId AND [idContratoPadre] IS NOT NULL", rootId, cancellationToken);
                }
            }
        }

        var selfRefCol = definition.Columns.FirstOrDefault(c => c.Type == CatalogFieldType.ForeignKey && string.Equals(c.ReferenceCatalogCode, definition.Code, StringComparison.OrdinalIgnoreCase));
        if (selfRefCol is not null)
        {
            totalDeleted += await ExecuteAsync($"DELETE FROM [dbo].[{definition.PhysicalTable}] WHERE [{selfRefCol.PhysicalName}] = @rootId", rootId, cancellationToken);
        }

        foreach (var childDef in MasterCatalogRegistry.Catalogs.Where(c => c.Code != definition.Code))
        {
            var fkCols = childDef.Columns.Where(c => c.Type == CatalogFieldType.ForeignKey && string.Equals(c.ReferenceCatalogCode, definition.Code, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var fkCol in fkCols)
            {
                totalDeleted += await ExecuteAsync($"DELETE FROM [dbo].[{childDef.PhysicalTable}] WHERE [{fkCol.PhysicalName}] = @rootId", rootId, cancellationToken);
            }
        }

        totalDeleted += await ExecuteAsync($"DELETE FROM [dbo].[{definition.PhysicalTable}] WHERE [{definition.PrimaryKeyColumn}] = @rootId", rootId, cancellationToken);
        return totalDeleted;
    }

    private async Task<DependencyCounts> ReadCountsAsync(string entityCode, int rootId, CancellationToken cancellationToken)
    {
        var sql = entityCode == FunctionalityCode
            ? "SELECT 0, 0, 0, 0"
            : entityCode == CapacityCode
                ? "SELECT 0, 0, COUNT(*), 0 FROM [dbo].[TFuncionalidad] WHERE [idCapacidad] = @rootId"
            : entityCode == DomainCode ? """
            SELECT COUNT(DISTINCT b.[idBuildingBlock]), COUNT(DISTINCT c.[idCapacidad]), COUNT(DISTINCT f.[idFuncionalidad]), COUNT(DISTINCT bridge.[idBuildingBlock])
            FROM [dbo].[TBuildingBlock] b
            LEFT JOIN [dbo].[TCapacidadDeSeguridad] c ON c.[idBuildingBlock] = b.[idBuildingBlock]
            LEFT JOIN [dbo].[TFuncionalidad] f ON f.[idCapacidad] = c.[idCapacidad]
            LEFT JOIN [dbo].[TBuildingBlockVsTTecnologiaTSI] bridge ON bridge.[idBuildingBlock] = b.[idBuildingBlock]
            WHERE b.[idDominio] = @rootId
            """ : """
            SELECT COUNT(DISTINCT b.[idBuildingBlock]), COUNT(DISTINCT c.[idCapacidad]), COUNT(DISTINCT f.[idFuncionalidad]), COUNT(DISTINCT bridge.[idBuildingBlock])
            FROM [dbo].[TBuildingBlock] b
            LEFT JOIN [dbo].[TCapacidadDeSeguridad] c ON c.[idBuildingBlock] = b.[idBuildingBlock]
            LEFT JOIN [dbo].[TFuncionalidad] f ON f.[idCapacidad] = c.[idCapacidad]
            LEFT JOIN [dbo].[TBuildingBlockVsTTecnologiaTSI] bridge ON bridge.[idBuildingBlock] = b.[idBuildingBlock]
            WHERE b.[idBuildingBlock] = @rootId
            """;
        await using var command = CreateCommand(sql, rootId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new DependencyCounts(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3))
            : new DependencyCounts(0, 0, 0, 0);
    }

    private async Task<int> ExecuteAsync(string sql, int rootId, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(sql, rootId);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CaptureDeleteSnapshotsAsync(AuditOperation operation, string entityCode, int rootId,
        string rootDisplayName, CancellationToken cancellationToken)
    {
        if (entityCode is not (DomainCode or BuildingBlockCode or CapacityCode or FunctionalityCode))
        {
            var def = MasterCatalogRegistry.GetByCode(entityCode)!;
            var order = 0;
            foreach (var childDef in MasterCatalogRegistry.Catalogs.Where(c => c.Code != def.Code))
            {
                var fkCols = childDef.Columns.Where(c => c.Type == CatalogFieldType.ForeignKey && string.Equals(c.ReferenceCatalogCode, def.Code, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var fkCol in fkCols)
                {
                    await CaptureRowsAsync(operation, entityCode, childDef.PhysicalTable, childDef.PrimaryKeyColumn, $"t.[{fkCol.PhysicalName}] = @rootId", childDef.DisplayColumn.PhysicalName, order++, 1, false, rootId, cancellationToken);
                }
            }
            await CaptureRowsAsync(operation, entityCode, def.PhysicalTable, def.PrimaryKeyColumn, $"t.[{def.PrimaryKeyColumn}] = @rootId", def.DisplayColumn.PhysicalName, order, 0, true, rootId, cancellationToken);
            return;
        }

        if (entityCode == DomainCode)
        {
            await CaptureRowsAsync(operation, entityCode, "TFuncionalidad", "idFuncionalidad", "f.[idCapacidad] IN (SELECT c.[idCapacidad] FROM [dbo].[TCapacidadDeSeguridad] c INNER JOIN [dbo].[TBuildingBlock] b ON b.[idBuildingBlock] = c.[idBuildingBlock] WHERE b.[idDominio] = @rootId)", "nombreFuncionalidad", 0, 3, false, rootId, cancellationToken);
            await CaptureRowsAsync(operation, entityCode, "TCapacidadDeSeguridad", "idCapacidad", "c.[idBuildingBlock] IN (SELECT b.[idBuildingBlock] FROM [dbo].[TBuildingBlock] b WHERE b.[idDominio] = @rootId)", "nombreCapacidad", 1, 2, false, rootId, cancellationToken);
            await CaptureRowsAsync(operation, entityCode, "TBuildingBlockVsTTecnologiaTSI", "idBuildingBlock", "bridge.[idBuildingBlock] IN (SELECT b.[idBuildingBlock] FROM [dbo].[TBuildingBlock] b WHERE b.[idDominio] = @rootId)", null, 0, 1, false, rootId, cancellationToken);
            await CaptureRowsAsync(operation, entityCode, "TBuildingBlock", "idBuildingBlock", "b.[idDominio] = @rootId", "nombreBuildingBlock", 2, 1, false, rootId, cancellationToken);
            await CaptureRowsAsync(operation, entityCode, "TMDominio", "iddominio", "d.[iddominio] = @rootId", "dominio", 3, 0, true, rootId, cancellationToken);
            return;
        }

        if (entityCode == BuildingBlockCode)
        {
            await CaptureRowsAsync(operation, entityCode, "TFuncionalidad", "idFuncionalidad", "f.[idCapacidad] IN (SELECT c.[idCapacidad] FROM [dbo].[TCapacidadDeSeguridad] c WHERE c.[idBuildingBlock] = @rootId)", "nombreFuncionalidad", 0, 3, false, rootId, cancellationToken);
            await CaptureRowsAsync(operation, entityCode, "TCapacidadDeSeguridad", "idCapacidad", "c.[idBuildingBlock] = @rootId", "nombreCapacidad", 1, 2, false, rootId, cancellationToken);
            await CaptureRowsAsync(operation, entityCode, "TBuildingBlockVsTTecnologiaTSI", "idBuildingBlock", "bridge.[idBuildingBlock] = @rootId", null, 0, 1, false, rootId, cancellationToken);
            await CaptureRowsAsync(operation, entityCode, "TBuildingBlock", "idBuildingBlock", "b.[idBuildingBlock] = @rootId", "nombreBuildingBlock", 2, 0, true, rootId, cancellationToken);
            return;
        }

        if (entityCode == CapacityCode)
        {
            await CaptureRowsAsync(operation, entityCode, "TFuncionalidad", "idFuncionalidad", "f.[idCapacidad] = @rootId", "nombreFuncionalidad", 0, 1, false, rootId, cancellationToken);
            await CaptureRowsAsync(operation, entityCode, "TCapacidadDeSeguridad", "idCapacidad", "c.[idCapacidad] = @rootId", "nombreCapacidad", 1, 0, true, rootId, cancellationToken);
            return;
        }

        await CaptureRowsAsync(operation, entityCode, "TFuncionalidad", "idFuncionalidad", "f.[idFuncionalidad] = @rootId", "nombreFuncionalidad", 0, 0, true, rootId, cancellationToken);
    }

    private async Task CaptureRowsAsync(AuditOperation operation, string entityCode, string table, string primaryKey,
        string predicate, string? displayColumn, int deleteOrder, int restoreOrder, bool root, int rootId,
        CancellationToken cancellationToken)
    {
        var alias = table == "TMDominio" ? "d" : table == "TBuildingBlock" ? "b" : table == "TCapacidadDeSeguridad" ? "c" : table == "TFuncionalidad" ? "f" : table == "TBuildingBlockVsTTecnologiaTSI" ? "bridge" : "t";
        await using var command = CreateCommand($"SELECT * FROM [dbo].[{table}] {alias} WHERE {predicate}", rootId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < reader.FieldCount; index++)
                values[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader.GetValue(index);
            values.TryGetValue(primaryKey, out var key);
            var foreignKeys = values.Where(item => item.Key.StartsWith("id", StringComparison.OrdinalIgnoreCase) && !item.Key.Equals(primaryKey, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(item => item.Key, item => item.Value);
            var displayName = displayColumn is not null && values.TryGetValue(displayColumn, out var display) ? display?.ToString() : null;
            auditTrail.AddSnapshot(operation, entityCode, table,
                JsonSerializer.Serialize(new { id = key }), JsonSerializer.Serialize(foreignKeys),
                JsonSerializer.Serialize(values), deleteOrder, restoreOrder, root && table == operation.PhysicalTableName,
                displayName);
        }
    }

    private DbCommand CreateCommand(string sql, int rootId)
    {
        var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@rootId";
        parameter.Value = rootId;
        command.Parameters.Add(parameter);
        return command;
    }

    private bool openedConnection;
    private async Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        openedConnection = connection.State != ConnectionState.Open;
        if (openedConnection) await dbContext.Database.OpenConnectionAsync(cancellationToken);
    }

    private async Task CloseConnectionAsync()
    {
        if (openedConnection)
        {
            await dbContext.Database.CloseConnectionAsync();
            openedConnection = false;
        }
    }

    private static void EnsureSupported(string entityCode)
    {
        var definition = MasterCatalogRegistry.GetByCode(entityCode);
        if (definition is null || !definition.IsDeletable)
        {
            throw new InvalidOperationException("La entidad no pertenece a la lista blanca de eliminaciones habilitadas.");
        }
    }

    private sealed record DependencyCounts(int BuildingBlocks, int Capacities, int Functionalities, int BridgeRows);
}