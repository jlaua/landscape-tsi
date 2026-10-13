using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Web.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Web.Controllers;

public sealed partial class AdoptionProcessController
{
    [HttpGet("Implementations")]
    [HttpGet("/AdoptionProcess/Implementations")]
    public async Task<IActionResult> Implementations(string? search, int? familiaId, int? estadoId, CancellationToken cancellationToken)
    {
        var rawProcesses = await adoptionService.ListImplementationsAsync(cancellationToken);

        var familias = await dbContext.Families.AsNoTracking()
            .OrderBy(f => f.Nombre)
            .Select(f => new CatalogOption(f.Id, f.Nombre ?? $"Familia #{f.Id}"))
            .ToListAsync(cancellationToken);

        var estados = await dbContext.TechnologyAdoptionStates.AsNoTracking()
            .OrderBy(s => s.Nombre)
            .Select(s => new CatalogOption(s.Id, s.Nombre ?? $"Estado #{s.Id}"))
            .ToListAsync(cancellationToken);

        var items = rawProcesses.Select(p => new ImplementationProcessSummaryViewModel
        {
            Id = p.Id,
            Codigo = p.Codigo,
            Nombre = p.Nombre,
            FamiliaId = p.FamiliaId,
            FamiliaNombre = p.FamiliaNombre,
            TecnologiaEstandarId = p.TecnologiaEstandarId,
            TecnologiaEstandarNombre = p.TecnologiaEstandarNombre,
            BuildingBlockId = p.BuildingBlockId,
            BuildingBlockNombre = p.BuildingBlockNombre,
            FechaInicioVigenciaContrato = p.FechaInicioVigenciaContrato,
            FechaFinVigenciaContrato = p.FechaFinVigenciaContrato,
            NumeroContratoCorporativo = p.NumeroContratoCorporativo,
            EstadoId = p.EstadoAdopcionId,
            EstadoAdopcion = p.EstadoAdopcionNombre,
            LiderCorporativo = p.LiderCorporativo,
            FechaInicio = p.FechaInicio,
            FechaEstimadaCierre = p.FechaEstimadaCierre,
            TotalEmpresas = p.TotalEmpresasConvocadas,
            EmpresasAlineadas = p.TotalEmpresasAlineadas,
            EmpresasEnDespliegue = p.TotalEmpresasEnDespliegue,
            EmpresasPendientes = p.TotalEmpresasPendientes,
            EmpresasNoAplica = p.TotalEmpresasNoAplica,
            PorcentajeAdopcion = p.PorcentajeAdopcion,
            IsActivo = p.IsActivo,
            IsTerminada = p.IsTerminada,
            IsCancelada = p.IsCancelada
        }).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            items = items.Where(i =>
                i.Codigo.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.Nombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.FamiliaNombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.TecnologiaEstandarNombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (i.LiderCorporativo != null && i.LiderCorporativo.Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        if (familiaId.HasValue && familiaId.Value > 0)
        {
            items = items.Where(i => i.FamiliaId == familiaId.Value).ToList();
        }

        if (estadoId.HasValue && estadoId.Value > 0)
        {
            items = items.Where(i => i.EstadoId == estadoId.Value).ToList();
        }

        var model = new ImplementationsIndexViewModel
        {
            Processes = items,
            Search = search,
            FamiliaId = familiaId,
            EstadoId = estadoId,
            Familias = familias,
            EstadosAdopcion = estados
        };

        return View("Implementations", model);
    }

    [HttpGet("Implementations/Create")]
    [HttpGet("/AdoptionProcess/Implementations/Create")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    public async Task<IActionResult> CreateImplementation(int? evaluacionId, int? familiaId, CancellationToken cancellationToken)
    {
        var defaultCode = $"IMPL-TSI-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100, 999)}";
        var model = new CreateImplementationViewModel
        {
            Codigo = defaultCode,
            FechaInicio = DateTime.Today,
            FamiliaId = familiaId,
            ProcesoEvaluacionOrigenId = evaluacionId
        };

        await PopulateCreateImplementationCatalogsAsync(model, cancellationToken);

        // Si se seleccionó una evaluación de origen, precargar datos
        if (evaluacionId.HasValue && evaluacionId.Value > 0)
        {
            var eval = await dbContext.AdoptionProcesses.AsNoTracking().FirstOrDefaultAsync(x => x.IdProcesoAdopcionTSI == evaluacionId.Value, cancellationToken);
            if (eval is null) return View("CreateImplementation", model);

            if (string.IsNullOrWhiteSpace(model.Nombre))
            {
                model.Nombre = $"Implementación Corporativa - {eval.NombreProceso}";
            }
            model.BuildingBlockId = eval.IdBuildingBlock;
            model.LiderCorporativo = eval.LiderCorporativoTSI;

            var bb = await dbContext.BuildingBlocks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == eval.IdBuildingBlock, cancellationToken);
            if (bb?.IdFamilia.HasValue == true)
            {
                model.FamiliaId = bb.IdFamilia.Value;
            }

            // Buscar estándar decretado en la evaluación
            var std = await dbContext.StandardTechnologyHistories.AsNoTracking()
                .Where(s => s.IdBuildingBlock == eval.IdBuildingBlock && s.RolEstandar == "PRINCIPAL" && s.EstadoVigencia == "ACTIVO_VIGENTE")
                .OrderByDescending(s => s.FechaRegistro)
                .FirstOrDefaultAsync(cancellationToken);

            if (std is not null)
            {
                model.TecnologiaEstandarId = std.IdTecnologiaTSI;
                model.FechaInicioVigenciaContrato = std.FechaInicioVigencia;
                model.FechaFinVigenciaContrato = std.FechaFinVigencia;
            }

            // Precargar empresas convocadas en la evaluación
            var evalCompanies = await dbContext.AdoptionProcessCompanies.AsNoTracking()
                .Where(cp => cp.IdProcesoAdopcionTSI == evaluacionId.Value && cp.Aplica)
                .ToDictionaryAsync(cp => cp.IdEmpresaSubsidiaria, cancellationToken);

            foreach (var sub in model.Subsidiaries)
            {
                if (evalCompanies.TryGetValue(sub.EmpresaId, out var ec))
                {
                    sub.Selected = true;
                    sub.Aplica = true;
                    sub.JustificacionNoAplica = null;
                    sub.ContactoFocalId = ec.IdContactoEmpresaSubsidiaria;
                }
            }
        }

        return View("CreateImplementation", model);
    }

    [HttpPost("Implementations/Create")]
    [HttpPost("/AdoptionProcess/Implementations/Create")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveImplementation(CreateImplementationViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCreateImplementationCatalogsAsync(model, cancellationToken);
            return View("CreateImplementation", model);
        }

        var actor = ActorId();
        if (actor is null) return Forbid();

        // Determinar Building Block si no fue explicitado
        if (!model.BuildingBlockId.HasValue || model.BuildingBlockId.Value <= 0)
        {
            // Intentar derivar del estándar o tecnología
            var std = await dbContext.StandardTechnologyHistories.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdTecnologiaTSI == model.TecnologiaEstandarId && s.RolEstandar == "PRINCIPAL", cancellationToken);
            if (std is not null)
            {
                model.BuildingBlockId = std.IdBuildingBlock;
            }
        }

        var selectedCompanies = model.Subsidiaries
            .Where(s => s.Selected)
            .Select(s => new ConveneImplementationCompanyInput(
                s.EmpresaId,
                s.ContactoFocalId,
                true,
                null,
                s.FechaInicioAdopcion,
                s.EspecialistaTecnico,
                s.UrlArquitectura,
                s.EstadoImplementacion,
                s.TecnologiaAsIs))
            .ToList();

        var cmd = new CreateImplementationProcessCommand(
            Codigo: model.Codigo,
            Nombre: model.Nombre,
            FamiliaId: model.FamiliaId,
            TecnologiaEstandarId: model.TecnologiaEstandarId,
            BuildingBlockId: model.BuildingBlockId,
            FechaInicioVigenciaContrato: model.FechaInicioVigenciaContrato,
            FechaFinVigenciaContrato: model.FechaFinVigenciaContrato,
            NumeroContratoCorporativo: model.NumeroContratoCorporativo,
            EstadoAdopcionId: model.EstadoAdopcionId,
            Objetivo: model.Objetivo,
            Alcance: model.Alcance,
            LiderCorporativo: model.LiderCorporativo,
            FechaInicio: model.FechaInicio,
            FechaEstimadaCierre: model.FechaEstimadaCierre,
            ProcesoEvaluacionOrigenId: model.ProcesoEvaluacionOrigenId,
            Empresas: selectedCompanies,
            ActorUserId: actor.Value,
            CorrelationId: HttpContext.TraceIdentifier);

        var result = await adoptionService.CreateImplementationProcessAsync(cmd, cancellationToken);
        if (!result.Succeeded || !result.EntityId.HasValue)
        {
            TempData["ErrorMessage"] = result.Message;
            await PopulateCreateImplementationCatalogsAsync(model, cancellationToken);
            return View("CreateImplementation", model);
        }

        TempData["SuccessMessage"] = "Solicitud de implementación creada exitosamente con sus empresas convocadas.";
        return RedirectToAction("ImplementationDetails", new { id = result.EntityId.Value });
    }

    [HttpGet("Implementations/{id:int}")]
    [HttpGet("/AdoptionProcess/Implementations/{id:int}")]
    public async Task<IActionResult> ImplementationDetails(int id, CancellationToken cancellationToken)
    {
        var detail = await adoptionService.GetImplementationDetailAsync(id, cancellationToken);
        if (detail is null)
        {
            return NotFound();
        }

        var estados = await dbContext.TechnologyAdoptionStates.AsNoTracking()
            .OrderBy(s => s.Nombre)
            .Select(s => new CatalogOption(s.Id, s.Nombre ?? $"Estado #{s.Id}"))
            .ToListAsync(cancellationToken);

        var familias = await dbContext.Families.AsNoTracking()
            .OrderBy(f => f.Nombre)
            .Select(f => new CatalogOption(f.Id, f.Nombre ?? $"Familia #{f.Id}"))
            .ToListAsync(cancellationToken);

        var techsQuery = dbContext.Technologies.AsNoTracking();
        if (detail.FamiliaId.HasValue && detail.FamiliaId.Value > 0)
        {
            techsQuery = techsQuery.Where(t => t.IdFamilia == detail.FamiliaId.Value);
        }
        var techOptions = await techsQuery
            .OrderBy(t => t.NombreCorporativo)
            .Select(t => new CatalogOption(t.Id, t.NombreCorporativo ?? $"Tecnología #{t.Id}"))
            .ToListAsync(cancellationToken);

        if (detail.TecnologiaEstandarId.HasValue && !techOptions.Any(t => t.Id == detail.TecnologiaEstandarId.Value) && detail.TecnologiaEstandarNombre != "Por definir")
        {
            techOptions.Insert(0, new CatalogOption(detail.TecnologiaEstandarId.Value, detail.TecnologiaEstandarNombre));
        }

        var companyContacts = await dbContext.CompanyContacts.AsNoTracking()
            .Where(c => c.IdEmpresaSubsidiaria.HasValue)
            .OrderBy(c => c.NombreContactoEmpresaSubsidiaria)
            .Select(c => new CompanySpecialistDto
            {
                Id = c.Id,
                EmpresaId = c.IdEmpresaSubsidiaria!.Value,
                Nombre = c.NombreContactoEmpresaSubsidiaria ?? $"Contacto #{c.Id}",
                Rol = c.Rol,
                Plataformas = c.EspecialistaEnQuePlataformas,
                Email = c.Email
            })
            .ToListAsync(cancellationToken);

        var partnerContacts = await (from pc in dbContext.PartnerContacts.AsNoTracking()
                                     join p in dbContext.Partners.AsNoTracking() on pc.IdPartner equals p.Id into pGroup
                                     from p in pGroup.DefaultIfEmpty()
                                     orderby p != null ? p.NombrePartner : "", pc.NombreContactoPartner
                                     select new PartnerSpecialistDto
                                     {
                                         Id = pc.Id,
                                         PartnerId = pc.IdPartner,
                                         PartnerNombre = p != null && !string.IsNullOrWhiteSpace(p.NombrePartner) ? p.NombrePartner : "Partner Adjudicado",
                                         Nombre = pc.NombreContactoPartner ?? $"Contacto #{pc.Id}",
                                         Rol = pc.Rol,
                                         Email = pc.Email,
                                         TecnologiaId = p != null ? p.IdTecnologiaTSI : null
                                     })
                                     .ToListAsync(cancellationToken);

        var allCompanies = await dbContext.Companies.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CatalogOption(c.Id, $"{c.Nombre} ({c.Pais})"))
            .ToListAsync(cancellationToken);

        var model = new ImplementationDetailsViewModel
        {
            Process = detail,
            EstadosAdopcion = estados,
            Familias = familias,
            TecnologiasDisponibles = techOptions,
            EspecialistasEmpresas = companyContacts,
            EspecialistasPartners = partnerContacts,
            TodasLasEmpresas = allCompanies
        };

        return View("ImplementationDetails", model);
    }

    [HttpPost("Implementations/Company/Update")]
    [HttpPost("/AdoptionProcess/Implementations/Company/Update")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateImplementationCompany(UpdateImplementationCompanyFormModel form, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        string? rutaArchivo = null;
        string? nombreArchivo = null;

        if (form.ArchivoArquitectura != null && form.ArchivoArquitectura.Length > 0)
        {
            // Validar extensión
            var allowedExtensions = new[] { ".pdf", ".png", ".jpg", ".jpeg", ".drawio", ".docx", ".xlsx", ".zip" };
            var ext = Path.GetExtension(form.ArchivoArquitectura.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                TempData["ErrorMessage"] = "Tipo de archivo no permitido. Extensiones válidas: PDF, PNG, JPG, DRAWIO, DOCX, XLSX, ZIP.";
                return RedirectToAction("ImplementationDetails", new { id = form.ProcesoId });
            }

            // Validar tamaño máx: 15MB
            if (form.ArchivoArquitectura.Length > 15 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "El archivo excede el tamaño máximo permitido de 15 MB.";
                return RedirectToAction("ImplementationDetails", new { id = form.ProcesoId });
            }

            var webRoot = webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadDir = Path.Combine(webRoot, "uploads", "architectures", form.ProcesoId.ToString());
            Directory.CreateDirectory(uploadDir);

            var safeOriginalName = Path.GetFileName(form.ArchivoArquitectura.FileName);
            var uniqueFileName = $"{form.ProcesoEmpresaId}_{Guid.NewGuid():N}_{safeOriginalName}";
            var fullPath = Path.Combine(uploadDir, uniqueFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await form.ArchivoArquitectura.CopyToAsync(stream, cancellationToken);
            }

            rutaArchivo = Path.Combine("uploads", "architectures", form.ProcesoId.ToString(), uniqueFileName).Replace("\\", "/");
            nombreArchivo = safeOriginalName;
        }

        var cmd = new UpdateImplementationCompanyCommand(
            ProcesoId: form.ProcesoId,
            ProcesoEmpresaId: form.ProcesoEmpresaId,
            Aplica: form.Aplica,
            JustificacionNoAplica: form.JustificacionNoAplica,
            FechaInicioAdopcion: form.FechaInicioAdopcion,
            EspecialistaTecnico: form.EspecialistaTecnico,
            IdContactoEspecialista: form.IdContactoEspecialista,
            UrlArquitectura: form.UrlArquitectura,
            RutaArchivoArquitectura: rutaArchivo,
            NombreArchivoArquitectura: nombreArchivo,
            EstadoImplementacion: form.EstadoImplementacion,
            ActorUserId: actor.Value,
            CorrelationId: HttpContext.TraceIdentifier,
            TecnologiaAsIs: form.TecnologiaAsIs);

        var result = await adoptionService.UpdateImplementationCompanyAsync(cmd, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Avance de la empresa actualizado correctamente.";
        }

        return RedirectToAction("ImplementationDetails", new { id = form.ProcesoId });
    }

    [HttpGet("Implementations/{id:int}/Edit")]
    [HttpGet("/AdoptionProcess/Implementations/{id:int}/Edit")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    public async Task<IActionResult> EditImplementation(int id, CancellationToken cancellationToken)
    {
        var p = await dbContext.AdoptionProcesses.AsNoTracking().FirstOrDefaultAsync(x => x.IdProcesoAdopcionTSI == id && x.TipoProceso == "IMPLEMENTACION", cancellationToken);
        if (p is null) return NotFound();

        var model = new EditImplementationViewModel
        {
            Id = p.IdProcesoAdopcionTSI,
            Codigo = p.CodigoProceso,
            Nombre = p.NombreProceso,
            FamiliaId = p.IdFamilia,
            TecnologiaEstandarId = p.IdTecnologiaEstandar ?? 0,
            BuildingBlockId = p.IdBuildingBlock,
            FechaInicioVigenciaContrato = p.FechaInicioVigenciaContrato,
            FechaFinVigenciaContrato = p.FechaFinVigenciaContrato,
            NumeroContratoCorporativo = p.NumeroContratoCorporativo,
            EstadoAdopcionId = p.IdEstadoAdopcionTSI,
            Objetivo = p.Objetivo,
            Alcance = p.Alcance,
            LiderCorporativo = p.LiderCorporativoTSI,
            FechaInicio = p.FechaInicio,
            FechaEstimadaCierre = p.FechaEstimadaCierre
        };

        await PopulateEditImplementationCatalogsAsync(model, cancellationToken);
        return View("EditImplementation", model);
    }

    [HttpPost("Implementations/{id:int}/Edit")]
    [HttpPost("/AdoptionProcess/Implementations/{id:int}/Edit")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateImplementation(int id, EditImplementationViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateEditImplementationCatalogsAsync(model, cancellationToken);
            return View("EditImplementation", model);
        }

        var actor = ActorId();
        if (actor is null) return Forbid();

        var cmd = new UpdateImplementationProcessCommand(
            id,
            model.Nombre.Trim(),
            model.FamiliaId,
            model.TecnologiaEstandarId > 0 ? model.TecnologiaEstandarId : null,
            model.BuildingBlockId,
            model.FechaInicioVigenciaContrato,
            model.FechaFinVigenciaContrato,
            model.NumeroContratoCorporativo?.Trim(),
            model.EstadoAdopcionId,
            model.Objetivo?.Trim(),
            model.Alcance?.Trim(),
            model.LiderCorporativo?.Trim(),
            model.FechaInicio,
            model.FechaEstimadaCierre,
            actor.Value,
            HttpContext.TraceIdentifier);

        var result = await adoptionService.UpdateImplementationProcessAsync(cmd, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
            await PopulateEditImplementationCatalogsAsync(model, cancellationToken);
            return View("EditImplementation", model);
        }

        TempData["SuccessMessage"] = "Solicitud de implementación actualizada exitosamente.";
        return RedirectToAction("ImplementationDetails", new { id });
    }

    [HttpPost("Implementations/{id:int}/Decommission")]
    [HttpPost("/AdoptionProcess/Implementations/{id:int}/Decommission")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DecommissionImplementation(int id, string? motivoBaja, string? returnUrl, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.DecommissionImplementationProcessAsync(id, motivoBaja ?? "Solicitud dada de baja", actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Implementations");
    }

    [HttpPost("Implementations/{id:int}/Delete")]
    [HttpPost("/AdoptionProcess/Implementations/{id:int}/Delete")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImplementation(int id, string? returnUrl, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.DeleteImplementationProcessAsync(id, actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "La solicitud de implementación fue eliminada exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Implementations");
    }

    [HttpPost("Implementations/{id:int}/ManageCompanies")]
    [HttpPost("/AdoptionProcess/Implementations/{id:int}/ManageCompanies")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageCompanies(int id, [FromForm] List<int> selectedCompanyIds, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();

        var result = await adoptionService.ManageImplementationCompaniesAsync(id, selectedCompanyIds ?? [], actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction("ImplementationDetails", new { id });
    }

    [HttpGet("Implementations/Architecture/Download/{procesoEmpresaId:int}")]
    [HttpGet("/AdoptionProcess/Implementations/Architecture/Download/{procesoEmpresaId:int}")]
    public async Task<IActionResult> DownloadArchitectureFile(int procesoEmpresaId, CancellationToken cancellationToken)
    {
        var pae = await dbContext.AdoptionProcessCompanies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdProcesoAdopcionEmpresa == procesoEmpresaId, cancellationToken);

        if (pae is null || string.IsNullOrWhiteSpace(pae.RutaArchivoArquitectura))
        {
            return NotFound("Archivo no encontrado o no registrado.");
        }

        var webRoot = webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var filePath = Path.Combine(webRoot, pae.RutaArchivoArquitectura.TrimStart('/').Replace("/", "\\"));

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("El archivo físico no se encuentra en el servidor.");
        }

        var fileName = !string.IsNullOrWhiteSpace(pae.NombreArchivoArquitectura)
            ? pae.NombreArchivoArquitectura
            : Path.GetFileName(filePath);

        return PhysicalFile(filePath, "application/octet-stream", fileName);
    }

    [HttpGet("Implementations/{id:int}/Reports")]
    [HttpGet("/AdoptionProcess/Implementations/{id:int}/Reports")]
    public async Task<IActionResult> ImplementationReports(int id, CancellationToken cancellationToken)
    {
        var reports = await adoptionService.GetImplementationReportsAsync(id, cancellationToken);
        if (reports is null)
        {
            return NotFound();
        }

        return View("ImplementationReports", reports);
    }

    [HttpGet("Implementations/Api/StandardsByFamily/{familyId:int?}")]
    [HttpGet("/AdoptionProcess/Implementations/Api/StandardsByFamily/{familyId:int?}")]
    public async Task<IActionResult> GetStandardsByFamilyApi(int? familyId, CancellationToken cancellationToken)
    {
        var standards = await adoptionService.GetCorporateStandardsByFamilyAsync(familyId, cancellationToken);
        return Json(standards);
    }

    [HttpGet("Implementations/Api/SpecialistsLookup")]
    [HttpGet("/AdoptionProcess/Implementations/Api/SpecialistsLookup")]
    public async Task<IActionResult> GetSpecialistsLookupApi(int? empresaId, int? tecnologiaId, CancellationToken cancellationToken)
    {
        var companyQuery = dbContext.CompanyContacts.AsNoTracking().Where(c => c.IdEmpresaSubsidiaria.HasValue);
        if (empresaId.HasValue && empresaId.Value > 0)
        {
            companyQuery = companyQuery.Where(c => c.IdEmpresaSubsidiaria == empresaId.Value);
        }

        var companyContacts = await companyQuery
            .OrderBy(c => c.NombreContactoEmpresaSubsidiaria)
            .Select(c => new CompanySpecialistDto
            {
                Id = c.Id,
                EmpresaId = c.IdEmpresaSubsidiaria!.Value,
                Nombre = c.NombreContactoEmpresaSubsidiaria ?? $"Contacto #{c.Id}",
                Rol = c.Rol,
                Plataformas = c.EspecialistaEnQuePlataformas,
                Email = c.Email
            })
            .ToListAsync(cancellationToken);

        var partnerContacts = await (from pc in dbContext.PartnerContacts.AsNoTracking()
                                     join p in dbContext.Partners.AsNoTracking() on pc.IdPartner equals p.Id into pGroup
                                     from p in pGroup.DefaultIfEmpty()
                                     orderby p != null ? p.NombrePartner : "", pc.NombreContactoPartner
                                     select new PartnerSpecialistDto
                                     {
                                         Id = pc.Id,
                                         PartnerId = pc.IdPartner,
                                         PartnerNombre = p != null && !string.IsNullOrWhiteSpace(p.NombrePartner) ? p.NombrePartner : "Partner Adjudicado",
                                         Nombre = pc.NombreContactoPartner ?? $"Contacto #{pc.Id}",
                                         Rol = pc.Rol,
                                         Email = pc.Email,
                                         TecnologiaId = p != null ? p.IdTecnologiaTSI : null
                                     })
                                     .ToListAsync(cancellationToken);

        return Json(new { companyContacts, partnerContacts });
    }

    [HttpGet("Implementations/Api/EvaluationData/{evaluacionId:int}")]
    [HttpGet("/AdoptionProcess/Implementations/Api/EvaluationData/{evaluacionId:int}")]
    public async Task<IActionResult> GetEvaluationDataApi(int evaluacionId, CancellationToken cancellationToken)
    {
        var eval = await dbContext.AdoptionProcesses.AsNoTracking().FirstOrDefaultAsync(x => x.IdProcesoAdopcionTSI == evaluacionId, cancellationToken);
        if (eval is null) return NotFound();

        int? familiaId = null;
        if (eval.IdBuildingBlock > 0)
        {
            var bb = await dbContext.BuildingBlocks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == eval.IdBuildingBlock, cancellationToken);
            familiaId = bb?.IdFamilia;
        }

        int? techId = null;
        string? techName = null;
        DateTime? inicioContrato = null;
        DateTime? finContrato = null;

        var std = await dbContext.StandardTechnologyHistories.AsNoTracking()
            .Where(s => s.IdBuildingBlock == eval.IdBuildingBlock && s.RolEstandar == "PRINCIPAL" && s.EstadoVigencia == "ACTIVO_VIGENTE")
            .OrderByDescending(s => s.FechaRegistro)
            .FirstOrDefaultAsync(cancellationToken);

        if (std is not null)
        {
            techId = std.IdTecnologiaTSI;
            inicioContrato = std.FechaInicioVigencia;
            finContrato = std.FechaFinVigencia;
            var tech = await dbContext.Technologies.AsNoTracking().FirstOrDefaultAsync(t => t.Id == std.IdTecnologiaTSI, cancellationToken);
            techName = tech?.NombreCorporativo;
        }

        var convenedCompanyIds = await dbContext.AdoptionProcessCompanies.AsNoTracking()
            .Where(cp => cp.IdProcesoAdopcionTSI == evaluacionId && cp.Aplica)
            .Select(cp => cp.IdEmpresaSubsidiaria)
            .ToListAsync(cancellationToken);

        return Json(new
        {
            buildingBlockId = eval.IdBuildingBlock,
            familiaId,
            liderCorporativo = eval.LiderCorporativoTSI,
            tecnologiaEstandarId = techId,
            tecnologiaEstandarNombre = techName,
            fechaInicioVigenciaContrato = inicioContrato?.ToString("yyyy-MM-dd"),
            fechaFinVigenciaContrato = finContrato?.ToString("yyyy-MM-dd"),
            convenedCompanyIds
        });
    }

    [HttpPost("Implementations/Api/CreateSpecialist")]
    [HttpPost("/AdoptionProcess/Implementations/Api/CreateSpecialist")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    public async Task<IActionResult> CreateSpecialistApi([FromBody] CreateSpecialistFormModel form, CancellationToken cancellationToken)
    {
        if (form.EmpresaId <= 0 || string.IsNullOrWhiteSpace(form.Nombre))
        {
            return BadRequest(new { success = false, message = "Empresa y Nombre son obligatorios." });
        }

        var contact = new Landscape.Tsi.Domain.Catalogs.TContactoEmpresaSubsidiaria
        {
            IdEmpresaSubsidiaria = form.EmpresaId,
            NombreContactoEmpresaSubsidiaria = form.Nombre.Trim(),
            Rol = form.Rol?.Trim(),
            Email = form.Email?.Trim(),
            Telefono = form.Telefono?.Trim(),
            EspecialistaEnQuePlataformas = form.Plataformas?.Trim()
        };

        dbContext.CompanyContacts.Add(contact);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Json(new
        {
            success = true,
            id = contact.Id,
            empresaId = contact.IdEmpresaSubsidiaria,
            nombre = contact.NombreContactoEmpresaSubsidiaria,
            rol = contact.Rol,
            email = contact.Email,
            plataformas = contact.EspecialistaEnQuePlataformas
        });
    }

    private async Task PopulateCreateImplementationCatalogsAsync(CreateImplementationViewModel model, CancellationToken cancellationToken)
    {
        model.Familias = await dbContext.Families.AsNoTracking()
            .OrderBy(f => f.Nombre)
            .Select(f => new CatalogOption(f.Id, f.Nombre ?? $"Familia #{f.Id}"))
            .ToListAsync(cancellationToken);

        model.EstadosAdopcion = await dbContext.TechnologyAdoptionStates.AsNoTracking()
            .OrderBy(s => s.Nombre)
            .Select(s => new CatalogOption(s.Id, s.Nombre ?? $"Estado #{s.Id}"))
            .ToListAsync(cancellationToken);

        model.BuildingBlocks = await dbContext.BuildingBlocks.AsNoTracking()
            .OrderBy(b => b.Nombre)
            .Select(b => new CatalogOption(b.Id, b.Nombre ?? $"Building Block #{b.Id}"))
            .ToListAsync(cancellationToken);

        model.TecnologiasDisponibles = await dbContext.Technologies.AsNoTracking()
            .OrderBy(t => t.NombreCorporativo)
            .Select(t => new CatalogOption(t.Id, t.NombreCorporativo ?? $"Tecnología #{t.Id}"))
            .ToListAsync(cancellationToken);

        model.EstandaresDecretados = await adoptionService.GetCorporateStandardsByFamilyAsync(model.FamiliaId, cancellationToken);

        model.EvaluacionesPrevias = await dbContext.AdoptionProcesses.AsNoTracking()
            .Where(p => p.TipoProceso != "IMPLEMENTACION")
            .OrderByDescending(p => p.FechaInicio)
            .Select(p => new CatalogOption(p.IdProcesoAdopcionTSI, $"{p.CodigoProceso} - {p.NombreProceso}"))
            .ToListAsync(cancellationToken);

        var partnerContacts = await (from pc in dbContext.PartnerContacts.AsNoTracking()
                                     join p in dbContext.Partners.AsNoTracking() on pc.IdPartner equals p.Id into pGroup
                                     from p in pGroup.DefaultIfEmpty()
                                     orderby p != null ? p.NombrePartner : "", pc.NombreContactoPartner
                                     select new CatalogOption(pc.Id, $"{pc.NombreContactoPartner} (Partner: {(p != null ? p.NombrePartner : "Adjudicado")}) - {pc.Rol ?? "Especialista"}"))
                                     .ToListAsync(cancellationToken);

        model.EspecialistasPartnersDisponibles = partnerContacts;

        if (model.Subsidiaries.Count == 0)
        {
            var companies = await dbContext.Companies.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync(cancellationToken);
            var contacts = await dbContext.CompanyContacts.AsNoTracking().ToListAsync(cancellationToken);
            var contactsByComp = contacts.GroupBy(c => c.IdEmpresaSubsidiaria ?? 0)
                .ToDictionary(g => g.Key, g => g.Select(c => new CatalogOption(c.Id, $"{c.NombreContactoEmpresaSubsidiaria} - {c.Rol ?? "Especialista"}")).ToList());

            model.Subsidiaries = companies.Select(c => new ImplementationSubsidiaryCheckboxItem
            {
                EmpresaId = c.Id,
                EmpresaNombre = c.Nombre ?? $"Empresa #{c.Id}",
                Pais = c.Pais,
                Rubro = c.Rubro,
                Selected = false,
                Aplica = true,
                EstadoImplementacion = "PENDIENTE",
                EspecialistasDisponibles = contactsByComp.GetValueOrDefault(c.Id, [])
            }).ToList();
        }
    }

    private async Task PopulateEditImplementationCatalogsAsync(EditImplementationViewModel model, CancellationToken cancellationToken)
    {
        model.Familias = await dbContext.Families.AsNoTracking()
            .OrderBy(f => f.Nombre)
            .Select(f => new CatalogOption(f.Id, f.Nombre ?? $"Familia #{f.Id}"))
            .ToListAsync(cancellationToken);

        model.EstadosAdopcion = await dbContext.TechnologyAdoptionStates.AsNoTracking()
            .OrderBy(s => s.Nombre)
            .Select(s => new CatalogOption(s.Id, s.Nombre ?? $"Estado #{s.Id}"))
            .ToListAsync(cancellationToken);

        model.BuildingBlocks = await dbContext.BuildingBlocks.AsNoTracking()
            .OrderBy(b => b.Nombre)
            .Select(b => new CatalogOption(b.Id, b.Nombre ?? $"Building Block #{b.Id}"))
            .ToListAsync(cancellationToken);

        model.TecnologiasDisponibles = await dbContext.Technologies.AsNoTracking()
            .OrderBy(t => t.NombreCorporativo)
            .Select(t => new CatalogOption(t.Id, t.NombreCorporativo ?? $"Tecnología #{t.Id}"))
            .ToListAsync(cancellationToken);

        model.EstandaresDecretados = await adoptionService.GetCorporateStandardsByFamilyAsync(model.FamiliaId, cancellationToken);
    }
}
