using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Application.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landscape.Tsi.Web.Controllers;

[Authorize(Policy = Permissions.CatalogView)]
[Route("reporteria")]
public sealed class ReportingController(IReportingService reporting) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpGet("api/catalogos")]
    public async Task<IActionResult> CatalogTotals([FromQuery] string? group, CancellationToken cancellationToken)
    {
        var points = await reporting.GetCatalogTotalsAsync(group, cancellationToken);
        return Ok(new { generatedAt = DateTimeOffset.UtcNow, items = points });
    }
}
