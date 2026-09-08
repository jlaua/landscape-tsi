using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Web.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landscape.Tsi.Web.Controllers;

[Authorize(Policy = Permissions.CatalogView)]
[Route("Administration/EntityView")]
public sealed class EntityViewController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(new CatalogMapViewModel
    {
        Entities = MasterCatalogRegistry.EntityMetadata,
        Relationships = MasterCatalogRegistry.LogicalRelationships
    });
}