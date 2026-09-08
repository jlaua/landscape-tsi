namespace Landscape.Tsi.Tests.Web;

public sealed class TechnologyMappingHierarchyTests
{
    [Fact]
    public void IndexView_PresentsBuildingBlockHierarchyBeforeFamilyAnalysis()
    {
        var repositoryRoot = FindRepositoryRoot();
        var view = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Landscape.Tsi.Web", "Views", "TechnologyMapping", "Index.cshtml"));

        var hierarchy = view.IndexOf("Building Blocks y Tecnologías TSI relacionadas", StringComparison.Ordinal);
        var familyAnalysis = view.IndexOf("Building Blocks por Familia", StringComparison.Ordinal);

        Assert.True(hierarchy >= 0, "La vista debe identificar la jerarquía principal de Building Blocks y Tecnologías TSI.");
        Assert.True(familyAnalysis > hierarchy, "El análisis por Familia debe permanecer después del listado principal.");
        Assert.Contains("item.BuildingBlockName", view, StringComparison.Ordinal);
        Assert.Contains("item.Technologies", view, StringComparison.Ordinal);
        Assert.Contains("Administrar tecnologías", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Tecnologías sin asignar", view, StringComparison.Ordinal);
        Assert.Contains("Dominio", view, StringComparison.Ordinal);
        Assert.Contains("Fase de adopción", view, StringComparison.Ordinal);
        Assert.DoesNotContain("item.TechnologyName", view, StringComparison.Ordinal);
    }

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

        throw new DirectoryNotFoundException("No se encontró la raíz del repositorio.");
    }
}