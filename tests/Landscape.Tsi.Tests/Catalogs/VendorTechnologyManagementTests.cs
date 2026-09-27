using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Domain.Adoption;
using Landscape.Tsi.Domain.Catalogs;
using Landscape.Tsi.Infrastructure.Catalogs;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Tests.Catalogs;

public sealed class VendorTechnologyManagementTests
{
    [Fact]
    public void TecnologiaTsiCatalog_ContainsVendorAndVersionDesplegadaForeignKeys()
    {
        var catalog = MasterCatalogRegistry.GetByCode("tecnologia-tsi");
        Assert.NotNull(catalog);

        var vendorCol = catalog.Columns.FirstOrDefault(c => c.Code == "vendor");
        Assert.NotNull(vendorCol);
        Assert.Equal(CatalogFieldType.ForeignKey, vendorCol.Type);
        Assert.Equal("idVendor", vendorCol.PhysicalName);
        Assert.Equal("vendor", vendorCol.ReferenceCatalogCode);

        var versionCol = catalog.Columns.FirstOrDefault(c => c.Code == "versionDesplegada");
        Assert.NotNull(versionCol);
        Assert.Equal(CatalogFieldType.ForeignKey, versionCol.Type);
        Assert.Equal("idVersionDesplegada", versionCol.PhysicalName);
        Assert.Equal("version-desplegada", versionCol.ReferenceCatalogCode);
    }

    [Fact]
    public void TecnologiaTsiImplementadaCatalog_ContainsVersionDesplegadaForeignKey()
    {
        var catalog = MasterCatalogRegistry.GetByCode("tecnologia-tsi-implementada");
        Assert.NotNull(catalog);

        var versionCol = catalog.Columns.FirstOrDefault(c => c.Code == "versionDesplegada");
        Assert.NotNull(versionCol);
        Assert.Equal(CatalogFieldType.ForeignKey, versionCol.Type);
        Assert.Equal("idVersionDesplegada", versionCol.PhysicalName);
        Assert.Equal("version-desplegada", versionCol.ReferenceCatalogCode);
    }

    [Fact]
    public void MasterCatalogRegistry_ContainsVendorAndVersionDesplegadaRelationships()
    {
        var vendorToTech = MasterCatalogRegistry.Relations.FirstOrDefault(r =>
            r.ParentCatalogCode == "vendor" && r.ChildCatalogCode == "tecnologia-tsi");
        Assert.NotNull(vendorToTech);

        var versionToTech = MasterCatalogRegistry.Relations.FirstOrDefault(r =>
            r.ParentCatalogCode == "version-desplegada" && r.ChildCatalogCode == "tecnologia-tsi");
        Assert.NotNull(versionToTech);

        var versionToImpl = MasterCatalogRegistry.Relations.FirstOrDefault(r =>
            r.ParentCatalogCode == "version-desplegada" && r.ChildCatalogCode == "tecnologia-tsi-implementada");
        Assert.NotNull(versionToImpl);
    }

    [Fact]
    public void CatalogDbContext_MapsVendorAndVersionDesplegadaProperties()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase($"vendor-tech-test-{Guid.NewGuid():N}").Options;
        using var context = new CatalogDbContext(options);

        var techEntityType = context.Model.FindEntityType(typeof(TTecnologiaTSI));
        Assert.NotNull(techEntityType);
        Assert.NotNull(techEntityType.FindProperty("IdVendor"));
        Assert.NotNull(techEntityType.FindProperty("IdVersionDesplegada"));

        var implEntityType = context.Model.FindEntityType(typeof(TTecnologiaTSIimplementadaSubsidiaria));
        Assert.NotNull(implEntityType);
        Assert.NotNull(implEntityType.FindProperty("IdVersionDesplegada"));
        Assert.NotNull(implEntityType.FindProperty("VersionDesplegada"));
    }
}
