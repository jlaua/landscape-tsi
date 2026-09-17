using System.Diagnostics;

using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Catalogs;
using Landscape.Tsi.Infrastructure.Catalogs;
using Landscape.Tsi.Infrastructure.Reporting;
using Landscape.Tsi.Web.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Web.Controllers;

[Authorize]
public class HomeController(CatalogDbContext catalogDbContext) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await CatalogTreemapBuilder.BuildAsync(catalogDbContext, cancellationToken);
        return View(model);
    }

    [HttpGet("api/building-block-detail/{id:int}")]
    public async Task<IActionResult> GetBuildingBlockDetail(int id, CancellationToken cancellationToken)
    {
        var bb = await catalogDbContext.BuildingBlocks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (bb is null) return NotFound();

        var domain = bb.IdDominio.HasValue ? await catalogDbContext.Domains.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bb.IdDominio.Value, cancellationToken) : null;
        var phase = bb.IdFase.HasValue ? await catalogDbContext.AdoptionPhases.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bb.IdFase.Value, cancellationToken) : null;

        var caps = await catalogDbContext.Capabilities.AsNoTracking().Where(c => c.IdBuildingBlock == id).ToListAsync(cancellationToken);
        var capIds = caps.Select(c => c.Id).ToHashSet();

        var funcs = await catalogDbContext.Functionalities.AsNoTracking()
            .Where(f => f.IdCapacidad.HasValue && capIds.Contains(f.IdCapacidad.Value))
            .ToListAsync(cancellationToken);

        var capMap = caps.ToDictionary(c => c.Id, c => c.Nombre ?? $"Capacidad #{c.Id}");

        var funcStates = await catalogDbContext.FunctionalityStates.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Nombre, cancellationToken);

        var funcDtos = funcs.Select(f => new FunctionalityItemDto(
            f.Id,
            f.Nombre ?? $"Funcionalidad #{f.Id}",
            f.Descripcion,
            f.IdEstado.HasValue && funcStates.TryGetValue(f.IdEstado.Value, out var st) ? st : "Activa",
            f.IdCapacidad.HasValue && capMap.TryGetValue(f.IdCapacidad.Value, out var capNom) ? capNom : "General"
        )).OrderBy(f => f.CapacidadSeguridadNombre).ThenBy(f => f.Nombre).ToList();

        var impls = await catalogDbContext.ImplementedTechnologies.AsNoTracking()
            .Where(i => i.IdBuildingBlock == id)
            .ToListAsync(cancellationToken);

        var empIds = impls.Select(i => i.IdEmpresaSubsidiaria).Distinct().ToList();
        var companies = await catalogDbContext.Companies.AsNoTracking()
            .Where(c => empIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var techIds = impls.Select(i => i.IdTecnologiaTSI).Distinct().ToList();
        var techs = await catalogDbContext.Technologies.AsNoTracking()
            .Where(t => techIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var implDtos = impls.Select(i =>
        {
            companies.TryGetValue(i.IdEmpresaSubsidiaria, out var comp);
            techs.TryGetValue(i.IdTecnologiaTSI, out var tch);
            return new CompanyImplementationItemDto(
                i.IdEmpresaSubsidiaria,
                comp?.Nombre ?? $"Empresa #{i.IdEmpresaSubsidiaria}",
                comp?.Pais ?? "Global",
                tch?.NombreCorporativo ?? tch?.NombreLocal ?? "Tecnología Desplegada",
                i.VersionDesplegada ?? "Estándar",
                i.EsInstanciaCorporativa
            );
        }).ToList();

        var dto = new BuildingBlockDetailPopupDto(
            bb.Id,
            bb.Nombre ?? $"BB #{bb.Id}",
            domain?.Dominio ?? "Dominio TSI",
            bb.Definicion,
            bb.Pilar,
            phase?.Nombre ?? "En Evaluación",
            funcDtos,
            implDtos
        );

        return Json(dto);
    }

    [HttpGet("api/building-block-detail/{id:int}/export")]
    [Authorize(Policy = Permissions.CatalogExport)]
    public async Task<IActionResult> ExportBuildingBlockDetail(int id, CancellationToken cancellationToken)
    {
        var bb = await catalogDbContext.BuildingBlocks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (bb is null) return NotFound();

        var domain = bb.IdDominio.HasValue ? await catalogDbContext.Domains.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bb.IdDominio.Value, cancellationToken) : null;
        var caps = await catalogDbContext.Capabilities.AsNoTracking().Where(c => c.IdBuildingBlock == id).ToListAsync(cancellationToken);
        var capIds = caps.Select(c => c.Id).ToHashSet();
        var capMap = caps.ToDictionary(c => c.Id, c => c.Nombre ?? $"Capacidad #{c.Id}");

        var funcs = await catalogDbContext.Functionalities.AsNoTracking()
            .Where(f => f.IdCapacidad.HasValue && capIds.Contains(f.IdCapacidad.Value))
            .ToListAsync(cancellationToken);
        var funcStates = await catalogDbContext.FunctionalityStates.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Nombre, cancellationToken);

        var impls = await catalogDbContext.ImplementedTechnologies.AsNoTracking()
            .Where(i => i.IdBuildingBlock == id)
            .ToListAsync(cancellationToken);
        var empIds = impls.Select(i => i.IdEmpresaSubsidiaria).Distinct().ToList();
        var companies = await catalogDbContext.Companies.AsNoTracking()
            .Where(c => empIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);
        var techIds = impls.Select(i => i.IdTecnologiaTSI).Distinct().ToList();
        var techs = await catalogDbContext.Technologies.AsNoTracking()
            .Where(t => techIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var wb = new SimpleExcelWorkbook();

        var sheetFuncs = wb.CreateSheet("Funcionalidades");
        sheetFuncs.AddHeader("Ficha de Building Block:", bb.Nombre ?? $"BB #{bb.Id}");
        sheetFuncs.AddHeader("Dominio TSI:", domain?.Dominio ?? "—");
        sheetFuncs.AddHeader("Total Capacidades:", caps.Count.ToString());
        sheetFuncs.AddHeader("Total Funcionalidades:", funcs.Count.ToString());
        sheetFuncs.AddRow(string.Empty);

        sheetFuncs.AddHeader("#", "Capacidad de Seguridad", "Funcionalidad", "Descripción", "Estado");

        var funcIndex = 1;
        var orderedFuncs = funcs.OrderBy(f => f.IdCapacidad.HasValue && capMap.ContainsKey(f.IdCapacidad.Value) ? capMap[f.IdCapacidad.Value] : "General").ThenBy(f => f.Nombre);
        foreach (var f in orderedFuncs)
        {
            var capName = f.IdCapacidad.HasValue && capMap.TryGetValue(f.IdCapacidad.Value, out var cName) ? cName : "General";
            var stateName = f.IdEstado.HasValue && funcStates.TryGetValue(f.IdEstado.Value, out var sName) ? sName : "Activa";
            sheetFuncs.AddRow(
                (funcIndex++).ToString(),
                capName,
                f.Nombre ?? $"Funcionalidad #{f.Id}",
                f.Descripcion ?? "—",
                stateName);
        }

        var sheetImpls = wb.CreateSheet("Subsidiarias");
        sheetImpls.AddHeader("Building Block:", bb.Nombre ?? $"BB #{bb.Id}");
        sheetImpls.AddHeader("Total Implementaciones:", impls.Count.ToString());
        sheetImpls.AddRow(string.Empty);

        sheetImpls.AddHeader("#", "Subsidiaria", "País", "Tecnología AS-IS", "Versión Desplegada", "Instancia Corp.");

        var implIndex = 1;
        foreach (var i in impls)
        {
            companies.TryGetValue(i.IdEmpresaSubsidiaria, out var comp);
            techs.TryGetValue(i.IdTecnologiaTSI, out var tch);
            sheetImpls.AddRow(
                (implIndex++).ToString(),
                comp?.Nombre ?? $"Empresa #{i.IdEmpresaSubsidiaria}",
                comp?.Pais ?? "Global",
                tch?.NombreCorporativo ?? tch?.NombreLocal ?? "Tecnología Desplegada",
                i.VersionDesplegada ?? "Estándar",
                i.EsInstanciaCorporativa ? "Sí" : "No");
        }

        var safeBbName = string.Join("", (bb.Nombre ?? "BB").Where(char.IsLetterOrDigit));
        var fileName = $"Landscape_TSI_BB_{id}_{safeBbName}.xlsx";
        return File(wb.Build(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}