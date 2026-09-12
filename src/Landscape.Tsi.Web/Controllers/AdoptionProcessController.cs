using System.Security.Claims;

using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Infrastructure.Catalogs;
using Landscape.Tsi.Web.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Web.Controllers;

[Authorize(Policy = Permissions.CatalogView)]
[Route("Administration/AdoptionProcess")]
public sealed class AdoptionProcessController(
    IAdoptionProcessService adoptionService,
    CatalogDbContext dbContext) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var processes = await adoptionService.ListProcessesAsync(cancellationToken);
        return View(new AdoptionProcessIndexViewModel
        {
            Processes = processes
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, string? returnUrl, CancellationToken cancellationToken)
    {
        var detail = await adoptionService.GetProcessDetailAsync(id, cancellationToken);
        if (detail is null)
        {
            return NotFound();
        }

        var technologies = await dbContext.Technologies.AsNoTracking()
            .OrderBy(t => t.NombreCorporativo)
            .Select(t => new CatalogOption(t.Id, t.NombreCorporativo ?? $"Tecnología #{t.Id}"))
            .ToListAsync(cancellationToken);

        var companies = await dbContext.Companies.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CatalogOption(c.Id, c.Nombre ?? $"Empresa #{c.Id}"))
            .ToListAsync(cancellationToken);

        var opTypes = await dbContext.OperationTypes.AsNoTracking()
            .OrderBy(o => o.Nombre)
            .Select(o => new CatalogOption(o.Id, o.Nombre ?? $"Tipo #{o.Id}"))
            .ToListAsync(cancellationToken);

        var workModes = await dbContext.WorkModes.AsNoTracking()
            .OrderBy(w => w.Nombre)
            .Select(w => new CatalogOption(w.Id, w.Nombre ?? $"Modalidad #{w.Id}"))
            .ToListAsync(cancellationToken);

        var adoptionStates = await dbContext.TechnologyAdoptionStates.AsNoTracking()
            .OrderBy(s => s.Nombre)
            .Select(s => new CatalogOption(s.Id, s.Nombre ?? $"Estado #{s.Id}"))
            .ToListAsync(cancellationToken);

        return View(new AdoptionProcessDetailViewModel
        {
            Process = detail,
            Technologies = technologies,
            Companies = companies,
            OperationTypes = opTypes,
            WorkModes = workModes,
            AdoptionStates = adoptionStates,
            ReturnUrl = returnUrl
        });
    }

    [HttpGet("Create")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    public async Task<IActionResult> Create(int? buildingBlockId, CancellationToken cancellationToken)
    {
        var buildingBlocks = await dbContext.BuildingBlocks.AsNoTracking()
            .OrderBy(b => b.Nombre)
            .Select(b => new CatalogOption(b.Id, b.Nombre ?? $"Building Block #{b.Id}"))
            .ToListAsync(cancellationToken);

        var states = await dbContext.TechnologyAdoptionStates.AsNoTracking()
            .OrderBy(s => s.Nombre)
            .Select(s => new CatalogOption(s.Id, s.Nombre ?? $"Estado #{s.Id}"))
            .ToListAsync(cancellationToken);

        var defaultCode = $"PROC-TSI-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100, 999)}";

        return View(new CreateAdoptionProcessViewModel
        {
            Codigo = defaultCode,
            BuildingBlockId = buildingBlockId ?? 0,
            BuildingBlocks = buildingBlocks,
            AdoptionStates = states
        });
    }

    [HttpPost("Create")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAdoptionProcessViewModel model, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        if (string.IsNullOrWhiteSpace(model.Codigo) || string.IsNullOrWhiteSpace(model.Nombre) || model.BuildingBlockId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Código, nombre y Building Block son obligatorios.");
            model.BuildingBlocks = await dbContext.BuildingBlocks.AsNoTracking()
                .OrderBy(b => b.Nombre)
                .Select(b => new CatalogOption(b.Id, b.Nombre ?? $"Building Block #{b.Id}"))
                .ToListAsync(cancellationToken);
            model.AdoptionStates = await dbContext.TechnologyAdoptionStates.AsNoTracking()
                .OrderBy(s => s.Nombre)
                .Select(s => new CatalogOption(s.Id, s.Nombre ?? $"Estado #{s.Id}"))
                .ToListAsync(cancellationToken);
            return View(model);
        }

        var result = await adoptionService.CreateProcessAsync(new CreateAdoptionProcessCommand(
            Codigo: model.Codigo.Trim(),
            Nombre: model.Nombre.Trim(),
            BuildingBlockId: model.BuildingBlockId,
            EstadoAdopcionId: model.EstadoAdopcionId > 0 ? model.EstadoAdopcionId : 1,
            Objetivo: model.Objetivo?.Trim(),
            Alcance: model.Alcance?.Trim(),
            LiderCorporativo: model.LiderCorporativo?.Trim(),
            FechaInicio: model.FechaInicio,
            FechaEstimadaCierre: model.FechaEstimadaCierre,
            ActorUserId: actor.Value,
            CorrelationId: HttpContext.TraceIdentifier), cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            model.BuildingBlocks = await dbContext.BuildingBlocks.AsNoTracking()
                .OrderBy(b => b.Nombre)
                .Select(b => new CatalogOption(b.Id, b.Nombre ?? $"Building Block #{b.Id}"))
                .ToListAsync(cancellationToken);
            model.AdoptionStates = await dbContext.TechnologyAdoptionStates.AsNoTracking()
                .OrderBy(s => s.Nombre)
                .Select(s => new CatalogOption(s.Id, s.Nombre ?? $"Estado #{s.Id}"))
                .ToListAsync(cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = "Proceso de Adopción TSI creado exitosamente.";
        return RedirectToAction(nameof(Details), new { id = result.EntityId!.Value });
    }

    [HttpPost("{id:int}/Standard")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStandard(int id, int buildingBlockId, int tecnologiaId, string rolEstandar, DateTime fechaInicio, string? motivoCambio, string? sustentoArquitectura, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.SetCorporateStandardAsync(new SetCorporateStandardCommand(
            BuildingBlockId: buildingBlockId,
            TecnologiaId: tecnologiaId,
            ProcesoAdopcionId: id,
            RolEstandar: rolEstandar,
            FechaInicio: fechaInicio == default ? DateTime.Today : fechaInicio,
            MotivoCambio: motivoCambio?.Trim(),
            SustentoArquitectura: sustentoArquitectura?.Trim(),
            ActorUserId: actor.Value,
            CorrelationId: HttpContext.TraceIdentifier), cancellationToken);

        if (result.Succeeded)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/Convene")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConveneCompany(int id, int empresaId, int? contactoFocalId, bool aplica, string? justificacionNoAplica, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.ConveneCompanyAsync(new ConveneCompanyCommand(
            ProcesoId: id,
            EmpresaId: empresaId,
            ContactoFocalId: contactoFocalId,
            Aplica: aplica,
            JustificacionNoAplica: justificacionNoAplica?.Trim(),
            ActorUserId: actor.Value,
            CorrelationId: HttpContext.TraceIdentifier), cancellationToken);

        if (result.Succeeded)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/Technology")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterTechnology(int id, int empresaId, int tecnologiaId, int buildingBlockId, int? procesoEmpresaId, bool esPrimaria, string? versionDesplegada, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.RegisterImplementedTechnologyAsync(new RegisterImplementedTechnologyCommand(
            EmpresaId: empresaId,
            TecnologiaId: tecnologiaId,
            BuildingBlockId: buildingBlockId,
            ProcesoEmpresaId: procesoEmpresaId,
            EsPrimaria: esPrimaria,
            VersionDesplegada: versionDesplegada?.Trim(),
            ActorUserId: actor.Value,
            CorrelationId: HttpContext.TraceIdentifier), cancellationToken);

        if (result.Succeeded)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/Contract")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveContract(int id, int tecnologiaImplementadaId, string numeroContrato, bool esAdenda, int? contratoPadreId, DateTime fechaInicio, DateTime fechaFin, DateTime? fechaAdjudicacion, string? rutaDocumento, decimal? monto, string? moneda, string? observaciones, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.SaveContractAsync(new SaveContractCommand(
            TecnologiaImplementadaId: tecnologiaImplementadaId,
            NumeroContrato: numeroContrato,
            EsAdenda: esAdenda,
            ContratoPadreId: contratoPadreId,
            FechaInicio: fechaInicio == default ? DateTime.Today : fechaInicio,
            FechaFin: fechaFin == default ? DateTime.Today.AddYears(1) : fechaFin,
            FechaAdjudicacion: fechaAdjudicacion,
            RutaDocumento: rutaDocumento?.Trim(),
            Monto: monto,
            Moneda: string.IsNullOrWhiteSpace(moneda) ? "USD" : moneda.Trim(),
            Observaciones: observaciones?.Trim(),
            ActorUserId: actor.Value,
            CorrelationId: HttpContext.TraceIdentifier), cancellationToken);

        if (result.Succeeded)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/Contract/{contractId:int}/Delete")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteContract(int id, int contractId, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.DeleteContractAsync(contractId, actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        if (result.Succeeded)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/Driver")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDriver(int id, int tecnologiaImplementadaId, string descripcion, string? unidadMedida, decimal? cantidad, decimal? precioUnitario, string? moneda, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.SaveDriverAsync(new SaveDriverCommand(
            TecnologiaImplementadaId: tecnologiaImplementadaId,
            Descripcion: descripcion,
            UnidadMedida: unidadMedida?.Trim(),
            Cantidad: cantidad,
            PrecioUnitario: precioUnitario,
            Moneda: string.IsNullOrWhiteSpace(moneda) ? "USD" : moneda.Trim(),
            ActorUserId: actor.Value,
            CorrelationId: HttpContext.TraceIdentifier), cancellationToken);

        if (result.Succeeded)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/Driver/{driverId:int}/Delete")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDriver(int id, int driverId, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.DeleteDriverAsync(driverId, actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        if (result.Succeeded)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/OperationModel")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOperationModel(int id, int tecnologiaImplementadaId, int tipoOperacionId, int modalidadLaboralId, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.SaveOperationModelAsync(new SaveOperationModelCommand(
            TecnologiaImplementadaId: tecnologiaImplementadaId,
            TipoOperacionId: tipoOperacionId,
            ModalidadLaboralId: modalidadLaboralId,
            ActorUserId: actor.Value,
            CorrelationId: HttpContext.TraceIdentifier), cancellationToken);

        if (result.Succeeded)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/Status")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, int statusId, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.UpdateProcessStatusAsync(id, statusId, actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        if (result.Succeeded)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Details), new { id });
    }

    private Guid? ActorId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}