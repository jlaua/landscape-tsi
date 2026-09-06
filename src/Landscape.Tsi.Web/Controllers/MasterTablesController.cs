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
    ILogger<MasterTablesController> logger) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(MasterCatalogRegistry.Catalogs);

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
    public async Task<IActionResult> DomainDetails(int id, CancellationToken cancellationToken)
    {
        EnsureDomainIsEnabled();
        var record = await dominioService.GetAsync(id, cancellationToken);
        return record is null
            ? NotFound()
            : View(new DominioDetailViewModel { Record = record, Edit = DominioFormModel.FromRecord(record) });
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
    public async Task<IActionResult> Catalog(string catalogRoute, string? search, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
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

        return View(new CatalogPageViewModel
        {
            Definition = definition,
            Search = search,
            Result = await catalogService.ListAsync(definition, search, page, pageSize, cancellationToken),
            Options = await catalogService.GetOptionsAsync(definition, cancellationToken)
        });
    }

    [HttpGet("{catalogRoute}/details/{id:int}")]
    public async Task<IActionResult> CatalogDetails(string catalogRoute, int id, CancellationToken cancellationToken)
    {
        var definition = MasterCatalogRegistry.GetByRoute(catalogRoute);
        if (definition is null || definition.Code == "dominio")
        {
            return NotFound();
        }
        var record = await catalogService.GetAsync(definition, id, cancellationToken);
        return record is null ? NotFound() : View(new CatalogDetailViewModel
        {
            Definition = definition,
            Record = record,
            Options = await catalogService.GetOptionsAsync(definition, cancellationToken)
        });
    }

    [HttpGet("{catalogRoute}/create")]
    [Authorize(Policy = Permissions.CatalogCreate)]
    public async Task<IActionResult> CatalogCreateForm(string catalogRoute, CancellationToken cancellationToken)
    {
        var definition = MasterCatalogRegistry.GetByRoute(catalogRoute);
        if (definition is null || definition.Code == "dominio")
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
        if (definition is null || definition.Code == "dominio")
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
        if (definition is null || definition.Code == "dominio")
        {
            return NotFound();
        }
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Forbid();
        }
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
        if (definition is null || definition.Code == "dominio")
        {
            return NotFound();
        }
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Forbid();
        }
        try
        {
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
}
