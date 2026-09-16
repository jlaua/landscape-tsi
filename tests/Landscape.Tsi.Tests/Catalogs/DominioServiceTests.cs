using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Domain.Catalogs;
using Landscape.Tsi.Infrastructure.Catalogs;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Tests.Catalogs;

public sealed class DominioServiceTests
{
    [Fact]
    public async Task CreateAndUpdate_StoresRealColumnsAndAppendsAudit()
    {
        await using var context = CreateContext();
        var service = new DominioService(context);
        var actorUserId = Guid.NewGuid();

        var id = await service.CreateAsync(
            new DominioCommand("  Identidad  ", "Descripción", "Referencia", "CIBER", "Lineamiento", "CVT", "Ejemplo"),
            actorUserId,
            "catalog-create");
        var created = await service.GetAsync(id);

        Assert.NotNull(created);
        Assert.Equal("Identidad", created.Dominio);
        Assert.Equal("CVT", created.SubDominioCvt);
        var createAudit = Assert.Single(context.AuthorizationAuditEvents);
        Assert.Equal("MasterCatalog.Created", createAudit.EventType);
        Assert.Equal("TMDominio", createAudit.ResourceType);
        Assert.Equal(actorUserId, createAudit.ActorUserId);
        Assert.Equal("Succeeded", createAudit.Result);

        Assert.True(await service.UpdateAsync(
            id,
            new DominioCommand("Identidad actualizada", null, null, null, null, null, null),
            actorUserId,
            "catalog-update"));
        Assert.Equal("Identidad actualizada", (await service.GetAsync(id))!.Dominio);
        Assert.Contains(context.AuthorizationAuditEvents, audit =>
            audit.EventType == "MasterCatalog.Updated" && audit.BeforeJson != null && audit.AfterJson != null);
    }

    [Fact]
    public async Task List_SearchesAndPaginatesWithoutExposingTechnicalKeyAsAFilter()
    {
        await using var context = CreateContext();
        context.Domains.AddRange(
            new TmDominio { Dominio = "Acceso", DescripcionDominio = "Identidad" },
            new TmDominio { Dominio = "Datos", DescripcionDominio = "Protección" },
            new TmDominio { Dominio = "Red", Referencias = "Identidad federada" });
        await context.SaveChangesAsync();
        var service = new DominioService(context);

        var result = await service.ListAsync("Identidad", 1, 10);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.True(item.Id > 0));
    }

    [Fact]
    public void Registry_IsExplicitCompleteAndInternallyConsistent()
    {
        Assert.True(MasterCatalogRegistry.IsAdministrable("dominio"));
        Assert.True(MasterCatalogRegistry.IsAdministrable("building-block"));
        Assert.False(MasterCatalogRegistry.IsAdministrable("TMDominio"));
        Assert.Equal(32, MasterCatalogRegistry.Catalogs.Count);
        Assert.Equal(32, MasterCatalogRegistry.Catalogs.Select(catalog => catalog.Code).Distinct().Count());
        Assert.Equal(32, MasterCatalogRegistry.Catalogs.Select(catalog => catalog.Route).Distinct().Count());
        Assert.All(MasterCatalogRegistry.Catalogs, catalog =>
        {
            Assert.DoesNotContain(catalog.PhysicalTable, catalog.Route, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(catalog.DisplayColumnCode, catalog.Columns.Select(column => column.Code));
            Assert.All(catalog.ListColumnCodes, code => Assert.Contains(code, catalog.Columns.Select(column => column.Code)));
            Assert.All(catalog.Columns.Where(column => column.Type == CatalogFieldType.ForeignKey), column =>
                Assert.NotNull(MasterCatalogRegistry.GetByCode(column.ReferenceCatalogCode!)));
        });
        Assert.All(MasterCatalogRegistry.Relations, relation =>
        {
            Assert.NotNull(MasterCatalogRegistry.GetByCode(relation.ParentCatalogCode));
            Assert.NotNull(MasterCatalogRegistry.GetByCode(relation.ChildCatalogCode));
        });
        Assert.True(MasterCatalogRegistry.Relations.Count >= 12);
        Assert.All(MasterCatalogRegistry.Catalogs, catalog =>
            Assert.True(catalog.Columns.Count > 0));
    }

    private static IdentityDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"dominio-{Guid.NewGuid():N}")
            .Options;
        return new IdentityDbContext(options);
    }
}