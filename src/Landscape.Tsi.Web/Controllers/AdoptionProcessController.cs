using System.Data;
using System.Security.Claims;

using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Catalogs;
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

        if (tecnologiaImplementadaId <= 0)
        {
            TempData["ErrorMessage"] = "Debe especificar una tecnología implementada válida para configurar su modelo de operación.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
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
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al guardar el modelo de operación: {ex.Message}";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/Status")]
    [HttpPost("Evaluations/{id:int}/Status")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, [FromForm] int statusId, [FromForm] string? returnUrl, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.UpdateProcessStatusAsync(id, statusId, actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        if (result.Succeeded)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("Evaluations")]
    [HttpGet("/AdoptionProcess/Evaluations")]
    public async Task<IActionResult> Evaluations(string? search, int? dominioId, int? estadoId, CancellationToken cancellationToken)
    {
        var rawProcesses = await adoptionService.ListProcessesAsync(cancellationToken);

        var bbs = await dbContext.BuildingBlocks.AsNoTracking()
            .Select(b => new { b.Id, b.Nombre, b.IdDominio })
            .ToListAsync(cancellationToken);
        var bbMap = bbs.ToDictionary(b => b.Id);

        var dominios = await dbContext.Domains.AsNoTracking()
            .OrderBy(d => d.Dominio)
            .Select(d => new CatalogOption(d.Id, d.Dominio ?? $"Dominio #{d.Id}"))
            .ToListAsync(cancellationToken);
        var domMap = dominios.ToDictionary(d => d.Id, d => d.Label);

        var estados = await dbContext.TechnologyAdoptionStates.AsNoTracking()
            .OrderBy(s => s.Nombre)
            .Select(s => new CatalogOption(s.Id, s.Nombre ?? $"Estado #{s.Id}"))
            .ToListAsync(cancellationToken);

        var items = rawProcesses.Select(p =>
        {
            bbMap.TryGetValue(p.BuildingBlockId, out var bb);
            var domNombre = (bb is not null && bb.IdDominio.HasValue && domMap.TryGetValue(bb.IdDominio.Value, out var dName)) ? dName : "Dominio TSI";
            var isBaja = p.Nombre.StartsWith("[DADO DE BAJA:", StringComparison.OrdinalIgnoreCase)
                         || p.Codigo.StartsWith("BAJA-", StringComparison.OrdinalIgnoreCase);

            return new EvaluationProcessSummaryViewModel
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Nombre = p.Nombre,
                BuildingBlockId = p.BuildingBlockId,
                BuildingBlockName = p.BuildingBlockNombre,
                DominioName = domNombre,
                LiderCorporativo = p.LiderCorporativo,
                EstadoId = p.EstadoAdopcionId,
                EstadoAdopcion = p.EstadoAdopcionNombre,
                FaseAdopcion = "EVALUACION",
                FechaInicio = p.FechaInicio,
                FechaEstimadaCierre = p.FechaEstimadaCierre,
                TotalEmpresas = p.TotalEmpresasConvocadas,
                EmpresasConAdopcion = p.TotalEmpresasImplementadas,
                EmpresasNoAplica = p.TotalEmpresasNoAplica,
                IsActivo = !isBaja
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            items = items.Where(i =>
                i.Codigo.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.Nombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.BuildingBlockName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.DominioName.Contains(term, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        if (dominioId.HasValue && dominioId.Value > 0)
        {
            var bbIdsInDomain = bbs.Where(b => b.IdDominio == dominioId.Value).Select(b => b.Id).ToHashSet();
            items = items.Where(i => bbIdsInDomain.Contains(i.BuildingBlockId)).ToList();
        }

        if (estadoId.HasValue && estadoId.Value > 0)
        {
            items = items.Where(i => i.EstadoId == estadoId.Value).ToList();
        }

        var vm = new EvaluationsIndexViewModel
        {
            Processes = items,
            Search = search,
            DominioId = dominioId,
            EstadoId = estadoId,
            Dominios = dominios,
            BuildingBlocks = bbs.Select(b => new CatalogOption(b.Id, b.Nombre ?? $"BB #{b.Id}")).ToList(),
            EstadosAdopcion = estados
        };

        return View("Evaluations", vm);
    }

    [HttpGet("Evaluations/Create")]
    [HttpGet("/AdoptionProcess/Evaluations/Create")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    public async Task<IActionResult> CreateEvaluation(int? buildingBlockId, CancellationToken cancellationToken)
    {
        var defaultCode = $"EVAL-TSI-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100, 999)}";
        var model = new CreateEvaluationViewModel
        {
            Codigo = defaultCode,
            BuildingBlockId = buildingBlockId ?? 0,
            FechaInicio = DateTime.Today
        };

        await PopulateCreateEvaluationCatalogsAsync(model, cancellationToken);

        if (buildingBlockId.HasValue && buildingBlockId.Value > 0)
        {
            var bb = await dbContext.BuildingBlocks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == buildingBlockId.Value, cancellationToken);
            if (bb != null)
            {
                model.DominioId = bb.IdDominio ?? 0;
            }
        }

        return View("CreateEvaluation", model);
    }

    [HttpGet("Evaluations/CapabilitiesPreview/{buildingBlockId:int}")]
    [HttpGet("/AdoptionProcess/Evaluations/CapabilitiesPreview/{buildingBlockId:int}")]
    public async Task<IActionResult> CapabilitiesPreview(int buildingBlockId, CancellationToken cancellationToken)
    {
        var dto = await adoptionService.GetBuildingBlockCapabilitiesAsync(buildingBlockId, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }
        return Json(dto);
    }

    [HttpPost("Evaluations/Create")]
    [HttpPost("/AdoptionProcess/Evaluations/Create")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEvaluation(CreateEvaluationViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCreateEvaluationCatalogsAsync(model, cancellationToken);
            return View("CreateEvaluation", model);
        }

        var actor = ActorId();
        if (actor is null) return Forbid();

        var createCmd = new CreateAdoptionProcessCommand(
            Codigo: model.Codigo,
            Nombre: model.Nombre,
            BuildingBlockId: model.BuildingBlockId,
            EstadoAdopcionId: model.EstadoAdopcionId,
            Objetivo: model.Objetivo,
            Alcance: model.Alcance,
            LiderCorporativo: model.LiderCorporativo,
            FechaInicio: model.FechaInicio,
            FechaEstimadaCierre: model.FechaEstimadaCierre,
            ActorUserId: actor.Value,
            CorrelationId: HttpContext.TraceIdentifier);

        var procResult = await adoptionService.CreateProcessAsync(createCmd, cancellationToken);
        if (!procResult.Succeeded || !procResult.EntityId.HasValue)
        {
            TempData["ErrorMessage"] = procResult.Message;
            await PopulateCreateEvaluationCatalogsAsync(model, cancellationToken);
            return View("CreateEvaluation", model);
        }

        var procesoId = procResult.EntityId.Value;

        var selectedCompanies = model.Subsidiaries
            .Where(s => s.Selected)
            .Select(s => new ConveneCompanyInput(
                s.EmpresaId,
                s.ContactoFocalId,
                s.Aplica,
                s.Aplica ? null : s.JustificacionNoAplica))
            .ToList();

        if (selectedCompanies.Count > 0)
        {
            await adoptionService.BatchConveneCompaniesAsync(procesoId, selectedCompanies, actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        }

        if (model.TecnologiaEstandarId.HasValue && model.TecnologiaEstandarId.Value > 0)
        {
            var sustento = $"Estándar corporativo seleccionado en evaluación. Vendor: {model.VendorCorporativo ?? "N/A"}. Contrato: {model.NumeroContratoCorporativo ?? "N/A"}";
            await adoptionService.SetCorporateStandardAsync(new SetCorporateStandardCommand(
                BuildingBlockId: model.BuildingBlockId,
                TecnologiaId: model.TecnologiaEstandarId.Value,
                ProcesoAdopcionId: procesoId,
                RolEstandar: "PRINCIPAL",
                FechaInicio: model.FechaInicio,
                MotivoCambio: "Definición de estándar corporativo durante evaluación TSI",
                SustentoArquitectura: sustento,
                ActorUserId: actor.Value,
                CorrelationId: HttpContext.TraceIdentifier), cancellationToken);
        }

        if (model.SubsidiaryAsIsList is not null)
        {
            foreach (var asIs in model.SubsidiaryAsIsList.Where(a => a.TieneTecnologia && a.TecnologiaId.HasValue && a.TecnologiaId.Value > 0))
            {
                var regCmd = new RegisterImplementedTechnologyCommand(
                    EmpresaId: asIs.EmpresaId,
                    TecnologiaId: asIs.TecnologiaId!.Value,
                    BuildingBlockId: model.BuildingBlockId,
                    ProcesoEmpresaId: null,
                    EsPrimaria: true,
                    VersionDesplegada: asIs.VersionDesplegada,
                    ActorUserId: actor.Value,
                    CorrelationId: HttpContext.TraceIdentifier);

                var regResult = await adoptionService.RegisterImplementedTechnologyAsync(regCmd, cancellationToken);
                if (regResult.Succeeded && regResult.EntityId.HasValue)
                {
                    var implId = regResult.EntityId.Value;

                    if (!string.IsNullOrWhiteSpace(asIs.NumeroContrato))
                    {
                        await adoptionService.SaveContractAsync(new SaveContractCommand(
                            TecnologiaImplementadaId: implId,
                            NumeroContrato: asIs.NumeroContrato,
                            EsAdenda: false,
                            ContratoPadreId: null,
                            FechaInicio: asIs.FechaInicioContrato ?? DateTime.Today,
                            FechaFin: asIs.FechaFinContrato ?? DateTime.Today.AddYears(1),
                            FechaAdjudicacion: null,
                            RutaDocumento: null,
                            Monto: asIs.MontoContratado,
                            Moneda: string.IsNullOrWhiteSpace(asIs.MonedaContrato) ? "USD" : asIs.MonedaContrato,
                            Observaciones: $"Vendor: {asIs.VendorNombre} / Partner: {asIs.PartnerNombre}",
                            ActorUserId: actor.Value,
                            CorrelationId: HttpContext.TraceIdentifier), cancellationToken);
                    }

                    if (asIs.Drivers != null)
                    {
                        foreach (var driver in asIs.Drivers.Where(d => !string.IsNullOrWhiteSpace(d.DescripcionDriver)))
                        {
                            await adoptionService.SaveDriverAsync(new SaveDriverCommand(
                                TecnologiaImplementadaId: implId,
                                Descripcion: driver.DescripcionDriver!,
                                UnidadMedida: driver.UnidadMedida,
                                Cantidad: driver.Cantidad,
                                PrecioUnitario: driver.PrecioUnitario,
                                Moneda: string.IsNullOrWhiteSpace(driver.Moneda) ? "USD" : driver.Moneda,
                                ActorUserId: actor.Value,
                                CorrelationId: HttpContext.TraceIdentifier), cancellationToken);
                        }
                    }

                    if (asIs.TipoOperacionId.HasValue && asIs.ModalidadLaboralId.HasValue)
                    {
                        await adoptionService.SaveOperationModelAsync(new SaveOperationModelCommand(
                            TecnologiaImplementadaId: implId,
                            TipoOperacionId: asIs.TipoOperacionId.Value,
                            ModalidadLaboralId: asIs.ModalidadLaboralId.Value,
                            ActorUserId: actor.Value,
                            CorrelationId: HttpContext.TraceIdentifier), cancellationToken);
                    }
                }
            }
        }

        TempData["SuccessMessage"] = $"Proceso de evaluación '{model.Codigo}' registrado exitosamente.";
        return RedirectToAction(nameof(Evaluations));
    }

    [HttpPost("Evaluations/{id:int}/Deactivate")]
    [HttpPost("/AdoptionProcess/Evaluations/{id:int}/Deactivate")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateEvaluation(int id, string motivoBaja, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        if (string.IsNullOrWhiteSpace(motivoBaja))
        {
            TempData["ErrorMessage"] = "Debe proporcionar un motivo válido para dar de baja la evaluación.";
            return RedirectToAction(nameof(Evaluations));
        }

        var result = await adoptionService.DeactivateProcessAsync(id, motivoBaja.Trim(), actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        if (result.Succeeded)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Evaluations));
    }

    [HttpGet("Evaluations/{id:int}/Edit")]
    [HttpGet("/AdoptionProcess/Evaluations/{id:int}/Edit")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    public async Task<IActionResult> EditEvaluation(int id, CancellationToken cancellationToken)
    {
        var proc = await dbContext.AdoptionProcesses.AsNoTracking().FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == id, cancellationToken);
        if (proc is null) return NotFound();

        var bb = await dbContext.BuildingBlocks.AsNoTracking().FirstOrDefaultAsync(b => b.Id == proc.IdBuildingBlock, cancellationToken);
        var dom = bb?.IdDominio.HasValue == true
            ? await dbContext.Domains.AsNoTracking().FirstOrDefaultAsync(d => d.Id == bb.IdDominio.Value, cancellationToken)
            : null;

        var estados = await dbContext.TechnologyAdoptionStates.AsNoTracking()
            .OrderBy(s => s.Nombre)
            .Select(s => new CatalogOption(s.Id, s.Nombre ?? $"Estado #{s.Id}"))
            .ToListAsync(cancellationToken);

        var vm = new EditEvaluationViewModel
        {
            Id = proc.IdProcesoAdopcionTSI,
            Codigo = proc.CodigoProceso,
            Nombre = proc.NombreProceso,
            BuildingBlockId = proc.IdBuildingBlock,
            BuildingBlockNombre = bb?.Nombre ?? $"BB #{proc.IdBuildingBlock}",
            DominioNombre = dom?.Dominio ?? "Dominio TSI",
            EstadoAdopcionId = proc.IdEstadoAdopcionTSI,
            EstadosAdopcion = estados,
            Objetivo = proc.Objetivo,
            Alcance = proc.Alcance,
            LiderCorporativo = proc.LiderCorporativoTSI,
            FechaInicio = proc.FechaInicio,
            FechaEstimadaCierre = proc.FechaEstimadaCierre
        };

        var principalStandard = await dbContext.StandardTechnologyHistories.AsNoTracking()
            .Where(s => s.IdBuildingBlock == proc.IdBuildingBlock && s.RolEstandar == "PRINCIPAL" && s.EstadoVigencia == "ACTIVO_VIGENTE")
            .FirstOrDefaultAsync(cancellationToken);

        var techName = principalStandard is not null
            ? await dbContext.Technologies.AsNoTracking()
                .Where(t => t.Id == principalStandard.IdTecnologiaTSI)
                .Select(t => t.NombreCorporativo ?? t.NombreLocal)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var availableTechs = await dbContext.Technologies.AsNoTracking()
            .OrderBy(t => t.NombreCorporativo)
            .Select(t => new CatalogOption(t.Id, t.NombreCorporativo ?? t.NombreLocal ?? $"Tecnología #{t.Id}"))
            .ToListAsync(cancellationToken);

        vm.TecnologiaEstandarId = principalStandard?.IdTecnologiaTSI;
        vm.TecnologiaEstandarNombre = techName;
        vm.RolEstandar = principalStandard?.RolEstandar ?? "PRINCIPAL";
        vm.TecnologiasDisponibles = availableTechs;

        return View("EditEvaluation", vm);
    }

    [HttpPost("Evaluations/{id:int}/Edit")]
    [HttpPost("/AdoptionProcess/Evaluations/{id:int}/Edit")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEditEvaluation(int id, EditEvaluationViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.EstadosAdopcion = await dbContext.TechnologyAdoptionStates.AsNoTracking()
                .OrderBy(s => s.Nombre)
                .Select(s => new CatalogOption(s.Id, s.Nombre ?? $"Estado #{s.Id}"))
                .ToListAsync(cancellationToken);
            model.TecnologiasDisponibles = await dbContext.Technologies.AsNoTracking()
                .OrderBy(t => t.NombreCorporativo)
                .Select(t => new CatalogOption(t.Id, t.NombreCorporativo ?? t.NombreLocal ?? $"Tecnología #{t.Id}"))
                .ToListAsync(cancellationToken);
            return View("EditEvaluation", model);
        }

        var actor = ActorId();
        if (actor is null) return Forbid();

        var proc = await dbContext.AdoptionProcesses.FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == id, cancellationToken);
        if (proc is null) return NotFound();

        var oldStateId = proc.IdEstadoAdopcionTSI;
        proc.NombreProceso = model.Nombre;
        proc.IdEstadoAdopcionTSI = model.EstadoAdopcionId;
        proc.LiderCorporativoTSI = model.LiderCorporativo;
        proc.Objetivo = model.Objetivo;
        proc.Alcance = model.Alcance;
        proc.FechaInicio = model.FechaInicio;
        proc.FechaEstimadaCierre = model.FechaEstimadaCierre;

        await dbContext.SaveChangesAsync(cancellationToken);

        if (oldStateId != model.EstadoAdopcionId)
        {
            await adoptionService.UpdateProcessStatusAsync(id, model.EstadoAdopcionId, actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        }

        // Si se definió o actualizó el estándar tecnológico corporativo oficial
        if (model.TecnologiaEstandarId.HasValue && model.TecnologiaEstandarId.Value > 0)
        {
            var currentStandard = await dbContext.StandardTechnologyHistories.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdBuildingBlock == proc.IdBuildingBlock && s.RolEstandar == "PRINCIPAL" && s.EstadoVigencia == "ACTIVO_VIGENTE", cancellationToken);

            if (currentStandard is null || currentStandard.IdTecnologiaTSI != model.TecnologiaEstandarId.Value)
            {
                await adoptionService.SetCorporateStandardAsync(new SetCorporateStandardCommand(
                    BuildingBlockId: proc.IdBuildingBlock,
                    TecnologiaId: model.TecnologiaEstandarId.Value,
                    ProcesoAdopcionId: proc.IdProcesoAdopcionTSI,
                    RolEstandar: "PRINCIPAL",
                    FechaInicio: DateTime.Today,
                    MotivoCambio: string.IsNullOrWhiteSpace(model.MotivoCambioEstandar) ? "Definición de estándar corporativo como resultado de la evaluación TSI" : model.MotivoCambioEstandar,
                    SustentoArquitectura: model.SustentoArquitecturaEstandar,
                    ActorUserId: actor.Value,
                    CorrelationId: HttpContext.TraceIdentifier), cancellationToken);
            }
        }

        TempData["SuccessMessage"] = $"Evaluación '{proc.CodigoProceso}' actualizada exitosamente.";
        return RedirectToAction(nameof(Evaluations));
    }

    private async Task PopulateCreateEvaluationCatalogsAsync(CreateEvaluationViewModel model, CancellationToken cancellationToken)
    {
        model.Dominios = await dbContext.Domains.AsNoTracking()
            .OrderBy(d => d.Dominio)
            .Select(d => new CatalogOption(d.Id, d.Dominio ?? $"Dominio #{d.Id}"))
            .ToListAsync(cancellationToken);

        model.BuildingBlocks = await dbContext.BuildingBlocks.AsNoTracking()
            .OrderBy(b => b.Nombre)
            .Select(b => new CatalogOption(b.Id, b.Nombre ?? $"Building Block #{b.Id}"))
            .ToListAsync(cancellationToken);

        model.EstadosAdopcion = await dbContext.TechnologyAdoptionStates.AsNoTracking()
            .OrderBy(s => s.Nombre)
            .Select(s => new CatalogOption(s.Id, s.Nombre ?? $"Estado #{s.Id}"))
            .ToListAsync(cancellationToken);

        model.TecnologiasDisponibles = await dbContext.Technologies.AsNoTracking()
            .OrderBy(t => t.NombreCorporativo)
            .Select(t => new CatalogOption(t.Id, t.NombreCorporativo ?? t.NombreLocal ?? $"Tecnología #{t.Id}"))
            .ToListAsync(cancellationToken);

        model.Familias = await dbContext.Families.AsNoTracking()
            .OrderBy(f => f.Nombre)
            .Select(f => new CatalogOption(f.Id, f.Nombre ?? $"Familia #{f.Id}"))
            .ToListAsync(cancellationToken);

        model.TecnologiasCatalogo = await (
            from t in dbContext.Technologies.AsNoTracking()
            join f in dbContext.Families.AsNoTracking() on t.IdFamilia equals f.Id into fGroup
            from fam in fGroup.DefaultIfEmpty()
            orderby t.NombreCorporativo
            select new TechnologyCatalogItem(
                t.Id,
                t.NombreCorporativo ?? t.NombreLocal ?? $"Tecnología #{t.Id}",
                fam != null ? fam.Nombre : null)
        ).ToListAsync(cancellationToken);

        model.TiposOperacion = await dbContext.OperationTypes.AsNoTracking()
            .OrderBy(o => o.Nombre)
            .Select(o => new CatalogOption(o.Id, o.Nombre ?? $"Tipo #{o.Id}"))
            .ToListAsync(cancellationToken);

        model.ModalidadesLaborales = await dbContext.WorkModes.AsNoTracking()
            .OrderBy(w => w.Nombre)
            .Select(w => new CatalogOption(w.Id, w.Nombre ?? $"Modalidad #{w.Id}"))
            .ToListAsync(cancellationToken);

        var contactsMap = await LoadContactsPerCompanyAsync(cancellationToken);

        if (model.Subsidiaries.Count == 0)
        {
            var companies = await dbContext.Companies.AsNoTracking()
                .OrderBy(c => c.Nombre)
                .ToListAsync(cancellationToken);

            model.Subsidiaries = companies.Select(c => new SubsidiaryCheckboxItem
            {
                EmpresaId = c.Id,
                EmpresaNombre = c.Nombre ?? $"Empresa #{c.Id}",
                Pais = c.Pais,
                Rubro = c.Rubro,
                Selected = true,
                Aplica = true,
                ContactosDisponibles = contactsMap.GetValueOrDefault(c.Id, [])
            }).ToList();

            model.SubsidiaryAsIsList = companies.Select(c => new SubsidiaryAsIsInputModel
            {
                EmpresaId = c.Id,
                EmpresaNombre = c.Nombre ?? $"Empresa #{c.Id}",
                TieneTecnologia = true,
                MonedaContrato = "USD"
            }).ToList();
        }
        else
        {
            foreach (var sub in model.Subsidiaries)
            {
                sub.ContactosDisponibles = contactsMap.GetValueOrDefault(sub.EmpresaId, []);
            }
        }
    }

    private async Task<Dictionary<int, List<CatalogOption>>> LoadContactsPerCompanyAsync(CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, List<CatalogOption>>();
        try
        {
            var conn = dbContext.Database.GetDbConnection();
            if (string.IsNullOrWhiteSpace(conn.ConnectionString))
            {
                return map;
            }

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT idContactoEmpresaSubsidiaria, idEmpresaSubsidiaria, nombreContactoEmpresaSubsidiaria, email FROM dbo.TContactoEmpresaSubsidiaria;";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetInt32(0);
                var empresaId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                var name = reader.IsDBNull(2) ? $"Contacto #{id}" : reader.GetString(2);
                var email = reader.IsDBNull(3) ? null : reader.GetString(3);
                var label = string.IsNullOrWhiteSpace(email) ? name : $"{name} ({email})";

                if (!map.TryGetValue(empresaId, out var list))
                {
                    list = [];
                    map[empresaId] = list;
                }
                list.Add(new CatalogOption(id, label));
            }
        }
        catch
        {
            // Resiliente ante variaciones de esquema o testing in-memory
        }

        return map;
    }

    [HttpPost("QuickCreateTechnology")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCreateTechnology([FromForm] string nombreCorporativo, [FromForm] string? nombreLocal, [FromForm] int? familiaId, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        if (string.IsNullOrWhiteSpace(nombreCorporativo))
        {
            return Json(new { succeeded = false, message = "El nombre corporativo de la tecnología es obligatorio." });
        }

        var trimmedNombre = nombreCorporativo.Trim();
        var trimmedLocal = string.IsNullOrWhiteSpace(nombreLocal) ? null : nombreLocal.Trim();
        var validFamiliaId = familiaId.HasValue && familiaId.Value > 0 ? familiaId : null;

        // Validar si ya existe para evitar duplicados
        var existing = await dbContext.Technologies.AsNoTracking()
            .FirstOrDefaultAsync(t => t.NombreCorporativo == trimmedNombre, cancellationToken);

        if (existing is not null)
        {
            var existingFamily = existing.IdFamilia.HasValue
                ? await dbContext.Families.Where(f => f.Id == existing.IdFamilia.Value).Select(f => f.Nombre).FirstOrDefaultAsync(cancellationToken)
                : null;

            return Json(new
            {
                succeeded = true,
                alreadyExisted = true,
                id = existing.Id,
                nombre = existing.NombreCorporativo ?? existing.NombreLocal ?? $"Tecnología #{existing.Id}",
                familia = existingFamily,
                message = $"La tecnología '{trimmedNombre}' ya existía en el catálogo y ha sido seleccionada."
            });
        }

        var newTech = new TTecnologiaTSI
        {
            NombreCorporativo = trimmedNombre,
            NombreLocal = trimmedLocal,
            IdFamilia = validFamiliaId
        };

        dbContext.Technologies.Add(newTech);
        await dbContext.SaveChangesAsync(cancellationToken);

        var familyName = newTech.IdFamilia.HasValue
            ? await dbContext.Families.Where(f => f.Id == newTech.IdFamilia.Value).Select(f => f.Nombre).FirstOrDefaultAsync(cancellationToken)
            : null;

        return Json(new
        {
            succeeded = true,
            alreadyExisted = false,
            id = newTech.Id,
            nombre = newTech.NombreCorporativo,
            familia = familyName,
            message = $"Tecnología '{trimmedNombre}' registrada exitosamente en el catálogo."
        });
    }

    [HttpGet("SearchTechnologies")]
    [Authorize(Policy = Permissions.CatalogView)]
    public async Task<IActionResult> SearchTechnologies([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var query = dbContext.Technologies.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(t => (t.NombreCorporativo != null && t.NombreCorporativo.Contains(term))
                                  || (t.NombreLocal != null && t.NombreLocal.Contains(term)));
        }

        var results = await (
            from t in query
            join f in dbContext.Families on t.IdFamilia equals f.Id into fGroup
            from fam in fGroup.DefaultIfEmpty()
            orderby t.NombreCorporativo
            select new TechnologyCatalogItem(
                t.Id,
                t.NombreCorporativo ?? t.NombreLocal ?? $"Tecnología #{t.Id}",
                fam != null ? fam.Nombre : null)
        ).Take(100).ToListAsync(cancellationToken);

        return Json(results);
    }

    private Guid? ActorId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}