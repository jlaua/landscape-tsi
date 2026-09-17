using System.Security.Claims;

using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Application.Reporting;
using Landscape.Tsi.Infrastructure.Catalogs;
using Landscape.Tsi.Web.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landscape.Tsi.Web.Controllers;

[Authorize(Policy = Permissions.CatalogView)]
[Route("reporteria")]
public sealed class ReportingController(
    IReportingService reporting,
    CatalogDbContext catalogDbContext) : Controller
{
    [HttpGet("")]
    [HttpGet("distribucion-dominios")]
    public async Task<IActionResult> Index([FromQuery] string? tab, CancellationToken cancellationToken = default)
    {
        var model = await CatalogTreemapBuilder.BuildAsync(catalogDbContext, cancellationToken);
        model.ActiveTab = string.Equals(tab, "catalogos", StringComparison.OrdinalIgnoreCase) ? "catalogos" : "dominios";
        return View("Index", model);
    }

    [HttpGet("catalogos")]
    public Task<IActionResult> Catalogs(CancellationToken cancellationToken = default)
        => Index("catalogos", cancellationToken);

    [HttpGet("distribucion-dominios-vista")]
    public Task<IActionResult> DomainDistribution(CancellationToken cancellationToken = default)
        => Index("dominios", cancellationToken);

    [HttpGet("empresas-ciso")]
    public async Task<IActionResult> CompaniesAndCiso(
        string? search, int? companyId, string? country, string? grouping, string? industry,
        string? ciso, bool? representativeOnly, bool onlyWithoutCiso = false,
        bool onlyWithoutRepresentative = false, bool allCiso = false, int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Forbid();
        var query = new CompanyCisoReportQuery(search, companyId, country, grouping, industry, ciso,
            representativeOnly, onlyWithoutCiso, onlyWithoutRepresentative, allCiso, page, 25, userId);
        var report = await reporting.GetCompanyCisoReportAsync(query, cancellationToken);
        var rows = report.Rows.Select(row => row with
        {
            CompanyRoute = Url.Action("CatalogDetails", "MasterTables", new { catalogRoute = "empresa-subsidiaria", id = row.CompanyId }),
            CisoRoute = row.CisoId.HasValue ? Url.Action("CatalogDetails", "MasterTables", new { catalogRoute = "ciso", id = row.CisoId.Value }) : null
        }).ToArray();
        return View(new CompanyCisoReportViewModel(report with { Rows = rows }));
    }

    [HttpGet("adopcion-empresas-tecnologia")]
    public async Task<IActionResult> CompanyAdoptionByTechnology(
        string? search, int? dominioId, int? buildingBlockId, int? empresaId,
        string? alignmentState, int page = 1, int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var query = new CompanyAdoptionReportQuery(search, dominioId, buildingBlockId, empresaId, alignmentState, page, pageSize);
        var report = await reporting.GetCompanyAdoptionReportAsync(query, cancellationToken);
        return View(report);
    }

    [HttpGet("api/catalogos")]
    public async Task<IActionResult> CatalogTotals([FromQuery] string? group, CancellationToken cancellationToken)
    {
        var points = await reporting.GetCatalogTotalsAsync(group, cancellationToken);
        return Ok(new { generatedAt = DateTimeOffset.UtcNow, items = points, relationCount = MasterCatalogRegistry.ReportingRelations.Count });
    }

    [HttpGet("api/catalogos/{code}/detalle")]
    public async Task<IActionResult> CatalogDetail(string code, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? sortColumn = null, [FromQuery] string? sortDirection = null, CancellationToken cancellationToken = default)
    {
        try { return Ok(await reporting.GetCatalogDetailAsync(code, search, page, pageSize, cancellationToken, sortColumn, sortDirection)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpGet("api/catalogos/{code}/relaciones")]
    public async Task<IActionResult> CatalogRelations(string code, CancellationToken cancellationToken = default)
    {
        try { return Ok(new { items = await reporting.GetCatalogRelationsAsync(code, cancellationToken) }); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpGet("api/catalogos/{childCode}/relacionados")]
    public async Task<IActionResult> RelatedDetail(string childCode, [FromQuery] string parentCode, [FromQuery] int parentId, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try { return Ok(await reporting.GetRelatedCatalogDetailAsync(parentCode, childCode, parentId, search, page, pageSize, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException) { return BadRequest(); }
    }

    [HttpGet("api/catalogos/{code}/registros/{id:int}/kpis")]
    public async Task<IActionResult> CatalogContextKpis(string code, int id, CancellationToken cancellationToken = default)
    {
        try { return Ok(new { items = await reporting.GetCatalogContextKpisAsync(code, id, cancellationToken) }); }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}