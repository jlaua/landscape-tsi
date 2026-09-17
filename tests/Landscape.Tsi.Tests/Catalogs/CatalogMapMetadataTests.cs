using System.Text.Json;

using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Web.Controllers;
using Landscape.Tsi.Web.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Landscape.Tsi.Tests.Catalogs;

public sealed class CatalogMapMetadataTests
{
    [Fact]
    public void EntityPayload_UsesStableCamelCaseContractAndConfiguredBadges()
    {
        var model = new CatalogMapViewModel
        {
            Entities = MasterCatalogRegistry.EntityMetadata,
            Relationships = MasterCatalogRegistry.LogicalRelationships
        };

        using var document = JsonDocument.Parse(model.EntityMetadataJson);
        var entities = document.RootElement.EnumerateArray().ToArray();
        var domain = Assert.Single(entities, entity => entity.GetProperty("id").GetString() == "TMDominio");
        var buildingBlock = Assert.Single(entities, entity => entity.GetProperty("id").GetString() == "TBuildingBlock");
        var operationType = Assert.Single(entities, entity => entity.GetProperty("id").GetString() == "TTipoOperacion");

        Assert.Equal("Dominio", domain.GetProperty("logicalName").GetString());
        Assert.Equal("M", domain.GetProperty("badge").GetString());
        Assert.Equal("T", buildingBlock.GetProperty("badge").GetString());
        Assert.Equal("M", operationType.GetProperty("badge").GetString());
        Assert.True(MasterCatalogRegistry.EntityMetadata.Single(entity => entity.Code == "dominio").IsDeletable);
        Assert.True(MasterCatalogRegistry.EntityMetadata.Single(entity => entity.Code == "building-block").IsDeletable);
        Assert.True(MasterCatalogRegistry.EntityMetadata.Single(entity => entity.Code == "funcionalidad").IsDeletable);
        Assert.True(MasterCatalogRegistry.EntityMetadata.Single(entity => entity.Code == "capacidad-seguridad").IsDeletable);
        Assert.Equal(CatalogEntityType.Master, MasterCatalogRegistry.EntityMetadata.Single(entity => entity.PhysicalTableName == "TModalidadLaboral").EntityType);
        Assert.Equal(CatalogEntityType.Master, MasterCatalogRegistry.EntityMetadata.Single(entity => entity.PhysicalTableName == "TModeloDeOperacion").EntityType);
        Assert.All(entities, entity =>
        {
            Assert.False(string.IsNullOrWhiteSpace(entity.GetProperty("logicalName").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(entity.GetProperty("physicalTableName").GetString()));
        });
        Assert.All(MasterCatalogRegistry.EntityMetadata.Where(entity => entity.IsAdministrable), entity => Assert.False(string.IsNullOrWhiteSpace(entity.Route)));
        Assert.DoesNotContain("?catalogRoute=", MasterCatalogRegistry.EntityMetadata.Single(entity => entity.Code == "building-block").Route, StringComparison.Ordinal);
        Assert.EndsWith("/Administration/MasterTables/building-block", MasterCatalogRegistry.EntityMetadata.Single(entity => entity.Code == "building-block").Route, StringComparison.Ordinal);
        Assert.EndsWith("/Administration/MasterTables/fase-adopcion", MasterCatalogRegistry.EntityMetadata.Single(entity => entity.Code == "fase-adopcion").Route, StringComparison.Ordinal);
        Assert.Equal("MasterTables", MasterCatalogRegistry.EntityMetadata.Single(entity => entity.Code == "building-block").ControllerName);
        Assert.Equal("Catalog", MasterCatalogRegistry.EntityMetadata.Single(entity => entity.Code == "building-block").ListActionName);
        Assert.Equal("Domain", MasterCatalogRegistry.EntityMetadata.Single(entity => entity.Code == "dominio").ListActionName);
    }

    [Fact]
    public void AdministrableCatalogs_UseTheCentralConfiguredBadgeClassification()
    {
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["dominio"] = "M",
            ["building-block"] = "T",
            ["capacidad-seguridad"] = "T",
            ["estado-capacidad"] = "M",
            ["funcionalidad"] = "T",
            ["estado-funcionalidad"] = "M",
            ["fase-adopcion"] = "T",
            ["tecnologia-tsi"] = "T",
            ["familia"] = "M",
            ["casos-uso"] = "T",
            ["empresa-subsidiaria"] = "T",
            ["ciso"] = "T",
            ["postura-roadmap"] = "M",
            ["estado-adopcion-tsi"] = "M",
            ["modalidad-laboral"] = "M",
            ["tipo-operacion"] = "M",
            ["tipo-servicio"] = "M",
            ["actividad-nivel-soporte"] = "M",
            ["modelo-operacion"] = "M",
            ["contrato-tecnologia"] = "T",
            ["proceso-adopcion-tsi"] = "T",
            ["proceso-adopcion-empresa"] = "T",
            ["estandar-tecnologia-historico"] = "T",
            ["tecnologia-tsi-implementada"] = "T",
            ["driver"] = "T",
            ["servicio-tecnologia"] = "T",
            ["tarifario-proyecto-horas"] = "T",
            ["tarifario-operacion"] = "T",
            ["vendor"] = "T",
            ["partner"] = "T",
            ["contacto-vendor"] = "T",
            ["contacto-partner"] = "T",
            ["contacto-empresa"] = "T",
            ["version-desplegada"] = "M"
        };

        var administrable = MasterCatalogRegistry.EntityMetadata
            .Where(entity => entity.IsAdministrable)
            .ToDictionary(entity => entity.Code, StringComparer.Ordinal);

        Assert.Equal(expected.Keys.OrderBy(key => key), administrable.Keys.OrderBy(key => key));
        foreach (var (code, badge) in expected)
        {
            Assert.Equal(badge, administrable[code].Badge);
            Assert.Contains(administrable[code].Badge, new[] { "M", "T" });
        }
    }

    [Fact]
    public void AdministrableCatalogs_UseTheApprovedFunctionalGroups()
    {
        var entities = MasterCatalogRegistry.EntityMetadata.Where(entity => entity.IsAdministrable).ToArray();

        Assert.Equal("Arquitectura de seguridad", entities.Single(entity => entity.PhysicalTableName == "TMFamilia").Group);
        Assert.Equal("Arquitectura de seguridad", entities.Single(entity => entity.PhysicalTableName == "TMPosturaRoadmap").Group);
        Assert.Equal("Arquitectura de seguridad", entities.Single(entity => entity.PhysicalTableName == "TMEstadoAdopcionTSI").Group);

        Assert.Equal(12, entities.Count(entity => entity.Group == "Arquitectura de seguridad"));
        Assert.Equal(4, entities.Count(entity => entity.Group == "Tecnología"));
        Assert.Equal(8, entities.Count(entity => entity.Group == "Organización"));
        Assert.Equal(10, entities.Count(entity => entity.Group == "Operación"));

        Assert.Equal(
            ["Tecnología TSI", "Versión Desplegada", "Casos de Uso", "Tecnología TSI Implementada por Empresa"],
            entities.Where(entity => entity.Group == "Tecnología").Select(entity => entity.LogicalName).ToArray());
    }

    [Fact]
    public void AuthoritativeStateCatalogs_AreReadOnlyAndNeverDeletable()
    {
        var readOnly = new[] { "estado-adopcion-tsi", "fase-adopcion", "estado-capacidad", "estado-funcionalidad" };

        foreach (var code in readOnly)
        {
            var catalog = MasterCatalogRegistry.GetByCode(code)!;
            Assert.True(catalog.IsReadOnly);
            Assert.False(catalog.IsDeletable);
        }

        Assert.All(MasterCatalogRegistry.Catalogs.Where(catalog => !readOnly.Contains(catalog.Code)), catalog => Assert.False(catalog.IsReadOnly));
    }

    [Fact]
    public void CisoList_MinimizesContactPii()
    {
        var ciso = MasterCatalogRegistry.GetByCode("ciso")!;
        Assert.DoesNotContain("email", ciso.ListColumnCodes);
        Assert.DoesNotContain("telefono", ciso.ListColumnCodes);
        Assert.Contains("nombre", ciso.ListColumnCodes);
        Assert.Contains("empresa", ciso.ListColumnCodes);
    }

    [Fact]
    public void CisoDetails_RedactContactPiiWithoutChangingOtherFunctionalValues()
    {
        var ciso = MasterCatalogRegistry.GetByCode("ciso")!;

        Assert.Equal("p***n@example.test", CatalogDisplayPrivacy.Protect(ciso, "email", "person@example.test"));
        Assert.Equal("+51  ••• 99", CatalogDisplayPrivacy.Protect(ciso, "telefono", "+51 999 999 999"));
        Assert.Equal("person@example.test", CatalogDisplayPrivacy.Protect(ciso, "email", "person@example.test", canViewSensitive: true));
        Assert.Equal("Nombre funcional", CatalogDisplayPrivacy.Protect(ciso, "nombre", "Nombre funcional"));
        Assert.Null(CatalogDisplayPrivacy.Protect(ciso, "email", null));
        Assert.Equal("person@example.test", CatalogDisplayPrivacy.PreserveExistingOnBlankUpdate(ciso, "email", null, "person@example.test"));
        Assert.Equal("replacement@example.test", CatalogDisplayPrivacy.PreserveExistingOnBlankUpdate(ciso, "email", "replacement@example.test", "person@example.test"));
    }

    [Fact]
    public void ComplexModalCatalogs_UseResponsiveDrawerPresentation()
    {
        var drawerCatalogs = MasterCatalogRegistry.Catalogs.Where(catalog => catalog.UsesResponsiveDrawer).Select(catalog => catalog.Code).Order().ToArray();

        Assert.Equal(new[] { "capacidad-seguridad", "casos-uso", "ciso", "funcionalidad" }, drawerCatalogs);
        Assert.All(drawerCatalogs, code => Assert.Equal(CatalogEditorMode.Modal, MasterCatalogRegistry.GetByCode(code)!.EditorMode));
        Assert.False(MasterCatalogRegistry.GetByCode("familia")!.UsesResponsiveDrawer);
        Assert.False(MasterCatalogRegistry.GetByCode("postura-roadmap")!.UsesResponsiveDrawer);
        Assert.False(MasterCatalogRegistry.GetByCode("modalidad-laboral")!.UsesResponsiveDrawer);
        Assert.False(MasterCatalogRegistry.GetByCode("tipo-operacion")!.UsesResponsiveDrawer);
    }

    [Fact]
    public void RelationshipPayload_UsesPhysicalStableEndpointsAndCardinalities()
    {
        var model = new CatalogMapViewModel
        {
            Entities = MasterCatalogRegistry.EntityMetadata,
            Relationships = MasterCatalogRegistry.LogicalRelationships
        };

        using var document = JsonDocument.Parse(model.RelationshipMetadataJson);
        var relationship = Assert.Single(document.RootElement.EnumerateArray(), item =>
            item.GetProperty("source").GetString() == "TMDominio" && item.GetProperty("target").GetString() == "TBuildingBlock");

        Assert.Equal("1", relationship.GetProperty("sourceCardinality").GetString());
        Assert.Equal("N", relationship.GetProperty("targetCardinality").GetString());
        Assert.All(document.RootElement.EnumerateArray(), item =>
        {
            Assert.True(item.GetProperty("id").GetString() is not null);
            Assert.True(item.GetProperty("source").GetString() is not null);
            Assert.True(item.GetProperty("target").GetString() is not null);
        });
    }

    [Fact]
    public void MasterTablesController_ExposesTheRoutesUsedByMetadata()
    {
        var catalogAction = typeof(MasterTablesController).GetMethod(nameof(MasterTablesController.Catalog));
        var domainAction = typeof(MasterTablesController).GetMethod(nameof(MasterTablesController.Domain));

        Assert.Equal("{catalogRoute}", Assert.Single(catalogAction!.GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>()).Template);
        Assert.Equal("Domain", Assert.Single(domainAction!.GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>()).Template);
        Assert.All(MasterCatalogRegistry.EntityMetadata.Where(entity => entity.IsAdministrable), entity =>
        {
            Assert.DoesNotContain("?catalogRoute=", entity.Route, StringComparison.Ordinal);
            Assert.StartsWith("/Administration/MasterTables/", entity.Route, StringComparison.Ordinal);
        });
        Assert.Contains(MasterCatalogRegistry.LogicalRelationships, relationship =>
            relationship.FromEntity == "dominio" && relationship.ToEntity == "building-block");
        Assert.Contains(MasterCatalogRegistry.LogicalRelationships, relationship =>
            relationship.FromEntity == "building-block" && relationship.ToEntity == "capacidad-seguridad");
        Assert.Contains(MasterCatalogRegistry.LogicalRelationships, relationship =>
            relationship.FromEntity == "capacidad-seguridad" && relationship.ToEntity == "funcionalidad");
        Assert.Contains(MasterCatalogRegistry.LogicalRelationships, relationship =>
            relationship.FromEntity == "building-block" && relationship.BridgeEntity == "bridge-building-technology");
        Assert.Equal("building-block", MasterCatalogRegistry.GetByCode("building-block")!.Code);
        Assert.True(MasterCatalogRegistry.GetByCode("building-block")!.IsDeletable);
    }

    [Fact]
    public void MasterTableMutationsRequireNamedAuthorizationPolicies()
    {
        var controller = typeof(MasterTablesController);
        foreach (var action in new[] { nameof(MasterTablesController.CreateCatalog), nameof(MasterTablesController.EditCatalog), nameof(MasterTablesController.DeleteCatalog) })
        {
            var method = controller.GetMethod(action)!;
            var policy = Assert.Single(method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true).Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>());
            Assert.Contains(policy.Policy, new[] { "Catalogos.Crear", "Catalogos.Editar", "Catalogos.Eliminar" });
        }
    }

    [Fact]
    public void BuildingTechnologyMapping_UsesTheSingleManyToManyRelationshipMetadata()
    {
        var relationship = Assert.Single(MasterCatalogRegistry.LogicalRelationships,
            item => item.FromEntity == "building-block" && item.ToEntity == "bridge-building-technology");

        Assert.Equal("1", relationship.CardinalityFrom);
        Assert.Equal("N", relationship.CardinalityTo);
        Assert.Equal("bridge-building-technology", relationship.BridgeEntity);
        Assert.Equal("TBuildingBlockVsTTecnologiaTSI", MasterCatalogRegistry.EntityMetadata.Single(entity => entity.Code == relationship.BridgeEntity).PhysicalTableName);
        Assert.Equal("FK_TBuildingBlockVsTTecnologiaTSI", relationship.ForeignKeyName);
        Assert.Equal("Bridge", relationship.RelationshipType);
        Assert.Contains(MasterCatalogRegistry.LogicalRelationships,
            item => item.FromEntity == "bridge-building-technology" && item.ToEntity == "tecnologia-tsi");
    }

    [Fact]
    public void TechnologyMappingController_ExposesStableNonGenericRoutes()
    {
        var controller = typeof(TechnologyMappingController);
        var routes = controller.GetMethods()
            .SelectMany(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), true)
                .Cast<HttpMethodAttribute>()
                .Select(attribute => attribute.Template))
            .Where(template => template is not null)
            .ToArray();

        Assert.Contains("", routes);
        Assert.Contains("Technology/{technologyId:int}/Relations", routes);
        Assert.Contains("BuildingBlock/{buildingBlockId:int}/Relations", routes);
    }
}