using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landscape.Tsi.Web.Controllers;

[Authorize]
public class DocumentationController : Controller
{
    private readonly IWebHostEnvironment _environment;

    public DocumentationController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult UserManual()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Diagram()
    {
        var localDocsPath = Path.Combine(_environment.ContentRootPath, "..", "..", "docs", "diagramas", "arquitectura-landscape-tsi.archify.html");
        if (System.IO.File.Exists(localDocsPath))
        {
            return PhysicalFile(Path.GetFullPath(localDocsPath), "text/html");
        }

        var wwwrootPath = Path.Combine(_environment.WebRootPath, "diagramas", "arquitectura.html");
        if (System.IO.File.Exists(wwwrootPath))
        {
            return PhysicalFile(Path.GetFullPath(wwwrootPath), "text/html");
        }

        return NotFound("No se encontró el diagrama de arquitectura generado.");
    }
}