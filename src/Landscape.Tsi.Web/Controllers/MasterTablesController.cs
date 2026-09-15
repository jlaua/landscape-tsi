using System.Security.Claims;

using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Web.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landscape.Tsi.Web.Controllers;

[Authorize(Policy = Permissions.CatalogView)]
[Route("Administration/MasterTables")]
public sealed class MasterTablesController(
    IDominioService dominioService,
    ICatalogManagementService catalogService,
    IDeletionImpactService deletionImpactService,
    IBuildingBlockRelatedService buildingBlockRelatedService,
    IBuildingBlockTechnologyMappingService technologyMappingService,
    IAdoptionProcessService adoptionService,
    IAssociationImpactService associationImpactService,
    IAssignmentService assignmentService,
    IEffectiveAccessService effectiveAccessService,
    ILogger<MasterTablesController> logger) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(MasterCatalogRegistry.EntityMetadata.Where(entity => entity.IsAdministrable).ToArray());

    [HttpGet("Domain")]
    public async Task<IActionResult> Domain(
        string? search,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        EnsureDomainIsEnabled();
        return View(new DominioPageViewModel
        {
            Search = search,
            Result = await dominioService.ListAsync(search, page, pageSize, cancellationToken)
        });
    }

    [HttpGet("Domain/{id:int}")]
    public async Task<IActionResult> DomainDetails(int id, string? relatedSearch, int relatedPage = 1, int relatedPageSize = 10, CancellationToken cancellationToken = default)
    {
        EnsureDomainIsEnabled();
        var record = await dominioService.GetAsync(id, cancellationToken);
        if (record is null) return NotFound();

        var parent = MasterCatalogRegistry.GetByCode("dominio")!;
        var child = MasterCatalogRegistry.GetByCode("building-block")!;
        var foreignKey = child.Columns.Single(column => column.ReferenceCatalogCode == parent.Code);
        var related = await catalogService.ListRelatedAsync(child, foreignKey, id, relatedSearch, relatedPage, relatedPageSize, cancellationToken);
        var impact = await deletionImpactService.PreviewAsync("dominio", id, cancellationToken);
        if (impact is null) return NotFound();

        return View(new DominioDetailViewModel
        {
            Record = record,
            Edit = DominioFormModel.FromRecord(record),
            RelatedBuildingBlocks = related,
            DependencyImpact = impact,
            RelatedSearch = relatedSearch
        });
    }

    [HttpGet("Domain/{id:int}/building-blocks-capacidades")]
    public async Task<IActionResult> DomainBuildingBlocksCapabilities(int id, CancellationToken cancellationToken)
    {
        EnsureDomainIsEnabled();
        var domain = await dominioService.GetAsync(id, cancellationToken);
        if (domain is null) return NotFound();

        var parent = MasterCatalogRegistry.GetByCode("dominio")!;
        var bbDef = MasterCatalogRegistry.GetByCode("building-block")!;
        var capDef = MasterCatalogRegistry.GetByCode("capacidad-seguridad")!;
        var foreignKeyDomain = bbDef.Columns.Single(c => c.ReferenceCatalogCode == parent.Code);
        var foreignKeyBB = capDef.Columns.Single(c => c.ReferenceCatalogCode == bbDef.Code);

        var bbResult = await catalogService.ListRelatedAsync(bbDef, foreignKeyDomain, id, null, 1, 50, cancellationToken);
        var items = new List<object>();

        foreach (var bb in bbResult.Items)
        {
            var capCount = await catalogService.GetRelatedCountAsync(capDef, foreignKeyBB, bb.Id, cancellationToken);
            items.Add(new
            {
                id = bb.Id,
                nombre = bb.DisplayValues.GetValueOrDefault("nombre") ?? $"BB #{bb.Id}",
                capacidadesCount = capCount
            });
        }

        return Json(new { items });
    }

    [HttpGet("Domain/{id:int}/delete-impact")]
    [Authorize(Policy = Permissions.CatalogDelete)]
    public async Task<IActionResult> DomainDeleteImpact(int id, CancellationToken cancellationToken)
    {
        EnsureDomainIsEnabled();
        var impact = await deletionImpactService.PreviewAsync("dominio", id, cancellationToken);
        return impact is null ? NotFound() : Json(impact);
    }

    [HttpPost("Domain/{id:int}/delete")]
    [Authorize(Policy = Permissions.CatalogDelete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDomain(int id, string? confirmation, CancellationToken cancellationToken)
    {
        EnsureDomainIsEnabled();
        if (!TryGetActorUserId(out var actorUserId)) return Forbid();
        if (!await HasCorporateScopeAsync(actorUserId, cancellationToken)) return Forbid();

        try
        {
            var result = await deletionImpactService.DeleteAsync("dominio", id, confirmation, actorUserId, HttpContext.TraceIdentifier, cancellationToken);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Registro eliminado correctamente. Se eliminaron {result.TotalRecordsDeleted} registros relacionados.";
                return RedirectToAction(nameof(Domain));
            }

            TempData["ErrorMessage"] = result.ErrorMessage ?? "No fue posible eliminar el registro.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al eliminar un dominio. Correlación: {CorrelationId}", HttpContext.TraceIdentifier);
            TempData["ErrorMessage"] = "No fue posible eliminar el registro. La operación fue revertida.";
        }
        return RedirectToAction(nameof(DomainDetails), new { id });
    }

    [HttpPost("Domain")]
    [Authorize(Policy = Permissions.CatalogCreate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDomain(
        DominioFormModel model,
        string? search,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        EnsureDomainIsEnabled();
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Forbid();
        }
        if (!await HasCorporateScopeAsync(actorUserId, cancellationToken)) return Forbid();

        try
        {
            await dominioService.CreateAsync(model.ToCommand(), actorUserId, HttpContext.TraceIdentifier, cancellationToken);
            TempData["SuccessMessage"] = "El dominio se creó correctamente.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al crear un registro de TMDominio. Correlación: {CorrelationId}", HttpContext.TraceIdentifier);
            TempData["ErrorMessage"] = "No fue posible crear el dominio. Intente nuevamente.";
        }

        return RedirectToAction(nameof(Domain), new { search, page, pageSize });
    }

    [HttpPost("Domain/{id:int}")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDomain(
        int id,
        DominioFormModel model,
        CancellationToken cancellationToken = default)
    {
        EnsureDomainIsEnabled();
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Forbid();
        }
        if (!await HasCorporateScopeAsync(actorUserId, cancellationToken)) return Forbid();

        try
        {
            if (!await dominioService.UpdateAsync(id, model.ToCommand(), actorUserId, HttpContext.TraceIdentifier, cancellationToken))
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "El dominio se actualizó correctamente.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al editar un registro de TMDominio. Correlación: {CorrelationId}", HttpContext.TraceIdentifier);
            TempData["ErrorMessage"] = "No fue posible actualizar el dominio. Intente nuevamente.";
        }

        return RedirectToAction(nameof(DomainDetails), new { id });
    }

    [HttpGet("{catalogRoute}")]
    public async Task<IActionResult> Catalog(
        string catalogRoute,
        string? search,
        int page = 1,
        int pageSize = 10,
        int? parentId = null,
        string? sortColumn = null,
        string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        var definition = MasterCatalogRegistry.GetByRoute(catalogRoute);
        if (definition is null)
        {
            return NotFound();
        }
        if (definition.Code == "dominio")
        {
            return RedirectToAction(nameof(Domain), new { search, page, pageSize });
        }
        if (parentId.HasValue && definition.Code != "building-block")
        {
            return NotFound();
        }

        var result = parentId.HasValue
            ? await catalogService.ListRelatedAsync(definition, definition.Columns.Single(column => column.ReferenceCatalogCode == "dominio"), parentId.Value, search, page, pageSize, cancellationToken)
            : await catalogService.ListAsync(definition, search, page, pageSize, cancellationToken, sortColumn, sortDirection);

        IReadOnlyDictionary<int, int>? vendorTechCounts = null;
        if (definition.Code == "vendor" && result.Items.Count > 0)
        {
            vendorTechCounts = await catalogService.GetVendorTechnologyCountsAsync(result.Items.Select(x => x.Id), cancellationToken);
        }

        return View(new CatalogPageViewModel
        {
            Definition = definition,
            Search = search,
            Result = result,
            SortColumn = sortColumn,
            SortDirection = sortDirection,
            Options = await catalogService.GetOptionsAsync(definition, cancellationToken),
            VendorTechnologyCounts = vendorTechCounts
        });
    }

    [HttpGet("vendor/{id:int}/technologies")]
    public async Task<IActionResult> GetVendorTechnologies(int id, CancellationToken cancellationToken)
    {
        var vendorDefinition = MasterCatalogRegistry.GetByCode("vendor");
        if (vendorDefinition is null) return NotFound();

        var vendor = await catalogService.GetAsync(vendorDefinition, id, cancellationToken);
        if (vendor is null) return NotFound();

        var vendorName = vendor.DisplayValues.GetValueOrDefault("nombreVendor") ?? $"Vendor #{id}";
        var technologies = await catalogService.GetVendorTechnologiesAsync(id, cancellationToken);

        return Json(new
        {
            vendorId = id,
            vendorName,
            totalCount = technologies.Count,
            items = technologies.Select(t => new
            {
                id = t.Id,
                nombreCorporativo = t.NombreCorporativo,
                nombreLocal = t.NombreLocal ?? "—",
                familia = t.Familia ?? "—",
                estadoAdopcion = t.EstadoAdopcion ?? "—",
                licenciamiento = t.Licenciamiento ?? "—",
                entorno = t.Entorno ?? "—",
                detailsUrl = Url.Action(nameof(CatalogDetails), new { catalogRoute = "tecnologia-tsi", id = t.Id })
            })
        });
    }

    [HttpGet("{catalogRoute}/details/{id:int}")]
    public async Task<IActionResult> CatalogDetails(
        string catalogRoute,
        int id,
        string? capabilitySearch,
        string? functionalitySearch,
        string? technologySearch,
        int capabilityPage = 1,
        int functionalityPage = 1,
        int technologyPage = 1,
        int relatedPageSize = 10,
        string? functionalitySortBy = null,
        string? functionalitySortDirection = null,
        CancellationToken cancellationToken = default)
    {
        var definition = MasterCatalogRegistry.GetByRoute(catalogRoute);
        if (definition is null || definition.Code == "dominio")
        {
            return NotFound();
        }
        var record = await catalogService.GetAsync(definition, id, cancellationToken);
        if (record is null) return NotFound();
        var related = definition.Code == "building-block"
            ? await buildingBlockRelatedService.GetAsync(id, capabilitySearch, functionalitySearch, technologySearch, capabilityPage, functionalityPage, technologyPage, relatedPageSize, functionalitySortBy, functionalitySortDirection, cancellationToken)
            : null;
        var technologyMapping = definition.Code == "building-block"
            ? await technologyMappingService.GetBuildingBlockRelationsAsync(id, technologySearch, null, cancellationToken)
            : null;
        var technologyRelations = definition.Code == "tecnologia-tsi"
            ? await technologyMappingService.GetTechnologyRelationsAsync(id, cancellationToken)
            : null;
        CatalogPageResult? relatedRecords = null;
        if (definition.Code == "capacidad-seguridad")
        {
            var child = MasterCatalogRegistry.GetByCode("funcionalidad")!;
            var foreignKey = child.Columns.Single(column => column.ReferenceCatalogCode == definition.Code);
            relatedRecords = await catalogService.ListRelatedAsync(child, foreignKey, id, functionalitySearch, functionalityPage, relatedPageSize, cancellationToken);
        }

        CatalogPageResult? implementedContracts = null;
        CatalogPageResult? implementedOperationModels = null;
        CatalogPageResult? implementedDrivers = null;
        CatalogPageResult? implementedServices = null;

        if (definition.Code == "tecnologia-tsi-implementada")
        {
            var contractDef = MasterCatalogRegistry.GetByCode("contrato-tecnologia");
            if (contractDef is not null)
            {
                var fk = contractDef.Columns.FirstOrDefault(c => c.ReferenceCatalogCode == definition.Code);
                if (fk is not null)
                {
                    implementedContracts = await catalogService.ListRelatedAsync(contractDef, fk, id, null, 1, 50, cancellationToken);
                }
            }

            var opModelDef = MasterCatalogRegistry.GetByCode("modelo-operacion");
            if (opModelDef is not null)
            {
                var fk = opModelDef.Columns.FirstOrDefault(c => c.ReferenceCatalogCode == definition.Code);
                if (fk is not null)
                {
                    implementedOperationModels = await catalogService.ListRelatedAsync(opModelDef, fk, id, null, 1, 50, cancellationToken);
                }
            }

            var driverDef = MasterCatalogRegistry.GetByCode("driver");
            if (driverDef is not null)
            {
                var fk = driverDef.Columns.FirstOrDefault(c => c.ReferenceCatalogCode == definition.Code);
                if (fk is not null)
                {
                    implementedDrivers = await catalogService.ListRelatedAsync(driverDef, fk, id, null, 1, 50, cancellationToken);
                }
            }

            var serviceDef = MasterCatalogRegistry.GetByCode("servicio-tecnologia");
            if (serviceDef is not null)
            {
                var fk = serviceDef.Columns.FirstOrDefault(c => c.ReferenceCatalogCode == definition.Code);
                if (fk is not null)
                {
                    implementedServices = await catalogService.ListRelatedAsync(serviceDef, fk, id, null, 1, 50, cancellationToken);
                }
            }
        }

        CatalogPageResult? processCompanies = null;
        CatalogPageResult? processStandards = null;
        CatalogPageResult? processServices = null;

        if (definition.Code == "proceso-adopcion-tsi")
        {
            var compDef = MasterCatalogRegistry.GetByCode("proceso-adopcion-empresa");
            if (compDef is not null)
            {
                var fk = compDef.Columns.FirstOrDefault(c => c.ReferenceCatalogCode == definition.Code);
                if (fk is not null)
                {
                    processCompanies = await catalogService.ListRelatedAsync(compDef, fk, id, null, 1, 50, cancellationToken);
                }
            }

            var stdDef = MasterCatalogRegistry.GetByCode("estandar-tecnologia-historico");
            if (stdDef is not null)
            {
                var fk = stdDef.Columns.FirstOrDefault(c => c.ReferenceCatalogCode == definition.Code);
                if (fk is not null)
                {
                    processStandards = await catalogService.ListRelatedAsync(stdDef, fk, id, null, 1, 50, cancellationToken);
                }
            }

            var srvDef = MasterCatalogRegistry.GetByCode("servicio-tecnologia");
            if (srvDef is not null)
            {
                var fk = srvDef.Columns.FirstOrDefault(c => c.ReferenceCatalogCode == definition.Code);
                if (fk is not null)
                {
                    processServices = await catalogService.ListRelatedAsync(srvDef, fk, id, null, 1, 50, cancellationToken);
                }
            }
        }

        var adoptionDetail = definition.Code == "building-block"
            ? await adoptionService.GetProcessDetailByBuildingBlockAsync(id, cancellationToken)
            : (definition.Code == "proceso-adopcion-tsi"
                ? await adoptionService.GetProcessDetailAsync(id, cancellationToken)
                : null);

        var options = await catalogService.GetOptionsAsync(definition, cancellationToken);
        if (definition.Code == "building-block")
        {
            var mutableOptions = options.ToDictionary(k => k.Key, k => k.Value, StringComparer.Ordinal);
            var capDef = MasterCatalogRegistry.GetByCode("capacidad-seguridad");
            if (capDef is not null)
            {
                var capOptions = await catalogService.GetOptionsAsync(capDef, cancellationToken);
                foreach (var (k, v) in capOptions)
                {
                    mutableOptions[k] = v;
                }
            }
            var funcDef = MasterCatalogRegistry.GetByCode("funcionalidad");
            if (funcDef is not null)
            {
                var funcOptions = await catalogService.GetOptionsAsync(funcDef, cancellationToken);
                foreach (var (k, v) in funcOptions)
                {
                    mutableOptions[k] = v;
                }
            }
            options = mutableOptions;
        }

        IReadOnlyList<VendorTechnologyDto>? vendorTechnologies = null;
        if (definition.Code == "vendor")
        {
            vendorTechnologies = await catalogService.GetVendorTechnologiesAsync(id, cancellationToken);
        }

        return View(new CatalogDetailViewModel
        {
            Definition = definition,
            Record = record,
            Options = options,
            Related = related,
            RelatedRecords = relatedRecords,
            TechnologyMapping = technologyMapping,
            TechnologyRelations = technologyRelations,
            FunctionalitySortBy = functionalitySortBy,
            FunctionalitySortDirection = functionalitySortDirection,
            FunctionalitySearch = functionalitySearch,
            CapabilitySearch = capabilitySearch,
            TechnologySearch = technologySearch,
            AdoptionDetail = adoptionDetail,
            ImplementedContracts = implementedContracts,
            ImplementedOperationModels = implementedOperationModels,
            ImplementedDrivers = implementedDrivers,
            ImplementedServices = implementedServices,
            ProcessCompanies = processCompanies,
            ProcessStandards = processStandards,
            ProcessServices = processServices,
            VendorTechnologies = vendorTechnologies
        });
    }

    [HttpGet("{catalogRoute}/{id:int}/delete-impact")]
    [Authorize(Policy = Permissions.CatalogDelete)]
    public async Task<IActionResult> CatalogDeleteImpact(string catalogRoute, int id, CancellationToken cancellationToken)
    {
        var definition = MasterCatalogRegistry.GetByRoute(catalogRoute);
        if (definition is null || !definition.IsDeletable) return NotFound();
        try
        {
            var impact = await deletionImpactService.PreviewAsync(definition.Code, id, cancellationToken);
            return impact is null ? NotFound() : Json(impact);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al calcular el impacto de eliminación para {Route} id {Id}. Correlación: {CorrelationId}", catalogRoute, id, HttpContext.TraceIdentifier);
            return StatusCode(500, new { message = "Error al calcular el impacto de dependencias." });
        }
    }

    [HttpGet("tecnologia-tsi-implementada/company-metrics")]
    [Authorize(Policy = Permissions.CatalogView)]
    public async Task<IActionResult> ImplementedTechnologiesCompanyMetrics(CancellationToken cancellationToken)
    {
        var parentDef = MasterCatalogRegistry.GetByCode("empresa-subsidiaria");
        var childDef = MasterCatalogRegistry.GetByCode("tecnologia-tsi-implementada");
        if (parentDef is null || childDef is null) return NotFound();

        var fkCol = childDef.Columns.FirstOrDefault(c => c.ReferenceCatalogCode == "empresa-subsidiaria");
        if (fkCol is null) return NotFound();

        var buckets = await catalogService.GetRelationCountsAsync(parentDef, childDef, fkCol, cancellationToken);
        var totalCompanies = buckets.Count(b => b.Total > 0);
        var totalImplementations = buckets.Sum(b => b.Total);
        var averagePerCompany = totalCompanies == 0 ? 0 : Math.Round((double)totalImplementations / totalCompanies, 1);

        return Json(new
        {
            summary = new
            {
                totalCompanies,
                totalImplementations,
                averagePerCompany
            },
            companies = buckets.Select(b => new
            {
                companyId = b.ParentId,
                companyName = b.ParentName,
                count = b.Total
            }).OrderByDescending(b => b.count).ThenBy(b => b.companyName).ToList()
        });
    }

    [HttpGet("tecnologia-tsi-implementada/companies/{companyId:int}/technologies")]
    [Authorize(Policy = Permissions.CatalogView)]
    public async Task<IActionResult> ImplementedTechnologiesByCompany(int companyId, CancellationToken cancellationToken)
    {
        var childDef = MasterCatalogRegistry.GetByCode("tecnologia-tsi-implementada");
        if (childDef is null) return NotFound();

        var fkCol = childDef.Columns.FirstOrDefault(c => c.ReferenceCatalogCode == "empresa-subsidiaria");
        if (fkCol is null) return NotFound();

        var related = await catalogService.ListRelatedAsync(childDef, fkCol, companyId, null, 1, 100, cancellationToken);
        var items = related.Items.Select(row => new
        {
            id = row.Id,
            empresa = row.DisplayValues.GetValueOrDefault("empresa") ?? "—",
            tecnologia = row.DisplayValues.GetValueOrDefault("tecnologia") ?? "—",
            buildingBlock = row.DisplayValues.GetValueOrDefault("buildingBlock") ?? "—",
            versionDesplegada = row.DisplayValues.GetValueOrDefault("versionDesplegada") ?? "—",
            esInstanciaCorporativa = row.DisplayValues.GetValueOrDefault("esInstanciaCorporativa") ?? "—",
            esTecnologiaPrimaria = row.DisplayValues.GetValueOrDefault("esTecnologiaPrimaria") ?? "—",
            detailsUrl = Url.Action(nameof(CatalogDetails), new { catalogRoute = "tecnologia-tsi-implementada", id = row.Id }),
            deleteImpactUrl = Url.Action(nameof(CatalogDeleteImpact), new { catalogRoute = "tecnologia-tsi-implementada", id = row.Id }),
            deleteActionUrl = Url.Action(nameof(DeleteCatalog), new { catalogRoute = "tecnologia-tsi-implementada", id = row.Id }),
            deleteRowName = $"{(row.DisplayValues.GetValueOrDefault("empresa") ?? "")} — {(row.DisplayValues.GetValueOrDefault("tecnologia") ?? row.Id.ToString())}"
        }).ToList();

        return Json(new { companyId, items, total = related.TotalCount });
    }

    [HttpPost("tecnologia-tsi-implementada/bulk-delete")]
    [Authorize(Policy = Permissions.CatalogDelete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDeleteImplementedTechnologies([FromForm] int[] selectedIds, [FromForm] string? confirmation, [FromForm] string? returnUrl, CancellationToken cancellationToken)
    {
        var definition = MasterCatalogRegistry.GetByRoute("tecnologia-tsi-implementada");
        if (definition is null || !definition.IsDeletable) return NotFound();

        if (selectedIds == null || selectedIds.Length == 0)
        {
            TempData["ErrorMessage"] = "Debe seleccionar al menos un registro para eliminar.";
            return RedirectToLocalOrCatalog(returnUrl, "tecnologia-tsi-implementada");
        }

        if (!string.Equals(confirmation?.Trim(), "ELIMINAR", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Debe escribir ELIMINAR para confirmar la eliminación masiva.";
            return RedirectToLocalOrCatalog(returnUrl, "tecnologia-tsi-implementada");
        }

        if (!TryGetActorUserId(out var actorUserId)) return Forbid();
        if (!await HasCorporateScopeAsync(actorUserId, cancellationToken)) return Forbid();

        var successCount = 0;
        var totalCascadeRecords = 0;
        var errors = new List<string>();

        foreach (var id in selectedIds.Distinct())
        {
            try
            {
                var result = await deletionImpactService.DeleteAsync(definition.Code, id, confirmation, actorUserId, HttpContext.TraceIdentifier, cancellationToken);
                if (result.Succeeded)
                {
                    successCount++;
                    totalCascadeRecords += result.TotalRecordsDeleted;
                }
                else if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    errors.Add($"ID {id}: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al eliminar masivamente id {Id} de {Catalog}. Correlación: {CorrelationId}", id, definition.Name, HttpContext.TraceIdentifier);
                errors.Add($"ID {id}: Error interno al procesar.");
            }
        }

        if (successCount > 0)
        {
            var message = $"Se eliminaron exitosamente {successCount} tecnología(s) implementada(s) ({totalCascadeRecords} registro(s) en cascada).";
            if (errors.Count > 0)
            {
                message += $" Hubo errores en {errors.Count} elemento(s).";
            }
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = errors.Count > 0
                ? $"No fue posible eliminar los registros seleccionados: {string.Join("; ", errors.Take(3))}"
                : "No fue posible eliminar los registros seleccionados.";
        }

        return RedirectToLocalOrCatalog(returnUrl, "tecnologia-tsi-implementada");
    }

    private IActionResult RedirectToLocalOrCatalog(string? returnUrl, string catalogRoute)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction(nameof(Catalog), new { catalogRoute });
    }

    [HttpPost("{catalogRoute}/{id:int}/delete")]
    [Authorize(Policy = Permissions.CatalogDelete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCatalog(string catalogRoute, int id, string? confirmation, string? returnUrl, CancellationToken cancellationToken)
    {
        var definition = MasterCatalogRegistry.GetByRoute(catalogRoute);
        if (definition is null || !definition.IsDeletable) return NotFound();
        if (!TryGetActorUserId(out var actorUserId)) return Forbid();
        if (!await HasCorporateScopeAsync(actorUserId, cancellationToken)) return Forbid();
        try
        {
            var result = await deletionImpactService.DeleteAsync(definition.Code, id, confirmation, actorUserId, HttpContext.TraceIdentifier, cancellationToken);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? (result.TotalRecordsDeleted == 1 ? "Registro eliminado correctamente. Se eliminó 1 registro." : $"Registro eliminado correctamente. Se eliminaron {result.TotalRecordsDeleted} registros relacionados.")
                : result.ErrorMessage ?? "No fue posible eliminar el registro.";
            if (result.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction(nameof(Catalog), new { catalogRoute });
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al eliminar {Catalog}. Correlación: {CorrelationId}", definition.Name, HttpContext.TraceIdentifier);
            TempData["ErrorMessage"] = "No fue posible eliminar el registro. La operación fue revertida.";
        }
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction(nameof(CatalogDetails), new { catalogRoute, id });
    }

    [HttpGet("{catalogRoute}/create")]
    [Authorize(Policy = Permissions.CatalogCreate)]
    public async Task<IActionResult> CatalogCreateForm(string catalogRoute, string? returnUrl, string? initialKey, string? initialValue, CancellationToken cancellationToken)
    {
        var definition = MasterCatalogRegistry.GetByRoute(catalogRoute);
        if (definition is null || definition.Code == "dominio" || definition.IsReadOnly)
        {
            return NotFound();
        }
        if (definition.EditorMode == CatalogEditorMode.Modal && string.IsNullOrWhiteSpace(returnUrl))
        {
            return RedirectToAction(nameof(Catalog), new { catalogRoute });
        }
        var initialValues = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(initialKey) && !string.IsNullOrWhiteSpace(initialValue))
        {
            initialValues[initialKey] = initialValue;
        }
        return View("CatalogEditor", new CatalogEditorViewModel
        {
            Definition = definition,
            Options = await catalogService.GetOptionsAsync(definition, cancellationToken),
            ReturnUrl = returnUrl,
            InitialValues = initialValues
        });
    }

    [HttpGet("{catalogRoute}/edit/{id:int}")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    public async Task<IActionResult> CatalogEditForm(string catalogRoute, int id, string? returnUrl, CancellationToken cancellationToken)
    {
        var definition = MasterCatalogRegistry.GetByRoute(catalogRoute);
        if (definition is null || definition.Code == "dominio" || definition.IsReadOnly)
        {
            return NotFound();
        }
        var record = await catalogService.GetAsync(definition, id, cancellationToken);
        return record is null ? NotFound() : View("CatalogEditor", new CatalogEditorViewModel
        {
            Definition = definition,
            Record = record,
            Options = await catalogService.GetOptionsAsync(definition, cancellationToken),
            ReturnUrl = returnUrl
        });
    }

    [HttpPost("{catalogRoute}/create")]
    [Authorize(Policy = Permissions.CatalogCreate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCatalog(string catalogRoute, CatalogInputModel input, string? returnUrl, CancellationToken cancellationToken)
    {
        var definition = MasterCatalogRegistry.GetByRoute(catalogRoute);
        if (definition is null || definition.Code == "dominio" || definition.IsReadOnly)
        {
            return NotFound();
        }
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Forbid();
        }
        if (!await HasCorporateScopeAsync(actorUserId, cancellationToken)) return Forbid();
        try
        {
            var id = await catalogService.CreateAsync(definition, input.Values, actorUserId, HttpContext.TraceIdentifier, cancellationToken);
            TempData["SuccessMessage"] = $"{definition.Name} se creó correctamente.";
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(CatalogDetails), new { catalogRoute, id });
        }
        catch (CatalogValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al crear {Catalog}. Correlación: {CorrelationId}", definition.Name, HttpContext.TraceIdentifier);
            TempData["ErrorMessage"] = $"No fue posible crear {definition.Name}. Intente nuevamente.";
        }
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return RedirectToAction(nameof(CatalogCreateForm), new { catalogRoute, returnUrl });
        }
        return RedirectToAction(definition.EditorMode == CatalogEditorMode.Page ? nameof(CatalogCreateForm) : nameof(Catalog), new { catalogRoute });
    }

    [HttpPost("{catalogRoute}/edit/{id:int}")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCatalog(string catalogRoute, int id, CatalogInputModel input, string? returnUrl, CancellationToken cancellationToken)
    {
        var definition = MasterCatalogRegistry.GetByRoute(catalogRoute);
        if (definition is null || definition.Code == "dominio" || definition.IsReadOnly)
        {
            return NotFound();
        }
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Forbid();
        }
        if (!await HasCorporateScopeAsync(actorUserId, cancellationToken)) return Forbid();
        try
        {
            var current = await catalogService.GetAsync(definition, id, cancellationToken);
            if (current is null) return NotFound();
            if (string.IsNullOrWhiteSpace(input.ConcurrencyToken) || !string.Equals(input.ConcurrencyToken, CatalogConcurrencyToken.Create(current), StringComparison.Ordinal))
            {
                TempData["ErrorMessage"] = "El registro fue modificado por otro usuario. Revise los cambios antes de volver a guardar.";
                return RedirectToAction(nameof(CatalogEditForm), new { catalogRoute, id, returnUrl });
            }
            if (!await catalogService.UpdateAsync(definition, id, input.Values, actorUserId, HttpContext.TraceIdentifier, cancellationToken))
            {
                return NotFound();
            }
            TempData["SuccessMessage"] = $"{definition.Name} se actualizó correctamente.";
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(CatalogDetails), new { catalogRoute, id });
        }
        catch (CatalogValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al editar {Catalog}. Correlación: {CorrelationId}", definition.Name, HttpContext.TraceIdentifier);
            TempData["ErrorMessage"] = $"No fue posible actualizar {definition.Name}. Intente nuevamente.";
        }
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return RedirectToAction(nameof(CatalogEditForm), new { catalogRoute, id, returnUrl });
        }
        return RedirectToAction(definition.EditorMode == CatalogEditorMode.Page ? nameof(CatalogEditForm) : nameof(CatalogDetails), new { catalogRoute, id });
    }

    [HttpGet("building-block/{id:int}/capability-candidates")]
    public async Task<IActionResult> CapabilityCandidates(int id, string? search, CancellationToken cancellationToken)
    {
        var candidates = await associationImpactService.GetCapabilityCandidatesAsync(id, search, cancellationToken);
        return Json(candidates);
    }

    [HttpGet("building-block/{id:int}/capability-reassign-impact")]
    public async Task<IActionResult> CapabilityReassignImpact(int id, int capabilityId, int targetBuildingBlockId, CancellationToken cancellationToken)
    {
        var impact = await associationImpactService.PreviewCapabilityReassignmentAsync(capabilityId, targetBuildingBlockId, cancellationToken);
        return impact is null ? NotFound() : Json(impact);
    }

    [HttpGet("building-block/{id:int}/functionality-candidates")]
    public async Task<IActionResult> FunctionalityCandidates(int id, int? capabilityId, string? search, CancellationToken cancellationToken)
    {
        var candidates = await associationImpactService.GetFunctionalityCandidatesAsync(id, capabilityId, search, cancellationToken);
        return Json(candidates);
    }

    [HttpGet("building-block/{id:int}/functionality-reassign-impact")]
    public async Task<IActionResult> FunctionalityReassignImpact(int id, int functionalityId, int targetCapabilityId, CancellationToken cancellationToken)
    {
        var impact = await associationImpactService.PreviewFunctionalityReassignmentAsync(functionalityId, targetCapabilityId, cancellationToken);
        return impact is null ? NotFound() : Json(impact);
    }

    [HttpGet("building-block-options")]
    public async Task<IActionResult> GetBuildingBlockOptions(CancellationToken cancellationToken)
    {
        var capDefinition = MasterCatalogRegistry.GetByCode("capacidad-seguridad");
        if (capDefinition is null) return NotFound();
        var options = await catalogService.GetOptionsAsync(capDefinition, cancellationToken);
        if (options.TryGetValue("idBuildingBlock", out var list))
        {
            return Json(list.Select(o => new { id = o.Id, name = o.Label }));
        }
        return Json(Array.Empty<object>());
    }

    [HttpGet("capability-options")]
    public async Task<IActionResult> GetCapabilityOptions(CancellationToken cancellationToken)
    {
        var funcDefinition = MasterCatalogRegistry.GetByCode("funcionalidad");
        if (funcDefinition is null) return NotFound();
        var options = await catalogService.GetOptionsAsync(funcDefinition, cancellationToken);
        if (options.TryGetValue("idCapacidad", out var list))
        {
            return Json(list.Select(o => new { id = o.Id, name = o.Label }));
        }
        return Json(Array.Empty<object>());
    }

    [HttpPost("building-block/{id:int}/associate-capability")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssociateCapability(int id, [FromForm] int capabilityId, [FromForm] string concurrencyToken, [FromForm] string? justification, CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Forbid();
        if (!await HasCorporateScopeAsync(actorUserId, cancellationToken)) return Forbid();

        var command = new AssignCapabilityCommand(capabilityId, id, concurrencyToken, justification, actorUserId, HttpContext.TraceIdentifier);
        var result = await assignmentService.AssignCapabilityAsync(command, cancellationToken);
        if (result.IsConcurrencyConflict)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return StatusCode(StatusCodes.Status409Conflict, new { message = result.ErrorMessage });
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id });
        }
        if (!result.Succeeded)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest(new { message = result.ErrorMessage });
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id });
        }

        TempData["SuccessMessage"] = "La Capacidad de Seguridad fue asociada exitosamente.";
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Ok(new { message = TempData["SuccessMessage"] });
        return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id });
    }

    [HttpPost("building-block/{id:int}/reassign-capability")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReassignCapability(int id, [FromForm] int capabilityId, [FromForm] int sourceBuildingBlockId, [FromForm] int targetBuildingBlockId, [FromForm] string concurrencyToken, [FromForm] string? justification, CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Forbid();
        if (!await HasCorporateScopeAsync(actorUserId, cancellationToken)) return Forbid();

        var command = new ReassignCapabilityCommand(capabilityId, sourceBuildingBlockId, targetBuildingBlockId, concurrencyToken, justification, actorUserId, HttpContext.TraceIdentifier);
        var result = await assignmentService.ReassignCapabilityAsync(command, cancellationToken);
        if (result.IsConcurrencyConflict)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return StatusCode(StatusCodes.Status409Conflict, new { message = result.ErrorMessage });
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id });
        }
        if (!result.Succeeded)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest(new { message = result.ErrorMessage });
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id });
        }

        TempData["SuccessMessage"] = "La Capacidad de Seguridad fue reasignada exitosamente.";
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Ok(new { message = TempData["SuccessMessage"] });
        return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id });
    }

    [HttpPost("building-block/{id:int}/associate-functionality")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssociateFunctionality(int id, [FromForm] int functionalityId, [FromForm] int targetCapabilityId, [FromForm] string concurrencyToken, [FromForm] string? justification, CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Forbid();
        if (!await HasCorporateScopeAsync(actorUserId, cancellationToken)) return Forbid();

        var command = new AssignFunctionalityCommand(functionalityId, targetCapabilityId, concurrencyToken, justification, actorUserId, HttpContext.TraceIdentifier);
        var result = await assignmentService.AssignFunctionalityAsync(command, cancellationToken);
        if (result.IsConcurrencyConflict)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return StatusCode(StatusCodes.Status409Conflict, new { message = result.ErrorMessage });
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id });
        }
        if (!result.Succeeded)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest(new { message = result.ErrorMessage });
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id });
        }

        TempData["SuccessMessage"] = "La Funcionalidad fue asociada exitosamente.";
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Ok(new { message = TempData["SuccessMessage"] });
        return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id });
    }

    [HttpPost("building-block/{id:int}/reassign-functionality")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReassignFunctionality(int id, [FromForm] int functionalityId, [FromForm] int sourceCapabilityId, [FromForm] int targetCapabilityId, [FromForm] string concurrencyToken, [FromForm] string? justification, CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Forbid();
        if (!await HasCorporateScopeAsync(actorUserId, cancellationToken)) return Forbid();

        var command = new ReassignFunctionalityCommand(functionalityId, sourceCapabilityId, targetCapabilityId, concurrencyToken, justification, actorUserId, HttpContext.TraceIdentifier);
        var result = await assignmentService.ReassignFunctionalityAsync(command, cancellationToken);
        if (result.IsConcurrencyConflict)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return StatusCode(StatusCodes.Status409Conflict, new { message = result.ErrorMessage });
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id });
        }
        if (!result.Succeeded)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest(new { message = result.ErrorMessage });
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id });
        }

        TempData["SuccessMessage"] = "La Funcionalidad fue reasignada exitosamente.";
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Ok(new { message = TempData["SuccessMessage"] });
        return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id });
    }

    [HttpPost("building-block/{id:int}/quick-update")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBuildingBlockQuickFields(
        int id,
        [FromForm] string? faseAdopcion,
        [FromForm] string? rutaEntregable,
        CancellationToken cancellationToken)
    {
        var definition = MasterCatalogRegistry.GetByCode("building-block")!;
        if (!TryGetActorUserId(out var actorUserId)) return Forbid();
        if (!await HasCorporateScopeAsync(actorUserId, cancellationToken)) return Forbid();

        var existing = await catalogService.GetAsync(definition, id, cancellationToken);
        if (existing is null) return NotFound();

        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var col in definition.Columns)
        {
            values[col.Code] = existing.Values.GetValueOrDefault(col.Code)?.ToString();
        }
        values["faseAdopcion"] = faseAdopcion;
        values["rutaEntregable"] = rutaEntregable;

        try
        {
            await catalogService.UpdateAsync(definition, id, values, actorUserId, HttpContext.TraceIdentifier, cancellationToken);
            TempData["SuccessMessage"] = "Campos del Building Block actualizados correctamente.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al actualizar campos rápidos de Building Block {Id}. Correlación: {CorrelationId}", id, HttpContext.TraceIdentifier);
            TempData["ErrorMessage"] = "No fue posible actualizar los campos del Building Block.";
        }

        return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id });
    }

    [HttpPost("building-block/{buildingBlockId:int}/create-capability")]
    [Authorize(Policy = Permissions.CatalogCreate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCapabilityForBuildingBlock(
        int buildingBlockId,
        [FromForm] string nombre,
        [FromForm] string? estado,
        [FromForm] string? descripcion,
        CancellationToken cancellationToken)
    {
        var capDef = MasterCatalogRegistry.GetByCode("capacidad-seguridad")!;
        if (!TryGetActorUserId(out var actorUserId)) return Forbid();
        if (!await HasCorporateScopeAsync(actorUserId, cancellationToken)) return Forbid();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            TempData["ErrorMessage"] = "El nombre de la Capacidad es obligatorio.";
            return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id = buildingBlockId });
        }

        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["buildingBlock"] = buildingBlockId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["nombre"] = nombre.Trim(),
            ["estado"] = estado,
            ["descripcion"] = descripcion
        };

        try
        {
            await catalogService.CreateAsync(capDef, values, actorUserId, HttpContext.TraceIdentifier, cancellationToken);
            TempData["SuccessMessage"] = $"Capacidad \"{nombre.Trim()}\" creada correctamente.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear capacidad para Building Block {BuildingBlockId}. Correlación: {CorrelationId}", buildingBlockId, HttpContext.TraceIdentifier);
            TempData["ErrorMessage"] = "No fue posible crear la capacidad.";
        }

        return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id = buildingBlockId });
    }

    [HttpPost("building-block/{buildingBlockId:int}/create-functionality")]
    [Authorize(Policy = Permissions.CatalogCreate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFunctionalityForBuildingBlock(
        int buildingBlockId,
        [FromForm] int capacidadId,
        [FromForm] string nombre,
        [FromForm] string? estado,
        [FromForm] string? descripcion,
        CancellationToken cancellationToken)
    {
        var funcDef = MasterCatalogRegistry.GetByCode("funcionalidad")!;
        if (!TryGetActorUserId(out var actorUserId)) return Forbid();
        if (!await HasCorporateScopeAsync(actorUserId, cancellationToken)) return Forbid();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            TempData["ErrorMessage"] = "El nombre de la Funcionalidad es obligatorio.";
            return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id = buildingBlockId });
        }

        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["capacidad"] = capacidadId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["nombre"] = nombre.Trim(),
            ["estado"] = estado,
            ["descripcion"] = descripcion
        };

        try
        {
            await catalogService.CreateAsync(funcDef, values, actorUserId, HttpContext.TraceIdentifier, cancellationToken);
            TempData["SuccessMessage"] = $"Funcionalidad \"{nombre.Trim()}\" creada correctamente.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear funcionalidad para Building Block {BuildingBlockId}. Correlación: {CorrelationId}", buildingBlockId, HttpContext.TraceIdentifier);
            TempData["ErrorMessage"] = "No fue posible crear la funcionalidad.";
        }

        return RedirectToAction(nameof(CatalogDetails), new { catalogRoute = "building-block", id = buildingBlockId });
    }

    private static void EnsureDomainIsEnabled()
    {
        if (!MasterCatalogRegistry.IsAdministrable("dominio"))
        {
            throw new InvalidOperationException("TMDominio no está habilitado en la lista blanca de catálogos.");
        }
    }

    private bool TryGetActorUserId(out Guid actorUserId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out actorUserId);

    private Task<bool> HasCorporateScopeAsync(Guid actorUserId, CancellationToken cancellationToken) =>
        effectiveAccessService.HasActiveCorporateScopeAsync(actorUserId, cancellationToken);
}