using System.Data;
using System.Security.Claims;

using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Catalogs;
using Landscape.Tsi.Infrastructure.Catalogs;
using Landscape.Tsi.Infrastructure.Reporting;
using Landscape.Tsi.Web.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Web.Controllers;

[Authorize(Policy = Permissions.CatalogView)]
[Route("Administration/AdoptionProcess")]
public sealed class AdoptionProcessController(
    IAdoptionProcessService adoptionService,
    IServiceManagementService serviceManagement,
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

        var services = await serviceManagement.GetServicesByProcessAsync(id, cancellationToken);
        var serviceTypes = await serviceManagement.GetServiceTypesAsync(cancellationToken);
        var supportActivities = await serviceManagement.GetSupportActivitiesAsync(null, cancellationToken);

        return View(new AdoptionProcessDetailViewModel
        {
            Process = detail,
            Technologies = technologies,
            Companies = companies,
            OperationTypes = opTypes,
            WorkModes = workModes,
            AdoptionStates = adoptionStates,
            Services = services,
            ServiceTypes = serviceTypes,
            SupportActivities = supportActivities,
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
    public async Task<IActionResult> ConveneCompany(
        int id,
        int? empresaId,
        List<int>? empresaIds,
        int? contactoFocalId,
        bool aplica,
        string? justificacionNoAplica,
        bool isBatchSync = false,
        CancellationToken cancellationToken = default)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var selectedIds = new List<int>();
        if (empresaIds != null && empresaIds.Count > 0)
        {
            selectedIds.AddRange(empresaIds);
        }
        else if (empresaId.HasValue && empresaId.Value > 0)
        {
            selectedIds.Add(empresaId.Value);
        }

        if (selectedIds.Count == 0 && !isBatchSync)
        {
            TempData["ErrorMessage"] = "Debe seleccionar al menos una empresa subsidiaria para convocar.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var inputs = selectedIds.Select(eid => new ConveneCompanyInput(
            EmpresaId: eid,
            ContactoFocalId: contactoFocalId,
            Aplica: aplica,
            JustificacionNoAplica: justificacionNoAplica?.Trim()));

        var result = await adoptionService.BatchConveneCompaniesAsync(id, inputs, actor.Value, HttpContext.TraceIdentifier, cancellationToken);

        if (result.Succeeded)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/RemoveCompany")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCompany(int id, int procesoEmpresaId, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.RemoveCompanyFromProcessAsync(id, procesoEmpresaId, actor.Value, HttpContext.TraceIdentifier, cancellationToken);

        if (result.Succeeded)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/empresa/{empresaId:int}/assign-focal")]
    [HttpPost("/AdoptionProcess/{id:int}/empresa/{empresaId:int}/assign-focal")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    public async Task<IActionResult> AssignFocal(int id, int empresaId, [FromBody] AssignFocalDto? model, CancellationToken cancellationToken)
    {
        var procEmp = await dbContext.AdoptionProcessCompanies
            .FirstOrDefaultAsync(cp => cp.IdProcesoAdopcionTSI == id && cp.IdEmpresaSubsidiaria == empresaId, cancellationToken);

        if (procEmp is null)
        {
            return NotFound(new { message = "La empresa no se encuentra convocada en este proceso." });
        }

        procEmp.IdContactoEmpresaSubsidiaria = model?.ContactoFocalId > 0 ? model.ContactoFocalId : null;
        procEmp.FechaModificacion = DateTime.UtcNow;
        procEmp.UsuarioModificacion = User.Identity?.Name ?? "SYSTEM";

        await dbContext.SaveChangesAsync(cancellationToken);

        string? contactName = null;
        string? contactEmail = null;
        if (procEmp.IdContactoEmpresaSubsidiaria.HasValue)
        {
            var c = await dbContext.CompanyContacts.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == procEmp.IdContactoEmpresaSubsidiaria.Value, cancellationToken);
            contactName = c?.NombreContactoEmpresaSubsidiaria;
            contactEmail = c?.Email;
        }

        return Ok(new
        {
            success = true,
            focalId = procEmp.IdContactoEmpresaSubsidiaria,
            focalName = contactName ?? "Sin asignar",
            focalEmail = contactEmail,
            message = "Contacto focal actualizado exitosamente."
        });
    }

    [HttpPost("{id:int}/empresa/{empresaId:int}/capability-state")]
    [HttpPost("/AdoptionProcess/{id:int}/empresa/{empresaId:int}/capability-state")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    public async Task<IActionResult> UpdateCompanyCapabilityState(
        int id,
        int empresaId,
        [FromBody] UpdateCapabilityStateDto? model,
        CancellationToken cancellationToken)
    {
        if (model is null || model.CapacidadId <= 0)
        {
            return BadRequest(new { success = false, message = "Datos de capacidad inválidos." });
        }

        var actor = ActorId();
        var actorId = actor ?? Guid.Empty;

        var result = await adoptionService.UpdateCompanyCapabilityStateAsync(
            id,
            empresaId,
            model.CapacidadId,
            model.EstadoCodigo,
            model.Comentario,
            actorId,
            HttpContext.TraceIdentifier,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new
        {
            success = true,
            capacidadId = model.CapacidadId,
            estadoCodigo = model.EstadoCodigo?.Trim().ToUpperInvariant(),
            comentario = model.Comentario,
            message = result.Message
        });
    }

    [HttpPost("{id:int}/empresa/{empresaId:int}/capability-comment")]
    [HttpPost("/AdoptionProcess/{id:int}/empresa/{empresaId:int}/capability-comment")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    public async Task<IActionResult> UpdateCompanyCapabilityComment(
        int id,
        int empresaId,
        [FromBody] UpdateCapabilityCommentDto? model,
        CancellationToken cancellationToken)
    {
        var actor = ActorId();
        var actorId = actor ?? Guid.Empty;

        var result = await adoptionService.UpdateCompanyCapabilityCommentAsync(
            id,
            empresaId,
            model?.Comentario,
            actorId,
            HttpContext.TraceIdentifier,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new
        {
            success = true,
            comentario = model?.Comentario,
            message = result.Message
        });
    }

    [HttpPost("{id:int}/empresa/{empresaId:int}/driver-volume")]
    [HttpPost("/AdoptionProcess/{id:int}/empresa/{empresaId:int}/driver-volume")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    public async Task<IActionResult> UpdateCompanyDriverVolume(
        int id,
        int empresaId,
        [FromBody] UpdateDriverVolumeDto? model,
        CancellationToken cancellationToken)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.DriverKey))
        {
            return BadRequest(new { success = false, message = "Identificador de driver no especificado." });
        }

        var actor = ActorId();
        var actorId = actor ?? Guid.Empty;

        var result = await adoptionService.UpdateCompanyDriverVolumeAsync(
            id,
            empresaId,
            model.DriverKey,
            model.Cantidad,
            actorId,
            HttpContext.TraceIdentifier,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new
        {
            success = true,
            driverKey = model.DriverKey,
            cantidad = model.Cantidad,
            driverId = result.EntityId,
            message = result.Message
        });
    }

    public sealed record UpdateVencimientosDriversDto(IReadOnlyList<string>? Drivers);

    [HttpPost("{id:int}/update-vencimientos-drivers")]
    [HttpPost("/AdoptionProcess/{id:int}/update-vencimientos-drivers")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    public async Task<IActionResult> UpdateVencimientosDrivers(
        int id,
        [FromBody] UpdateVencimientosDriversDto? model,
        CancellationToken cancellationToken)
    {
        var actor = ActorId();
        var actorId = actor ?? Guid.Empty;

        var result = await adoptionService.SaveVencimientoReportDriversAsync(
            id,
            model?.Drivers ?? [],
            actorId,
            HttpContext.TraceIdentifier,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new
        {
            success = true,
            message = result.Message
        });
    }

    [HttpGet("empresa/{empresaId:int}/contacts-lookup")]
    [HttpGet("/AdoptionProcess/empresa/{empresaId:int}/contacts-lookup")]
    public async Task<IActionResult> GetCompanyContactsLookup(int empresaId, CancellationToken cancellationToken)
    {
        var contacts = await dbContext.CompanyContacts.AsNoTracking()
            .Where(c => c.IdEmpresaSubsidiaria == empresaId)
            .OrderBy(c => c.NombreContactoEmpresaSubsidiaria)
            .Select(c => new
            {
                id = c.Id,
                nombre = c.NombreContactoEmpresaSubsidiaria,
                rol = c.Rol,
                email = c.Email,
                telefono = c.Telefono
            })
            .ToListAsync(cancellationToken);

        return Json(contacts);
    }

    [HttpGet("empresa/{empresaId:int}/contacts")]
    [HttpGet("/AdoptionProcess/empresa/{empresaId:int}/contacts")]
    public async Task<IActionResult> GetCompanyContacts(int empresaId, CancellationToken cancellationToken)
    {
        var company = await dbContext.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == empresaId, cancellationToken);
        var companyName = company?.Nombre ?? $"Empresa #{empresaId}";

        var contacts = await dbContext.CompanyContacts.AsNoTracking()
            .Where(c => c.IdEmpresaSubsidiaria == empresaId)
            .OrderBy(c => c.NombreContactoEmpresaSubsidiaria)
            .ToListAsync(cancellationToken);

        var canViewSensitive = User.HasClaim(CustomClaimTypes.Permission, Permissions.SensitiveDataView);

        return Json(new
        {
            entityId = empresaId,
            entityName = companyName,
            entityType = "empresa-subsidiaria",
            totalCount = contacts.Count,
            items = contacts.Select(c => new
            {
                id = c.Id,
                nombre = c.NombreContactoEmpresaSubsidiaria ?? "—",
                rol = c.Rol ?? "—",
                email = canViewSensitive ? (c.Email ?? "—") : SensitiveDataMasker.MaskEmail(c.Email),
                telefono = canViewSensitive ? (c.Telefono ?? "—") : SensitiveDataMasker.MaskPhone(c.Telefono),
                otro = c.Otro ?? "—",
                notas = c.Notas ?? "—"
            })
        });
    }

    [HttpPost("empresa/{empresaId:int}/contacts")]
    [HttpPost("/AdoptionProcess/empresa/{empresaId:int}/contacts")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    public async Task<IActionResult> CreateCompanyContact(int empresaId, [FromBody] SaveMasterContactDto? model, CancellationToken cancellationToken)
    {
        var name = model?.Nombre?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "El nombre del contacto es obligatorio." });
        }

        var contact = new TContactoEmpresaSubsidiaria
        {
            IdEmpresaSubsidiaria = empresaId,
            NombreContactoEmpresaSubsidiaria = name,
            Rol = string.IsNullOrWhiteSpace(model?.Rol) ? null : model.Rol.Trim(),
            Email = string.IsNullOrWhiteSpace(model?.Email) ? null : model.Email.Trim(),
            Telefono = string.IsNullOrWhiteSpace(model?.Telefono) ? null : model.Telefono.Trim(),
            Otro = string.IsNullOrWhiteSpace(model?.Otro) ? null : model.Otro.Trim(),
            Notas = string.IsNullOrWhiteSpace(model?.Notas) ? null : model.Notas.Trim()
        };

        dbContext.CompanyContacts.Add(contact);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            id = contact.Id,
            message = "Contacto registrado exitosamente."
        });
    }

    [HttpPost("empresa/{empresaId:int}/contacts/{contactId:int}/edit")]
    [HttpPost("/AdoptionProcess/empresa/{empresaId:int}/contacts/{contactId:int}/edit")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    public async Task<IActionResult> EditCompanyContact(int empresaId, int contactId, [FromBody] SaveMasterContactDto? model, CancellationToken cancellationToken)
    {
        var contact = await dbContext.CompanyContacts.FirstOrDefaultAsync(c => c.Id == contactId, cancellationToken);
        if (contact is null) return NotFound(new { message = "Contacto no encontrado." });

        var name = model?.Nombre?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "El nombre del contacto es obligatorio." });
        }

        contact.NombreContactoEmpresaSubsidiaria = name;
        contact.Rol = string.IsNullOrWhiteSpace(model?.Rol) ? null : model.Rol.Trim();
        contact.Email = string.IsNullOrWhiteSpace(model?.Email) ? null : model.Email.Trim();
        contact.Telefono = string.IsNullOrWhiteSpace(model?.Telefono) ? null : model.Telefono.Trim();
        contact.Otro = string.IsNullOrWhiteSpace(model?.Otro) ? null : model.Otro.Trim();
        contact.Notas = string.IsNullOrWhiteSpace(model?.Notas) ? null : model.Notas.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            message = "Contacto actualizado exitosamente."
        });
    }

    [HttpPost("empresa/{empresaId:int}/contacts/{contactId:int}/delete")]
    [HttpPost("/AdoptionProcess/empresa/{empresaId:int}/contacts/{contactId:int}/delete")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    public async Task<IActionResult> DeleteCompanyContact(int empresaId, int contactId, CancellationToken cancellationToken)
    {
        var contact = await dbContext.CompanyContacts.FirstOrDefaultAsync(c => c.Id == contactId, cancellationToken);
        if (contact is null) return NotFound(new { message = "Contacto no encontrado." });

        dbContext.CompanyContacts.Remove(contact);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            message = "Contacto eliminado exitosamente."
        });
    }

    [HttpPost("{id:int}/Technology")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterTechnology(int id, int empresaId, int tecnologiaId, int buildingBlockId, int? procesoEmpresaId, bool esPrimaria, string? versionDesplegada, bool esInstanciaCorporativa, CancellationToken cancellationToken)
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
            CorrelationId: HttpContext.TraceIdentifier,
            EsInstanciaCorporativa: esInstanciaCorporativa), cancellationToken);

        if (result.Succeeded)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/DeleteImplementedTechnology")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImplementedTechnology(int id, int tecnologiaImplementadaId, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.DeleteImplementedTechnologyAsync(id, tecnologiaImplementadaId, actor.Value, HttpContext.TraceIdentifier, cancellationToken);

        if (result.Succeeded)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/Contract")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveContract(
        int id,
        int tecnologiaImplementadaId,
        string numeroContrato,
        bool esAdenda,
        bool esPayg,
        int? contratoPadreId,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        DateTime? fechaAdjudicacion,
        string? rutaDocumento,
        decimal? monto,
        string? moneda,
        string? observaciones,
        int? contratoId,
        CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.SaveContractAsync(new SaveContractCommand(
            TecnologiaImplementadaId: tecnologiaImplementadaId,
            NumeroContrato: numeroContrato,
            EsAdenda: esAdenda,
            ContratoPadreId: contratoPadreId,
            FechaInicio: esPayg ? null : (fechaInicio == default ? DateTime.Today : fechaInicio),
            FechaFin: esPayg ? null : (fechaFin == default ? DateTime.Today.AddYears(1) : fechaFin),
            FechaAdjudicacion: esPayg ? null : fechaAdjudicacion,
            RutaDocumento: rutaDocumento?.Trim(),
            Monto: monto,
            Moneda: string.IsNullOrWhiteSpace(moneda) ? "USD" : moneda.Trim(),
            Observaciones: observaciones?.Trim(),
            ActorUserId: actor.Value,
            CorrelationId: HttpContext.TraceIdentifier,
            EsPayg: esPayg,
            ContratoId: contratoId), cancellationToken);

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
    public async Task<IActionResult> SaveDriver(
        int id,
        int tecnologiaImplementadaId,
        string descripcion,
        string? unidadMedida,
        decimal? cantidad,
        decimal? precioUnitario,
        string? moneda,
        int? driverId,
        CancellationToken cancellationToken)
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
            CorrelationId: HttpContext.TraceIdentifier,
            DriverId: driverId), cancellationToken);

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

    [HttpPost("{id:int}/FinalizeEvaluation")]
    [HttpPost("Evaluations/{id:int}/FinalizeEvaluation")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FinalizeEvaluation(
        int id,
        int buildingBlockId,
        int tecnologiaId,
        string rolEstandar,
        DateTime fechaInicioVigencia,
        string? motivoAdjudicacion,
        string? sustentoArquitectura,
        string? numeroContratoCorporativo,
        decimal? montoContratoCorporativo,
        string? monedaContratoCorporativo,
        DateTime? fechaInicioContratoCorporativo,
        DateTime? fechaFinContratoCorporativo,
        DateTime? fechaAdjudicacionContratoCorporativo,
        bool esPaygContratoCorporativo,
        List<string>? driverDescripcion,
        List<string>? driverUnidadMedida,
        List<decimal?>? driverCantidad,
        List<decimal?>? driverPrecioUnitario,
        List<string>? driverMoneda,
        List<int>? subsidiariasAlineadasIds,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var drivers = new List<CorporateDriverItemDto>();
        if (driverDescripcion != null)
        {
            for (int i = 0; i < driverDescripcion.Count; i++)
            {
                var desc = driverDescripcion[i];
                if (!string.IsNullOrWhiteSpace(desc))
                {
                    drivers.Add(new CorporateDriverItemDto(
                        Descripcion: desc.Trim(),
                        UnidadMedida: driverUnidadMedida != null && i < driverUnidadMedida.Count ? driverUnidadMedida[i]?.Trim() : null,
                        Cantidad: driverCantidad != null && i < driverCantidad.Count ? driverCantidad[i] : null,
                        PrecioUnitario: driverPrecioUnitario != null && i < driverPrecioUnitario.Count ? driverPrecioUnitario[i] : null,
                        Moneda: driverMoneda != null && i < driverMoneda.Count ? driverMoneda[i]?.Trim() : "USD"));
                }
            }
        }

        var command = new FinalizeEvaluationWithStandardCommand(
            ProcesoId: id,
            BuildingBlockId: buildingBlockId,
            TecnologiaId: tecnologiaId,
            RolEstandar: string.IsNullOrWhiteSpace(rolEstandar) ? "PRINCIPAL" : rolEstandar.Trim().ToUpperInvariant(),
            FechaInicioVigencia: fechaInicioVigencia == default ? DateTime.Today : fechaInicioVigencia,
            MotivoAdjudicacion: motivoAdjudicacion?.Trim(),
            SustentoArquitectura: sustentoArquitectura?.Trim(),
            NumeroContratoCorporativo: numeroContratoCorporativo?.Trim(),
            MontoContratoCorporativo: montoContratoCorporativo,
            MonedaContratoCorporativo: monedaContratoCorporativo?.Trim(),
            FechaInicioContratoCorporativo: fechaInicioContratoCorporativo,
            FechaFinContratoCorporativo: fechaFinContratoCorporativo,
            FechaAdjudicacionContratoCorporativo: fechaAdjudicacionContratoCorporativo,
            EsPaygContratoCorporativo: esPaygContratoCorporativo,
            DriversCorporativos: drivers,
            SubsidiariasAlineadasIds: subsidiariasAlineadasIds ?? [],
            ActorUserId: actor.Value,
            CorrelationId: HttpContext.TraceIdentifier);

        var result = await adoptionService.FinalizeEvaluationWithStandardAsync(command, cancellationToken);
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
                         || p.Nombre.Contains("DADO DE BAJA", StringComparison.OrdinalIgnoreCase)
                         || p.Nombre.Contains("CANCELAD", StringComparison.OrdinalIgnoreCase)
                         || p.Codigo.StartsWith("BAJA-", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(p.EstadoAdopcionNombre, "CANCELADO", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(p.EstadoAdopcionNombre, "CANCELADA", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(p.EstadoAdopcionNombre, "DADO DE BAJA", StringComparison.OrdinalIgnoreCase);

            var isTerminada = !isBaja && (
                string.Equals(p.EstadoAdopcionNombre, "ESTANDARIZADA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.EstadoAdopcionNombre, "IMPLEMENTADO", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.EstadoAdopcionNombre, "TERMINADA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.EstadoAdopcionNombre, "TERMINADO", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.EstadoAdopcionNombre, "FINALIZADA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.EstadoAdopcionNombre, "FINALIZADO", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.EstadoAdopcionNombre, "CERRADA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.EstadoAdopcionNombre, "CERRADO", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.EstadoAdopcionNombre, "COMPLETADA", StringComparison.OrdinalIgnoreCase)
            );

            var isActivo = !isBaja && !isTerminada;

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
                IsActivo = isActivo,
                IsCancelada = isBaja,
                IsTerminada = isTerminada
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
                true,
                null))
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
            var participatingEmpresaIds = model.Subsidiaries
                .Where(s => s.Selected)
                .Select(s => s.EmpresaId)
                .ToHashSet();

            var processCompanyMap = await dbContext.AdoptionProcessCompanies.AsNoTracking()
                .Where(cp => cp.IdProcesoAdopcionTSI == procesoId)
                .ToDictionaryAsync(cp => cp.IdEmpresaSubsidiaria, cp => cp.IdProcesoAdopcionEmpresa, cancellationToken);

            foreach (var asIs in model.SubsidiaryAsIsList.Where(a => participatingEmpresaIds.Contains(a.EmpresaId) && a.TieneTecnologia && a.TecnologiaId.HasValue && a.TecnologiaId.Value > 0))
            {
                var cpId = processCompanyMap.GetValueOrDefault(asIs.EmpresaId);

                // Resolver nombres de vendor o partner si no vienen en el request pero se eligió un ID
                if (string.IsNullOrWhiteSpace(asIs.VendorNombre) && asIs.VendorId.HasValue && asIs.VendorId.Value > 0)
                {
                    asIs.VendorNombre = await dbContext.Vendors.AsNoTracking()
                        .Where(v => v.Id == asIs.VendorId.Value)
                        .Select(v => v.NombreVendor)
                        .FirstOrDefaultAsync(cancellationToken);
                }
                // Múltiples partners o partner directo
                var partnerDescs = new List<string>();
                if (asIs.Partners != null && asIs.Partners.Count > 0)
                {
                    foreach (var pt in asIs.Partners)
                    {
                        var pName = pt.PartnerNombre;
                        if (string.IsNullOrWhiteSpace(pName) && pt.PartnerId.HasValue && pt.PartnerId.Value > 0)
                        {
                            pName = await dbContext.Partners.AsNoTracking()
                                .Where(p => p.Id == pt.PartnerId.Value)
                                .Select(p => p.NombrePartner)
                                .FirstOrDefaultAsync(cancellationToken);
                        }
                        var pContact = pt.PartnerContacto;
                        if (string.IsNullOrWhiteSpace(pContact) && pt.PartnerContactoId.HasValue && pt.PartnerContactoId.Value > 0)
                        {
                            pContact = await dbContext.PartnerContacts.AsNoTracking()
                                .Where(c => c.Id == pt.PartnerContactoId.Value)
                                .Select(c => c.NombreContactoPartner)
                                .FirstOrDefaultAsync(cancellationToken);
                        }
                        if (!string.IsNullOrWhiteSpace(pName))
                        {
                            var entry = pName;
                            if (!string.IsNullOrWhiteSpace(pContact)) entry += $" (Contacto: {pContact})";
                            partnerDescs.Add(entry);
                        }
                    }

                    if (asIs.Partners.Count > 0 && asIs.Partners[0].PartnerId.HasValue)
                    {
                        asIs.PartnerId = asIs.Partners[0].PartnerId;
                        asIs.PartnerNombre = asIs.Partners[0].PartnerNombre;
                        asIs.PartnerContactoId = asIs.Partners[0].PartnerContactoId;
                        asIs.PartnerContacto = asIs.Partners[0].PartnerContacto;
                    }
                }

                if (partnerDescs.Count == 0)
                {
                    if (string.IsNullOrWhiteSpace(asIs.PartnerNombre) && asIs.PartnerId.HasValue && asIs.PartnerId.Value > 0)
                    {
                        asIs.PartnerNombre = await dbContext.Partners.AsNoTracking()
                            .Where(p => p.Id == asIs.PartnerId.Value)
                            .Select(p => p.NombrePartner)
                            .FirstOrDefaultAsync(cancellationToken);
                    }
                    if (string.IsNullOrWhiteSpace(asIs.PartnerContacto) && asIs.PartnerContactoId.HasValue && asIs.PartnerContactoId.Value > 0)
                    {
                        asIs.PartnerContacto = await dbContext.PartnerContacts.AsNoTracking()
                            .Where(c => c.Id == asIs.PartnerContactoId.Value)
                            .Select(c => c.NombreContactoPartner)
                            .FirstOrDefaultAsync(cancellationToken);
                    }
                    var pDesc = asIs.PartnerNombre ?? "Directo";
                    if (!string.IsNullOrWhiteSpace(asIs.PartnerContacto)) pDesc += $" (Contacto: {asIs.PartnerContacto})";
                    partnerDescs.Add(pDesc);
                }

                var partnerSummary = string.Join(", ", partnerDescs);

                if (string.IsNullOrWhiteSpace(asIs.VendorContacto) && asIs.VendorContactoId.HasValue && asIs.VendorContactoId.Value > 0)
                {
                    asIs.VendorContacto = await dbContext.VendorContacts.AsNoTracking()
                        .Where(c => c.Id == asIs.VendorContactoId.Value)
                        .Select(c => c.NombreContactoVendor)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                var regCmd = new RegisterImplementedTechnologyCommand(
                    EmpresaId: asIs.EmpresaId,
                    TecnologiaId: asIs.TecnologiaId!.Value,
                    BuildingBlockId: model.BuildingBlockId,
                    ProcesoEmpresaId: cpId > 0 ? cpId : null,
                    EsPrimaria: true,
                    VersionDesplegada: asIs.VersionDesplegada,
                    ActorUserId: actor.Value,
                    CorrelationId: HttpContext.TraceIdentifier,
                    EsInstanciaCorporativa: asIs.EsInstanciaCorporativa);

                var regResult = await adoptionService.RegisterImplementedTechnologyAsync(regCmd, cancellationToken);
                if (regResult.Succeeded && regResult.EntityId.HasValue)
                {
                    var implId = regResult.EntityId.Value;

                    if (!string.IsNullOrWhiteSpace(asIs.NumeroContrato) || asIs.MontoContratado.HasValue || asIs.MontoAnual.HasValue || asIs.MontoTrianual.HasValue)
                    {
                        var effectiveMonto = asIs.MontoTrianual ?? asIs.MontoAnual ?? asIs.MontoContratado;
                        var numContrato = !string.IsNullOrWhiteSpace(asIs.NumeroContrato) ? asIs.NumeroContrato : $"CT-{asIs.EmpresaId}-{procesoId}";

                        var vendorDesc = asIs.VendorNombre ?? "-";
                        if (!string.IsNullOrWhiteSpace(asIs.VendorContacto)) vendorDesc += $" (Contacto: {asIs.VendorContacto})";

                        await adoptionService.SaveContractAsync(new SaveContractCommand(
                            TecnologiaImplementadaId: implId,
                            NumeroContrato: numContrato,
                            EsAdenda: false,
                            ContratoPadreId: null,
                            FechaInicio: asIs.EsPayg ? null : (asIs.FechaInicioContrato ?? (model.FechaInicio != default ? model.FechaInicio : DateTime.Today)),
                            FechaFin: asIs.EsPayg ? null : (asIs.FechaFinContrato ?? (model.FechaInicio != default ? model.FechaInicio : DateTime.Today)),
                            FechaAdjudicacion: null,
                            RutaDocumento: null,
                            Monto: effectiveMonto,
                            Moneda: string.IsNullOrWhiteSpace(asIs.MonedaContrato) ? "USD" : asIs.MonedaContrato,
                            Observaciones: $"Vendor: {vendorDesc} / Partner(s): {partnerSummary}",
                            ActorUserId: actor.Value,
                            CorrelationId: HttpContext.TraceIdentifier,
                            EsPayg: asIs.EsPayg,
                            MontoAnual: asIs.MontoAnual,
                            MontoTrianual: asIs.MontoTrianual), cancellationToken);
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

                    if (asIs.TipoOperacionId.HasValue || asIs.ModalidadLaboralId.HasValue)
                    {
                        await adoptionService.SaveOperationModelAsync(new SaveOperationModelCommand(
                            TecnologiaImplementadaId: implId,
                            TipoOperacionId: asIs.TipoOperacionId,
                            ModalidadLaboralId: asIs.ModalidadLaboralId,
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

    [HttpPost("Evaluations/{id:int}/Delete")]
    [HttpPost("/AdoptionProcess/Evaluations/{id:int}/Delete")]
    [Authorize(Policy = Permissions.CatalogDelete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvaluation(int id, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.DeleteProcessCascadeAsync(id, actor.Value, HttpContext.TraceIdentifier, cancellationToken);
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

        model.Vendors = await dbContext.Vendors.AsNoTracking()
            .Where(v => !string.IsNullOrWhiteSpace(v.NombreVendor))
            .OrderBy(v => v.NombreVendor)
            .Select(v => new CatalogOption(v.Id, v.NombreVendor!))
            .Distinct()
            .ToListAsync(cancellationToken);

        model.Partners = await dbContext.Partners.AsNoTracking()
            .Where(p => !string.IsNullOrWhiteSpace(p.NombrePartner))
            .OrderBy(p => p.NombrePartner)
            .Select(p => new CatalogOption(p.Id, p.NombrePartner!))
            .Distinct()
            .ToListAsync(cancellationToken);

        model.VersionesDesplegadas = await dbContext.DeploymentVersions.AsNoTracking()
            .Where(v => v.EsActivo)
            .OrderBy(v => v.Orden)
            .ThenBy(v => v.Nombre)
            .Select(v => new CatalogOption(v.Id, v.Nombre ?? $"Versión #{v.Id}"))
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

            var defaultDate = model.FechaInicio != default ? model.FechaInicio : DateTime.Today;
            model.SubsidiaryAsIsList = companies.Select(c => new SubsidiaryAsIsInputModel
            {
                EmpresaId = c.Id,
                EmpresaNombre = c.Nombre ?? $"Empresa #{c.Id}",
                TieneTecnologia = true,
                MonedaContrato = "USD",
                FechaInicioContrato = defaultDate,
                FechaFinContrato = defaultDate
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
    [HttpPost("/AdoptionProcess/QuickCreateTechnology")]
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

    [HttpPost("QuickCreateVendor")]
    [HttpPost("/AdoptionProcess/QuickCreateVendor")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCreateVendor([FromForm] string nombreVendor, [FromForm] int? tecnologiaId, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        if (string.IsNullOrWhiteSpace(nombreVendor))
        {
            return Json(new { succeeded = false, message = "El nombre del Vendor es obligatorio." });
        }

        var trimmed = nombreVendor.Trim();
        var existing = await dbContext.Vendors.AsNoTracking()
            .FirstOrDefaultAsync(v => v.NombreVendor == trimmed, cancellationToken);

        if (existing is not null)
        {
            return Json(new
            {
                succeeded = true,
                alreadyExisted = true,
                id = existing.Id,
                nombre = existing.NombreVendor,
                message = $"El Vendor '{trimmed}' ya existía en el catálogo y ha sido seleccionado."
            });
        }

        var newVendor = new TVendor
        {
            NombreVendor = trimmed,
            IdTecnologiaTSI = (tecnologiaId.HasValue && tecnologiaId.Value > 0) ? tecnologiaId.Value : null
        };

        dbContext.Vendors.Add(newVendor);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Json(new
        {
            succeeded = true,
            alreadyExisted = false,
            id = newVendor.Id,
            nombre = newVendor.NombreVendor,
            message = $"Vendor '{trimmed}' registrado exitosamente."
        });
    }

    [HttpPost("QuickCreatePartner")]
    [HttpPost("/AdoptionProcess/QuickCreatePartner")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCreatePartner([FromForm] string nombrePartner, [FromForm] int? vendorId, [FromForm] string? descripcion, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        if (string.IsNullOrWhiteSpace(nombrePartner))
        {
            return Json(new { succeeded = false, message = "El nombre del Partner es obligatorio." });
        }

        var trimmed = nombrePartner.Trim();
        var existing = await dbContext.Partners.AsNoTracking()
            .FirstOrDefaultAsync(p => p.NombrePartner == trimmed, cancellationToken);

        if (existing is not null)
        {
            return Json(new
            {
                succeeded = true,
                alreadyExisted = true,
                id = existing.Id,
                nombre = existing.NombrePartner,
                message = $"El Partner '{trimmed}' ya existía en el catálogo y ha sido seleccionado."
            });
        }

        var newPartner = new TPartner
        {
            NombrePartner = trimmed,
            DescripcionPartner = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
            IdVendor = (vendorId.HasValue && vendorId.Value > 0) ? vendorId.Value : null
        };

        dbContext.Partners.Add(newPartner);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Json(new
        {
            succeeded = true,
            alreadyExisted = false,
            id = newPartner.Id,
            nombre = newPartner.NombrePartner,
            message = $"Partner '{trimmed}' registrado exitosamente."
        });
    }

    [HttpPost("QuickCreateVendorContact")]
    [HttpPost("/AdoptionProcess/QuickCreateVendorContact")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCreateVendorContact(
        [FromForm] int vendorId,
        [FromForm] string nombreContacto,
        [FromForm] string? rol,
        [FromForm] string? email,
        [FromForm] string? telefono,
        [FromForm] string? notas,
        CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        if (vendorId <= 0)
        {
            return Json(new { succeeded = false, message = "Debe seleccionar un Vendor válido." });
        }

        if (string.IsNullOrWhiteSpace(nombreContacto))
        {
            return Json(new { succeeded = false, message = "El nombre del contacto de vendor es obligatorio." });
        }

        var trimmedNombre = nombreContacto.Trim();
        var trimmedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        var trimmedTelefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
        var trimmedRol = string.IsNullOrWhiteSpace(rol) ? null : rol.Trim();
        var trimmedNotas = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim();

        var existing = await dbContext.VendorContacts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.IdVendor == vendorId && c.NombreContactoVendor == trimmedNombre, cancellationToken);

        if (existing is not null)
        {
            return Json(new
            {
                succeeded = true,
                alreadyExisted = true,
                id = existing.Id,
                nombre = existing.NombreContactoVendor,
                email = existing.Email,
                telefono = existing.Telefono,
                rol = existing.Rol,
                message = $"El contacto '{trimmedNombre}' ya existía y ha sido seleccionado."
            });
        }

        var newContact = new TContactoVendor
        {
            IdVendor = vendorId,
            NombreContactoVendor = trimmedNombre,
            Rol = trimmedRol,
            Email = trimmedEmail,
            Telefono = trimmedTelefono,
            Notas = trimmedNotas
        };

        dbContext.VendorContacts.Add(newContact);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Json(new
        {
            succeeded = true,
            alreadyExisted = false,
            id = newContact.Id,
            nombre = newContact.NombreContactoVendor,
            email = newContact.Email,
            telefono = newContact.Telefono,
            rol = newContact.Rol,
            message = $"Contacto '{trimmedNombre}' registrado exitosamente."
        });
    }

    [HttpPost("QuickCreatePartnerContact")]
    [HttpPost("/AdoptionProcess/QuickCreatePartnerContact")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCreatePartnerContact(
        [FromForm] int partnerId,
        [FromForm] int? vendorId,
        [FromForm] string nombreContacto,
        [FromForm] string? rol,
        [FromForm] string? email,
        [FromForm] string? telefono,
        [FromForm] string? notas,
        CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        if (partnerId <= 0)
        {
            return Json(new { succeeded = false, message = "Debe seleccionar un Partner válido." });
        }

        if (string.IsNullOrWhiteSpace(nombreContacto))
        {
            return Json(new { succeeded = false, message = "El nombre del contacto de partner es obligatorio." });
        }

        var trimmedNombre = nombreContacto.Trim();
        var trimmedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        var trimmedTelefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
        var trimmedRol = string.IsNullOrWhiteSpace(rol) ? null : rol.Trim();
        var trimmedNotas = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim();

        var existing = await dbContext.PartnerContacts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.IdPartner == partnerId && c.NombreContactoPartner == trimmedNombre, cancellationToken);

        if (existing is not null)
        {
            return Json(new
            {
                succeeded = true,
                alreadyExisted = true,
                id = existing.Id,
                nombre = existing.NombreContactoPartner,
                email = existing.Email,
                telefono = existing.Telefono,
                rol = existing.Rol,
                message = $"El contacto '{trimmedNombre}' ya existía y ha sido seleccionado."
            });
        }

        var newContact = new TContactoPartner
        {
            IdPartner = partnerId,
            IdVendor = (vendorId.HasValue && vendorId.Value > 0) ? vendorId.Value : null,
            NombreContactoPartner = trimmedNombre,
            Rol = trimmedRol,
            Email = trimmedEmail,
            Telefono = trimmedTelefono,
            Notas = trimmedNotas
        };

        dbContext.PartnerContacts.Add(newContact);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Json(new
        {
            succeeded = true,
            alreadyExisted = false,
            id = newContact.Id,
            nombre = newContact.NombreContactoPartner,
            email = newContact.Email,
            telefono = newContact.Telefono,
            rol = newContact.Rol,
            message = $"Contacto '{trimmedNombre}' registrado exitosamente."
        });
    }

    [HttpGet("GetVendors")]
    [HttpGet("/AdoptionProcess/GetVendors")]
    public async Task<IActionResult> GetVendors(int? tecnologiaId, CancellationToken cancellationToken)
    {
        var query = dbContext.Vendors.AsNoTracking();
        if (tecnologiaId.HasValue && tecnologiaId.Value > 0)
        {
            query = query.Where(v => v.IdTecnologiaTSI == tecnologiaId.Value || v.IdTecnologiaTSI == null);
        }

        var list = await query
            .Where(v => !string.IsNullOrWhiteSpace(v.NombreVendor))
            .OrderBy(v => v.NombreVendor)
            .Select(v => new { id = v.Id, nombre = v.NombreVendor })
            .Distinct()
            .ToListAsync(cancellationToken);

        return Json(list);
    }

    [HttpGet("GetVendorContacts")]
    [HttpGet("/AdoptionProcess/GetVendorContacts")]
    public async Task<IActionResult> GetVendorContacts(int vendorId, CancellationToken cancellationToken)
    {
        if (vendorId <= 0) return Json(Array.Empty<object>());
        var list = await dbContext.VendorContacts.AsNoTracking()
            .Where(c => c.IdVendor == vendorId && !string.IsNullOrWhiteSpace(c.NombreContactoVendor))
            .OrderBy(c => c.NombreContactoVendor)
            .Select(c => new
            {
                id = c.Id,
                nombre = c.NombreContactoVendor,
                email = c.Email,
                telefono = c.Telefono,
                rol = c.Rol
            })
            .ToListAsync(cancellationToken);

        return Json(list);
    }

    [HttpGet("GetPartners")]
    [HttpGet("/AdoptionProcess/GetPartners")]
    public async Task<IActionResult> GetPartners(int? vendorId, CancellationToken cancellationToken)
    {
        var query = dbContext.Partners.AsNoTracking();
        if (vendorId.HasValue && vendorId.Value > 0)
        {
            query = query.Where(p => p.IdVendor == vendorId.Value || p.IdVendor == null);
        }

        var list = await query
            .Where(p => !string.IsNullOrWhiteSpace(p.NombrePartner))
            .OrderBy(p => p.NombrePartner)
            .Select(p => new { id = p.Id, nombre = p.NombrePartner })
            .Distinct()
            .ToListAsync(cancellationToken);

        return Json(list);
    }

    [HttpGet("GetPartnerContacts")]
    [HttpGet("/AdoptionProcess/GetPartnerContacts")]
    public async Task<IActionResult> GetPartnerContacts(int partnerId, CancellationToken cancellationToken)
    {
        if (partnerId <= 0) return Json(Array.Empty<object>());
        var list = await dbContext.PartnerContacts.AsNoTracking()
            .Where(c => c.IdPartner == partnerId && !string.IsNullOrWhiteSpace(c.NombreContactoPartner))
            .OrderBy(c => c.NombreContactoPartner)
            .Select(c => new
            {
                id = c.Id,
                nombre = c.NombreContactoPartner,
                email = c.Email,
                telefono = c.Telefono,
                rol = c.Rol
            })
            .ToListAsync(cancellationToken);

        return Json(list);
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

    [HttpPost("SaveService")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveService(
        int procesoId,
        int? servicioId,
        string codigoServicio,
        string nombreServicio,
        string? descripcion,
        int tipoServicioId,
        int? tecnologiaTsiId,
        int? tecnologiaImplementadaId,
        int? empresaId,
        int? vendorId,
        string? nombreProveedorServicio,
        string? estadoServicio,
        string? moneda,
        CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var command = new SaveServiceHeaderCommand(
            servicioId,
            codigoServicio,
            nombreServicio,
            descripcion,
            tipoServicioId,
            tecnologiaTsiId,
            tecnologiaImplementadaId,
            empresaId,
            procesoId,
            vendorId,
            nombreProveedorServicio,
            estadoServicio ?? "EVALUACION",
            moneda ?? "USD",
            actor.Value,
            HttpContext.TraceIdentifier);

        var result = await serviceManagement.SaveServiceHeaderAsync(command, cancellationToken);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id = procesoId });
    }

    [HttpPost("DeleteService")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteService(int procesoId, int servicioId, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await serviceManagement.DeleteServiceAsync(servicioId, actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id = procesoId });
    }

    [HttpPost("SaveProjectRateCard")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProjectRateCard(
        int procesoId,
        int? rateCardId,
        int servicioId,
        string complejidad,
        int rangoHorasDesde,
        int? rangoHorasHasta,
        decimal tarifaHora,
        decimal horasEstimadas,
        string? moneda,
        string? observaciones,
        CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var command = new SaveProjectRateCardCommand(
            rateCardId,
            servicioId,
            complejidad,
            rangoHorasDesde,
            rangoHorasHasta,
            tarifaHora,
            horasEstimadas,
            moneda ?? "USD",
            observaciones,
            actor.Value,
            HttpContext.TraceIdentifier);

        var result = await serviceManagement.SaveProjectRateCardAsync(command, cancellationToken);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id = procesoId });
    }

    [HttpPost("DeleteProjectRateCard")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProjectRateCard(int procesoId, int rateCardId, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await serviceManagement.DeleteProjectRateCardAsync(rateCardId, actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id = procesoId });
    }

    [HttpPost("SaveOperationRateCard")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOperationRateCard(
        int procesoId,
        int? rateCardId,
        int servicioId,
        string nivelSoporte,
        string modalidad,
        string? detalleModalidad,
        int horasBaseMensual,
        string expertise,
        string locacion,
        decimal? tarifaHora,
        decimal? tarifaMensual,
        int? cantidadMeses,
        decimal? horasEstimadas,
        string? moneda,
        CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var command = new SaveOperationRateCardCommand(
            rateCardId,
            servicioId,
            nivelSoporte,
            modalidad,
            detalleModalidad,
            horasBaseMensual,
            expertise,
            locacion,
            tarifaHora,
            tarifaMensual,
            cantidadMeses ?? 1,
            horasEstimadas,
            moneda ?? "USD",
            actor.Value,
            HttpContext.TraceIdentifier);

        var result = await serviceManagement.SaveOperationRateCardAsync(command, cancellationToken);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id = procesoId });
    }

    [HttpPost("DeleteOperationRateCard")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOperationRateCard(int procesoId, int rateCardId, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await serviceManagement.DeleteOperationRateCardAsync(rateCardId, actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id = procesoId });
    }

    [HttpGet("SupportActivities")]
    [Authorize(Policy = Permissions.CatalogView)]
    public async Task<IActionResult> GetSupportActivities([FromQuery] string? nivel, CancellationToken cancellationToken)
    {
        var activities = await serviceManagement.GetSupportActivitiesAsync(nivel, cancellationToken);
        return Json(activities);
    }

    [HttpGet("{id:int}/Reports")]
    [HttpGet("Evaluations/{id:int}/Reports")]
    [Authorize(Policy = Permissions.CatalogView)]
    public async Task<IActionResult> Reports(int id, CancellationToken cancellationToken)
    {
        var reportData = await adoptionService.GetEvaluationReportsAsync(id, cancellationToken);
        if (reportData is null)
        {
            return NotFound();
        }

        return View(reportData);
    }

    [HttpGet("{id:int}/export-excel")]
    [HttpGet("Evaluations/{id:int}/export-excel")]
    [Authorize(Policy = Permissions.AdoptionExport)]
    public async Task<IActionResult> ExportEvaluationExcel(int id, [FromQuery] string? section, CancellationToken cancellationToken)
    {
        var report = await adoptionService.GetEvaluationReportsAsync(id, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        var workbook = new SimpleExcelWorkbook();
        var sec = (section ?? "all").ToLowerInvariant();

        // 1. Diagnóstico AS-IS y Contratos
        if (sec is "all" or "as-is" or "alcance")
        {
            var sheet = workbook.CreateSheet("Diagnostico_AS_IS");
            sheet.AddHeader("Empresa", "País", "Fecha Vencimiento", report.EsWaap ? "WAF AS-IS" : "Tecnología AS-IS", "Tipo Contrato", "Fabricante / Vendor", "Partner / Integrador", "Operación");
            foreach (var r in report.AlcanceReport)
            {
                sheet.AddRow(
                    r.EmpresaNombre,
                    r.Pais ?? "—",
                    r.FechaVencimientoContrato.HasValue ? r.FechaVencimientoContrato.Value.ToString("yyyy-MM-dd") : "PAYG / Sin Vencimiento",
                    r.TecnologiaAsIs ?? "—",
                    r.TipoContrato ?? "—",
                    r.Vendor ?? "—",
                    r.Partner ?? "—",
                    r.TipoOperacion ?? "—"
                );
            }
        }

        // 2. Vencimiento de Contratos & Proyección
        if (sec is "all" or "vencimientos" or "timeline")
        {
            var sheet = workbook.CreateSheet("Vencimiento_Contratos");
            var headers = report.EsWaap
                ? new List<string?> { "#", "Empresa", "Producto AS-IS", "Throughput (GB/m)", "Apps FQDN", "WAF Req (M/m)", "Fecha Vencimiento" }
                : new List<string?> { "#", "Empresa", "Tecnología AS-IS", "Tipo Contrato", "Operación", "Drivers Registrados", "Fecha Vencimiento" };

            foreach (var m in report.VencimientoContratosReport.PeriodosHito)
            {
                headers.Add(m.PeriodoLabel);
            }
            sheet.AddHeader(headers);

            foreach (var r in report.VencimientoContratosReport.Filas)
            {
                var rowCells = report.EsWaap
                    ? new List<object?>
                    {
                        r.Secuencia,
                        r.EmpresaNombre,
                        r.ProductoActual ?? "—",
                        r.ThroughputGbMes,
                        r.CantidadAppFqdn,
                        r.RequestWafMillonesMes,
                        r.FechaVencimiento.HasValue ? r.FechaVencimiento.Value.ToString("yyyy-MM-dd") : (r.VencimientoLabel ?? "—")
                    }
                    : new List<object?>
                    {
                        r.Secuencia,
                        r.EmpresaNombre,
                        r.ProductoActual ?? "—",
                        r.TipoContrato ?? "—",
                        r.TipoOperacion ?? "—",
                        r.CantidadDrivers > 0 ? $"{r.CantidadDrivers} driver(s)" : "—",
                        r.FechaVencimiento.HasValue ? r.FechaVencimiento.Value.ToString("yyyy-MM-dd") : (r.VencimientoLabel ?? "—")
                    };

                foreach (var m in report.VencimientoContratosReport.PeriodosHito)
                {
                    var isExpired = r.HitoExpiracionPorPeriodo.TryGetValue(m.PeriodoLabel, out var exp) && exp;
                    rowCells.Add(isExpired ? "X" : string.Empty);
                }
                sheet.AddRow(rowCells);
            }

            // Fila de Totales
            var totalCells = report.EsWaap
                ? new List<object?>
                {
                    string.Empty,
                    "TOTAL CONSOLIDADO",
                    string.Empty,
                    report.VencimientoContratosReport.TotalThroughputGbMes,
                    report.VencimientoContratosReport.TotalAppsFqdn,
                    report.VencimientoContratosReport.TotalRequestWafMillonesMes,
                    string.Empty
                }
                : new List<object?>
                {
                    string.Empty,
                    "TOTAL CONSOLIDADO",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    $"{report.VolumetriaReport.TotalCantidadDrivers} driver(s)",
                    string.Empty
                };

            foreach (var m in report.VencimientoContratosReport.PeriodosHito)
            {
                totalCells.Add(string.Empty);
            }
            sheet.AddRow(totalCells);
        }

        // 3. Volumetría por Empresa / Drivers
        if (sec is "all" or "volumetria" or "volumen" or "drivers")
        {
            var sheet = workbook.CreateSheet("Volumetria_Empresas");

            if (report.EsWaap)
            {
                sheet.AddHeader(
                    "Empresa", "Tecnología AS-IS", "Throughput (Gbps)", "Ancho Banda (Gbps)",
                    "Dominio", "Sub Dominios", "Apps FQDN", "Apps FQDN (API Sec)", "API Protection (M req/m)",
                    "Antibot (M req/m)", "WAF (M req/m)", "Total Antibot+WAF (M req/m)", "Data Transfer (TB/m)",
                    "Request Size (KB/GB)", "Response Size (KB/GB/TB)"
                );

                foreach (var r in report.VolumetriaReport.Filas)
                {
                    sheet.AddRow(
                        r.EmpresaNombre,
                        r.TecnologiaAsIs ?? "—",
                        r.ThroughputMensualGbps,
                        r.AnchoBandaMensualGbps,
                        r.Dominio,
                        r.SubDominios,
                        r.CantidadAppsFqdn,
                        r.CantidadAppsFqdnApiSecurity,
                        r.ApiProtectionRequestMillonesMes,
                        r.MillonesRequestAntibot,
                        r.MillonesRequestWaf,
                        r.MillonesRequestAntibotWaf,
                        r.DataTransferTbMensual,
                        r.RequestSize ?? "—",
                        r.ResponseSize ?? "—"
                    );
                }

                // Fila de Totales
                sheet.AddRow(
                    "TOTAL GENERAL",
                    string.Empty,
                    report.VolumetriaReport.TotalThroughputMensualGbps,
                    report.VolumetriaReport.TotalAnchoBandaMensualGbps,
                    report.VolumetriaReport.TotalDominio,
                    report.VolumetriaReport.TotalSubDominios,
                    report.VolumetriaReport.TotalAppsFqdn,
                    report.VolumetriaReport.TotalAppsFqdnApiSecurity,
                    report.VolumetriaReport.TotalApiProtectionRequestMillonesMes,
                    report.VolumetriaReport.TotalMillonesRequestAntibot,
                    report.VolumetriaReport.TotalMillonesRequestWaf,
                    report.VolumetriaReport.TotalMillonesRequestAntibotWaf,
                    report.VolumetriaReport.TotalDataTransferTbMensual,
                    string.Empty,
                    string.Empty
                );
            }
            {
                var matriz = report.VolumetriaReport.MatrizDrivers;
                if (matriz != null && matriz.ColumnasDrivers.Count > 0)
                {
                    var volHeaders = new List<string?> { "Empresa", "Tecnología AS-IS" };
                    volHeaders.AddRange(matriz.ColumnasDrivers);
                    sheet.AddHeader(volHeaders);

                    foreach (var fila in matriz.Filas)
                    {
                        var rowVals = new List<object?>
                        {
                            fila.EmpresaNombre,
                            fila.TecnologiaAsIs ?? "—"
                        };
                        foreach (var col in matriz.ColumnasDrivers)
                        {
                            var cant = fila.ValoresPorColumnaDriver.TryGetValue(col, out var val) ? val : 0m;
                            rowVals.Add(cant);
                        }
                        sheet.AddRow(rowVals);
                    }

                    // Fila de Totales Consolidados por Columna
                    var totalRowVals = new List<object?>
                    {
                        "TOTALES CONSOLIDADOS",
                        string.Empty
                    };
                    foreach (var col in matriz.ColumnasDrivers)
                    {
                        var tot = matriz.TotalesPorColumna.TryGetValue(col, out var totVal) ? totVal : 0m;
                        totalRowVals.Add(tot);
                    }
                    sheet.AddRow(totalRowVals);
                }
                else
                {
                    sheet.AddHeader("Empresa", "Tecnología AS-IS", "Driver de Consumo", "Cantidad Operativa");
                    foreach (var r in report.AlcanceReport)
                    {
                        sheet.AddRow(r.EmpresaNombre, r.TecnologiaAsIs ?? "—", "Sin drivers registrados", 0m);
                    }
                }
            }
        }

        // 5. Proyección de Costos AS-IS (Valorizado)
        if (sec is "all" or "costos" or "costos-as-is" or "costo")
        {
            var sheet = workbook.CreateSheet("Costos_AS_IS");
            sheet.AddHeader(
                "Empresa", "Tecnología AS-IS", "Descripción del Driver", "Unidad de Medida",
                "Cantidad", "Precio Unitario", "Moneda", "Costo Total Proyectado"
            );

            if (report.CostosAsIsReport != null && report.CostosAsIsReport.Filas.Count > 0)
            {
                foreach (var drv in report.CostosAsIsReport.Filas)
                {
                    sheet.AddRow(
                        drv.EmpresaNombre,
                        drv.TecnologiaAsIs ?? "—",
                        drv.DescripcionDriver,
                        drv.UnidadMedida,
                        drv.DriverId > 0 ? drv.Cantidad : 0m,
                        drv.DriverId > 0 ? drv.PrecioUnitario : 0m,
                        drv.Moneda,
                        drv.DriverId > 0 ? drv.CostoTotal : 0m
                    );
                }

                // Fila de Totales
                sheet.AddRow(
                    "TOTAL GENERAL",
                    string.Empty,
                    $"{report.CostosAsIsReport.TotalItemsConCosto} driver(s)",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    "USD",
                    report.CostosAsIsReport.TotalInversionGeneral
                );
            }
        }

        // 4. Matriz de Cobertura de Capacidades
        if (sec is "all" or "capacidades" or "matrix")
        {
            var sheet = workbook.CreateSheet("Capacidades_Seguridad");
            var headers = new List<string?> { "Empresa", "Tecnología AS-IS" };
            headers.AddRange(report.CapacidadesMatrixReport.ColumnasCapacidades);
            headers.Add("Comentario / Situación");
            sheet.AddHeader(headers);

            foreach (var r in report.CapacidadesMatrixReport.Filas)
            {
                var rowCells = new List<object?>
                {
                    r.EmpresaNombre,
                    r.TecnologiaAsIs ?? "—"
                };

                foreach (var capCol in report.CapacidadesMatrixReport.ColumnasCapacidades)
                {
                    var estado = r.Capacidades.TryGetValue(capCol, out var cell) ? cell.EstadoCodigo : "NA";
                    rowCells.Add(estado);
                }

                rowCells.Add(r.ComentarioSubsidiaria ?? "—");
                sheet.AddRow(rowCells);
            }
        }

        var fileBytes = workbook.Build();
        var fileNamePrefix = sec == "all" ? "Evaluacion_Consolidada" : $"Evaluacion_{sec}";
        var fileName = $"{fileNamePrefix}_{report.CodigoProceso}_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx";
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private Guid? ActorId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}