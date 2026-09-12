using System.IO;
using System.Text.RegularExpressions;

using Landscape.Tsi.Infrastructure.Catalogs;

namespace Landscape.Tsi.Tests.Web;

public sealed class BuildingBlockAssignmentAccessibilityTests
{
    private static readonly string RepoRoot = FindRepositoryRoot();
    private static readonly string ViewsPath = Path.Combine(RepoRoot, "src", "Landscape.Tsi.Web", "Views", "MasterTables");
    private static readonly string JsPath = Path.Combine(RepoRoot, "src", "Landscape.Tsi.Web", "wwwroot", "js");

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Landscape.Tsi.slnx")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new InvalidOperationException("No se encontró la raíz del repositorio.");
    }

    [Theory]
    [InlineData("_AssociateCapabilityModal.cshtml")]
    [InlineData("_ReassignCapabilityModal.cshtml")]
    [InlineData("_AssociateFunctionalityModal.cshtml")]
    [InlineData("_ReassignFunctionalityModal.cshtml")]
    public void Modals_ComplyWithWcagAccessibilityStandards(string viewFileName)
    {
        var filePath = Path.Combine(ViewsPath, viewFileName);
        Assert.True(File.Exists(filePath), $"Modal view file {viewFileName} must exist.");

        var content = File.ReadAllText(filePath);

        // 1. Modal wrapper has role/class, tabindex="-1", aria-labelledby, and aria-hidden="true"
        Assert.Contains("tabindex=\"-1\"", content);
        Assert.Contains("aria-hidden=\"true\"", content);
        Assert.Contains("aria-labelledby=", content);

        // 2. Close button has accessible label
        Assert.Contains("aria-label=\"Cerrar\"", content);

        // 3. Form fields have associated labels with 'for' attribute
        var labelForMatches = Regex.Matches(content, @"<label\s+[^>]*for=""([^""]+)""");
        Assert.NotEmpty(labelForMatches);
        foreach (Match match in labelForMatches)
        {
            var id = match.Groups[1].Value;
            Assert.True(
                content.Contains($"id=\"{id}\""),
                $"Label with for=\"{id}\" in {viewFileName} must match an element with id=\"{id}\".");
        }
    }

    [Fact]
    public void CatalogDetailsView_FunctionalitiesTable_HasAccessibleAriaSortAndReorderedColumns()
    {
        var filePath = Path.Combine(ViewsPath, "CatalogDetails.cshtml");
        Assert.True(File.Exists(filePath));

        var content = File.ReadAllText(filePath);

        // Verify aria-sort bindings
        Assert.Contains("aria-sort=\"@capSortAria\"", content);
        Assert.Contains("aria-sort=\"@funcSortAria\"", content);
        Assert.Contains("aria-sort=\"@statusSortAria\"", content);

        // Verify column headers order: Capacidad before Funcionalidad before Estado de funcionalidad
        var capHeaderIdx = content.IndexOf("Capacidad @(capSortAria", StringComparison.Ordinal);
        var funcHeaderIdx = content.IndexOf("Funcionalidad @(funcSortAria", StringComparison.Ordinal);
        var statusHeaderIdx = content.IndexOf("Estado de funcionalidad @(statusSortAria", StringComparison.Ordinal);

        Assert.True(capHeaderIdx > 0, "Capacidad column header must exist.");
        Assert.True(funcHeaderIdx > capHeaderIdx, "Funcionalidad header must follow Capacidad header.");
        Assert.True(statusHeaderIdx > funcHeaderIdx, "Estado header must follow Funcionalidad header.");

        // Verify responsive table wrapper has region role and tabindex for keyboard accessibility
        Assert.Contains("role=\"region\"", content);
        Assert.Contains("tabindex=\"0\"", content);

        // Verify modals and script inclusion
        Assert.Contains("_AssociateCapabilityModal", content);
        Assert.Contains("_ReassignCapabilityModal", content);
        Assert.Contains("_AssociateFunctionalityModal", content);
        Assert.Contains("_ReassignFunctionalityModal", content);
        Assert.Contains("assignment-modals.js", content);
    }

    [Fact]
    public void AssignmentModalsJs_ImplementsKeyboardTrapAndFocusRestoration()
    {
        var filePath = Path.Combine(JsPath, "assignment-modals.js");
        Assert.True(File.Exists(filePath), "assignment-modals.js must exist.");

        var jsContent = File.ReadAllText(filePath);

        // 1. Handles Tab / Shift+Tab keyboard trap
        Assert.Contains("e.key === \"Tab\"", jsContent);
        Assert.Contains("e.shiftKey", jsContent);

        // 2. Restores focus upon hidden.bs.modal
        Assert.Contains("hidden.bs.modal", jsContent);
        Assert.Contains("lastActiveElement.focus()", jsContent);

        // 3. Focuses first element upon shown.bs.modal
        Assert.Contains("shown.bs.modal", jsContent);

        // 4. Sanitizes dynamic text with escapeHtml
        Assert.Contains("escapeHtml", jsContent);
    }
}