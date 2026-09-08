using System.Security.Claims;

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
    public async Task<IActionResult> Catalog(string catalogRoute, string? search, int page = 1, int pageSize = 10, int? parentId = null, CancellationToken cancellationToken = default)
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
            : await catalogService.ListAsync(definition, search, page, pageSize, cancellationToken);
        return View(new CatalogPageViewModel
        {
            Definition = definition,
            Search = search,
            Result = result,
            Options = await catalogService.GetOptionsAsync(definition, cancellationToken)
        });
    }

    [HttpGet("{catalogRoute}/details/{id:int}")]
    public async Task<IActionResult> CatalogDetails(string catalogRoute, int id, string? capabilitySearch, string? functionalitySearch, string? technologySearch, int capabilityPage = 1, int functionalityPage = 1, int technologyPage = 1, int relatedPageSize = 10, CancellationToken cancellationToken = default)
    {
        var definition = MasterCatalogRegistry.GetByRoute(catalogRoute);
        if (definition is null || definition.Code == "dominio")
        {
            return NotFound();
        }
        var record = await catalogService.GetAsync(definition, id, cancellationToken);
        if (record is null) return NotFound();
        var related = definition.Code == "building-block"
            ? await buildingBlockRelatedService.GetAsync(id, capabilitySearch, functionalitySearch, technologySearch, capabilityPage, functionalityPage, technologyPage, relatedPageSize, cancellationToken)
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
        return View(new CatalogDetailViewModel
        {
            Definition = definition,
            Record = record,
            Options = await catalogService.GetOptionsAsync(definition, cancellationToken),
            Related = related,
            RelatedRecords = relatedRecords,
            TechnologyMapping = technologyMapping,
            TechnologyRelations = technologyRelations
        });
    }

    [HttpGet("{catalogRoute}/{id:int}/delete-impact")]
    [Authorize(Policy = Permissions.CatalogDelete)]
    public async Task<IActionResult> CatalogDeleteImpact(string catalogRoute, int id, CancellationToken cancellationToken)
    {
        var definition = MasterCatalogRegistry.GetByRoute(catalogRoute);
        if (definition is null || !definition.IsDeletable) return NotFound();
        var impact = await deletionImpactService.PreviewAsync(definition.Code, id, cancellationToken);
        return impact is null ? NotFound() : Json(impact);
    }

    [HttpPost("{catalogRoute}/{id:int}/delete")]
    [Authorize(Policy = Permissions.CatalogDelete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCatalog(string catalogRoute, int id, string? confirmation, CancellationToken cancellationToken)
    {
        var definition = MasterCatalogRegistry.GetByRoute(catalogRoute);
        if (definition is null || !definition.IsDeletable) return NotFound();
        if (!TryGetActorUserId(out var actorUserId)) return Forbid();
        if (!await HasCorporateScopeAsync(actorUserId, cancellationToken)) return Forbid();
        try
        {
            var result = await deletionImpactService.DeleteAsync(definition.Code, id, confirmation, actorUserId, HttpContext.TraceIdentifier, cancellationToken);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? $"Registro eliminado correctamente. Se eliminaron {result.TotalRecordsDeleted} registros relacionados."
                : result.ErrorMessage ?? "No fue posible eliminar el registro.";
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Catalog), new { catalogRoute });
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al eliminar {Catalog}. Correlación: {CorrelationId}", definition.Name, HttpContext.TraceIdentifier);
            TempData["ErrorMessage"] = "No fue posible eliminar el registro. La operación fue revertida.";
        }
        return RedirectToAction(nameof(CatalogDetails), new { catalogRoute, id });
    }

    [HttpGet("{catalogRoute}/create")]
    [Authorize(Policy = Permissions.CatalogCreate)]
    public async Task<IActionResult> CatalogCreateForm(string catalogRoute, CancellationToken cancellationToken)
    {
        var definition = MasterCatalogRegistry.GetByRoute(catalogRoute);
        if (definition is null || definition.Code == "dominio" || definition.IsReadOnly)
        {
            return NotFound();
        }
        if (definition.EditorMode == CatalogEditorMode.Modal)
        {
            return RedirectToAction(nameof(Catalog), new { catalogRoute });
        }
        return View("CatalogEditor", new CatalogEditorViewModel
        {
            Definition = definition,
            Options = await catalogService.GetOptionsAsync(definition, cancellationToken)
        });
    }

    [HttpGet("{catalogRoute}/edit/{id:int}")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    public async Task<IActionResult> CatalogEditForm(string catalogRoute, int id, CancellationToken cancellationToken)
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
            Options = await catalogService.GetOptionsAsync(definition, cancellationToken)
        });
    }

    [HttpPost("{catalogRoute}/create")]
    [Authorize(Policy = Permissions.CatalogCreate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCatalog(string catalogRoute, CatalogInputModel input, CancellationToken cancellationToken)
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
        return RedirectToAction(definition.EditorMode == CatalogEditorMode.Page ? nameof(CatalogCreateForm) : nameof(Catalog), new { catalogRoute });
    }

    [HttpPost("{catalogRoute}/edit/{id:int}")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCatalog(string catalogRoute, int id, CatalogInputModel input, CancellationToken cancellationToken)
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
                return RedirectToAction(nameof(CatalogEditForm), new { catalogRoute, id });
            }
            if (!await catalogService.UpdateAsync(definition, id, input.Values, actorUserId, HttpContext.TraceIdentifier, cancellationToken))
            {
                return NotFound();
            }
            TempData["SuccessMessage"] = $"{definition.Name} se actualizó correctamente.";
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
        return RedirectToAction(definition.EditorMode == CatalogEditorMode.Page ? nameof(CatalogEditForm) : nameof(CatalogDetails), new { catalogRoute, id });
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
