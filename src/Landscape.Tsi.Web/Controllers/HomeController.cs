using System.Diagnostics;

using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Web.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landscape.Tsi.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index() => View(new CatalogMapViewModel
    {
        Entities = MasterCatalogRegistry.EntityMetadata,
        Relationships = MasterCatalogRegistry.LogicalRelationships
    });

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