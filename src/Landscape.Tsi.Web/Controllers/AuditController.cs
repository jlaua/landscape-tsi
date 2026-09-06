using System.Security.Claims;

using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Web.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landscape.Tsi.Web.Controllers;

[Authorize(Policy = Permissions.AuditView)]
public sealed class AuditController(IAuthorizationAuditQuery query) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? empresaSubsidiariaId, CancellationToken cancellationToken)
    {
        if (!empresaSubsidiariaId.HasValue)
        {
            return View(new AuditQueryViewModel());
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            var events = await query.ListAsync(
                userId, empresaSubsidiariaId.Value, HttpContext.TraceIdentifier, cancellationToken);
            return View(new AuditQueryViewModel
            {
                EmpresaSubsidiariaId = empresaSubsidiariaId,
                Events = events
            });
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
    }
}
