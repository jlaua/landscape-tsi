using Landscape.Tsi.Application.Identity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landscape.Tsi.Web.Controllers;

[Authorize(Policy = Permissions.UserManage)]
public sealed class AdministrationController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}