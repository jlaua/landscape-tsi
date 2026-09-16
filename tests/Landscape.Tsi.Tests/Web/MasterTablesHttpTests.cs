using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Infrastructure.Catalogs;
using Landscape.Tsi.Infrastructure.Identity;
using Landscape.Tsi.Web.Models;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Landscape.Tsi.Tests.Web;

[Collection("Web application")]
public sealed class MasterTablesHttpTests
{
    public static TheoryData<string> MutableCatalogRoutes => new()
    {
        "building-block",
        "capacidad-seguridad",
        "funcionalidad",
        "tecnologia-tsi",
        "familia",
        "casos-uso",
        "empresa-subsidiaria",
        "ciso",
        "postura-roadmap",
        "modalidad-laboral",
        "tipo-operacion"
    };

    public static TheoryData<string> AuthoritativeCatalogRoutes => new()
    {
        "estado-adopcion-tsi",
        "fase-adopcion",
        "estado-capacidad",
        "estado-funcionalidad"
    };

    [Fact]
    public async Task TestAuthenticationHandler_LeavesAnonymousRequestsUnauthenticated()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/Administration/MasterTables?anonymous=true")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/Audit?anonymous=true")).StatusCode);
    }

    [Fact]
    public async Task AuthorizedCatalogList_Returns200_AndReadOnlyCreateIsNotAvailable()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var list = await client.GetAsync("/Administration/MasterTables/familia");
        var readOnlyCreate = await client.GetAsync("/Administration/MasterTables/estado-adopcion-tsi/create");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, readOnlyCreate.StatusCode);
    }

    [Fact]
    public async Task MissingPermissionOrScope_IsForbiddenOnProtectedCatalogRoutes()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/Administration/MasterTables/familia?noPermission=true")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/Administration/MasterTables/tecnologia-tsi/create?noCreate=true")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/Administration/MasterTables/tecnologia-tsi/edit/1?noEdit=true")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/Administration/MasterTables/tecnologia-tsi/1/delete-impact?noDelete=true")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Administration/MasterTables/familia/create?noScope=true")).StatusCode);
    }

    [Theory]
    [MemberData(nameof(MutableCatalogRoutes))]
    public async Task EveryMutableCatalog_ExposesListDetailCreateAndEditRoutes(string route)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Administration/MasterTables/{route}?search=ITEST&page=1&pageSize=10")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Administration/MasterTables/{route}/details/1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Administration/MasterTables/{route}/edit/1")).StatusCode);

        var create = await client.GetAsync($"/Administration/MasterTables/{route}/create");
        var definition = MasterCatalogRegistry.GetByRoute(route)!;
        Assert.Equal(definition.EditorMode == CatalogEditorMode.Modal ? HttpStatusCode.Redirect : HttpStatusCode.OK, create.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AuthoritativeCatalogRoutes))]
    public async Task EveryAuthoritativeCatalog_AllowsReadAndRejectsMutationRoutes(string route)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Administration/MasterTables/{route}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Administration/MasterTables/{route}/details/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Administration/MasterTables/{route}/create")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Administration/MasterTables/{route}/edit/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Administration/MasterTables/{route}/1/delete-impact")).StatusCode);
    }

    [Fact]
    public async Task ImplementedTechnologyDetails_RendersTitleAndChildTablesSections()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Administration/MasterTables/tecnologia-tsi-implementada/details/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var decoded = System.Net.WebUtility.HtmlDecode(content);
        Assert.Contains("Tecnología TSI Implementada por Empresa", decoded);
        Assert.Contains("Contratos, Modelo de Operación y Drivers", decoded);
        Assert.Contains("Contratos y Adendas de la Tecnología", decoded);
        Assert.Contains("Modelo de Operación", decoded);
        Assert.Contains("Drivers de Costo y Volumetría", decoded);
    }

    [Fact]
    public async Task Domain_ExposesListDetailAndDeleteImpactWithoutUsingSql()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Administration/MasterTables/Domain?search=ITEST&page=1&pageSize=10")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Administration/MasterTables/Domain/1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Administration/MasterTables/Domain/1/delete-impact")).StatusCode);
    }

    [Fact]
    public async Task BuildingBlockDetails_ReorganizedColumns_ContainsAccessibleHeadersAndCorrectOrder()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/building-block/details/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("aria-sort=\"none\"", html);
        Assert.Contains("Capacidad", html);
        Assert.Contains("Funcionalidad", html);
        Assert.Contains("Estado de funcionalidad", html);
        Assert.Contains("btn-reassign-cap", html);
        Assert.Contains("btn-reassign-func", html);
        Assert.Contains("associate-capability-modal", html);
        Assert.Contains("reassign-capability-modal", html);
        Assert.Contains("associate-functionality-modal", html);
        Assert.Contains("reassign-functionality-modal", html);
        Assert.Contains("assignment-modals.js", html);

        // Verify column order: Capacidad precedes Funcionalidad
        var capIdx = html.IndexOf("data-label=\"Capacidad\"", StringComparison.Ordinal);
        var funcIdx = html.IndexOf("data-label=\"Funcionalidad\"", StringComparison.Ordinal);
        var stateIdx = html.IndexOf("data-label=\"Estado de funcionalidad\"", StringComparison.Ordinal);
        Assert.True(capIdx > 0 && capIdx < funcIdx && funcIdx < stateIdx);
    }

    [Fact]
    public async Task BuildingBlockDetails_EmptyState_RendersDescriptiveEmptyMessage()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/building-block/details/999");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("No existen Funcionalidades asociadas a las capacidades de este Building Block.", html);
    }

    [Fact]
    public async Task BuildingBlockDetails_SortingParameters_RendersAriaSort()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/building-block/details/1?functionalitySortBy=capacity&functionalitySortDirection=desc");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("aria-sort=\"descending\"", html);
    }

    [Fact]
    public async Task CapabilityCandidates_ReturnsJson()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/building-block/1/capability-candidates");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Cap Orfana", json);
        Assert.Contains("Cap De Otro", json);
    }

    [Fact]
    public async Task FunctionalityCandidates_ReturnsJson()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/building-block/1/functionality-candidates");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Func Orfana", json);
    }

    [Fact]
    public async Task CapabilityReassignImpact_ReturnsJson()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/building-block/1/capability-reassign-impact?capabilityId=10&targetBuildingBlockId=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("BB Destino", json);
        Assert.Contains("token_cap_123", json);
    }

    [Fact]
    public async Task FunctionalityReassignImpact_ReturnsJson()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/building-block/1/functionality-reassign-impact?functionalityId=20&targetCapabilityId=30");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"isCrossBuildingBlock\":true", json);
        Assert.Contains("token_func_456", json);
    }

    [Fact]
    public async Task CatalogList_IncludesPageSize100_AndSortLinks_AndRowDeleteButton()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/ciso");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("value=\"100\"", html);
        Assert.Contains("onchange=\"this.form.submit()\"", html);
        Assert.Contains("table-sort-link", html);
        Assert.Contains("sortColumn=nombre", html);
        Assert.Contains("sortColumn=empresa", html);
        Assert.Contains("data-delete-impact-url", html);
        Assert.Contains("data-delete-action-url", html);
        Assert.Contains("delete-catalog-modal", html);
        Assert.Contains("domain-delete-impact.js", html);
    }

    [Theory]
    [InlineData("asc", "ascending", "\u25b2")]
    [InlineData("desc", "descending", "\u25bc")]
    public async Task CatalogList_WithSortingParameters_RendersSortDirectionAndIndicator(string dir, string ariaSort, string indicator)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/Administration/MasterTables/ciso?sortColumn=nombre&sortDirection={dir}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains($"aria-sort=\"{ariaSort}\"", html);
        Assert.Contains(indicator, System.Net.WebUtility.HtmlDecode(html));
    }

    [Fact]
    public async Task CatalogDeleteImpact_ReturnsJson_ForDeletableMasterCatalog()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/ciso/1/delete-impact");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"rootEntity\":\"ciso\"", json);
        Assert.Contains("\"totalRecordsToDelete\":1", json);
    }

    [Fact]
    public async Task CatalogDeleteImpact_ForImplementedTechnology_ReturnsOk()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/tecnologia-tsi-implementada/1/delete-impact");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"rootEntity\":\"tecnologia-tsi-implementada\"", json);
    }

    [Fact]
    public async Task ImplementedTechnologiesCompanyMetrics_ReturnsOk_WithBuckets()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/tecnologia-tsi-implementada/company-metrics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"totalCompanies\":2", json);
        Assert.Contains("\"totalImplementations\":8", json);
        Assert.Contains("\"companyName\":\"BCP\"", json);
    }

    [Fact]
    public async Task ImplementedTechnologiesByCompany_ReturnsOk_WithItems()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/tecnologia-tsi-implementada/companies/1/technologies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"empresa\":\"BCP\"", json);
        Assert.Contains("\"tecnologia\":\"Crowdstrike\"", json);
        Assert.Contains("\"deleteImpactUrl\"", json);
    }

    [Fact]
    public async Task BulkDelete_RequiresValidConfirmation_RedirectsWithError()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var token = await GetAntiforgeryTokenAsync(client);

        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("selectedIds", "1"),
            new KeyValuePair<string, string>("confirmation", "INVALID")
        ]);

        var response = await client.PostAsync("/Administration/MasterTables/tecnologia-tsi-implementada/bulk-delete", content);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Administration/MasterTables/tecnologia-tsi-implementada", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task BulkDelete_Success_DeletesSelectedItems_RedirectsWithSuccess()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var token = await GetAntiforgeryTokenAsync(client);

        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("selectedIds", "1"),
            new KeyValuePair<string, string>("selectedIds", "2"),
            new KeyValuePair<string, string>("confirmation", "ELIMINAR")
        ]);

        var response = await client.PostAsync("/Administration/MasterTables/tecnologia-tsi-implementada/bulk-delete", content);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Administration/MasterTables/tecnologia-tsi-implementada", response.Headers.Location?.ToString());
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path = "/Administration/MasterTables/tecnologia-tsi-implementada")
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        Assert.True(match.Success, "No se encontró el token de antiforgery.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    [Fact]
    public async Task ImplementedTechnologiesCatalogView_RendersBulkToolbarAndDashboard()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/tecnologia-tsi-implementada");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("bulk-actions-bar", html);
        Assert.Contains("select-all-tech", html);
        Assert.Contains("company-technologies-dashboard", html);
        Assert.Contains("company-tech-chart", html);
        Assert.Contains("implemented-tech-admin.js", html);
    }

    [Fact]
    public async Task OptionsEndpoints_ReturnExpectedOptions()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var bbResponse = await client.GetAsync("/Administration/MasterTables/building-block-options");
        Assert.Equal(HttpStatusCode.OK, bbResponse.StatusCode);
        var bbJson = await bbResponse.Content.ReadAsStringAsync();
        Assert.Contains("Building Block 1", bbJson);

        var capResponse = await client.GetAsync("/Administration/MasterTables/capability-options");
        Assert.Equal(HttpStatusCode.OK, capResponse.StatusCode);
        var capJson = await capResponse.Content.ReadAsStringAsync();
        Assert.Contains("Capacidad 10", capJson);
    }

    [Fact]
    public async Task AssociateAndReassign_UnauthorizedWithoutEditPermission_ReturnsForbidden()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["capabilityId"] = "10",
            ["concurrencyToken"] = "token"
        });

        var response = await client.PostAsync("/Administration/MasterTables/building-block/1/associate-capability?noEdit=true", content);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DomainBuildingBlocksCapabilities_ReturnsExpectedJson()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/Domain/1/building-blocks-capacidades");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("items", json);
    }

    [Fact]
    public async Task UpdateBuildingBlockQuickFields_WithoutAntiforgery_ReturnsBadRequest()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["faseAdopcion"] = "2",
            ["rutaEntregable"] = "https://docs.corp/entregable.pdf"
        });

        var response = await client.PostAsync("/Administration/MasterTables/building-block/1/quick-update", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCapabilityForBuildingBlock_WithoutAntiforgery_ReturnsBadRequest()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["nombre"] = "Nueva Capacidad Test",
            ["estado"] = "1",
            ["descripcion"] = "Descripción de prueba"
        });

        var response = await client.PostAsync("/Administration/MasterTables/building-block/1/create-capability", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateFunctionalityForBuildingBlock_WithoutAntiforgery_ReturnsBadRequest()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["capacidadId"] = "10",
            ["nombre"] = "Nueva Funcionalidad Test",
            ["estado"] = "1",
            ["descripcion"] = "Descripción de prueba func"
        });

        var response = await client.PostAsync("/Administration/MasterTables/building-block/1/create-functionality", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdoptionProcessDetails_ReturnsOk_WithChildCatalogSections()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/proceso-adopcion-tsi/details/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("ENTIDADES HIJAS Y GESTIÓN INTEGRAL", html);
        Assert.Contains("Empresas en Proceso de Adopción", html);
        Assert.Contains("Estándares y Tecnologías Asignadas", html);
        Assert.Contains("Servicios de Tecnología Asociados", html);
        Assert.Contains("Tablero de Adopción", html);
    }

    [Fact]
    public async Task CatalogCreateForm_WithReturnUrl_ReturnsOk()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/proceso-adopcion-empresa/create?returnUrl=%2FAdministration%2FMasterTables%2Fproceso-adopcion-tsi%2Fdetails%2F1&initialKey=proceso&initialValue=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("MANTENIMIENTO AMPLIADO", html);
        Assert.Contains("name=\"returnUrl\"", html);
        Assert.Contains("value=\"/Administration/MasterTables/proceso-adopcion-tsi/details/1\"", html);
    }

    [Theory]
    [InlineData("contacto-vendor", "Nombre del contacto")]
    [InlineData("contacto-partner", "Nombre del contacto")]
    public async Task ContactCatalogs_RenderAttributes_InListAndDetails(string route, string mainLabel)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        // 1. Verificar listado
        var listResponse = await client.GetAsync($"/Administration/MasterTables/{route}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var rawListHtml = await listResponse.Content.ReadAsStringAsync();
        var listHtml = WebUtility.HtmlDecode(rawListHtml);
        Assert.Contains(mainLabel, listHtml);
        Assert.Contains("Rol / Cargo", listHtml);
        Assert.Contains("Correo electrónico", listHtml);
        Assert.Contains("Teléfono", listHtml);

        // 2. Verificar detalle
        var detailResponse = await client.GetAsync($"/Administration/MasterTables/{route}/details/1");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var rawDetailHtml = await detailResponse.Content.ReadAsStringAsync();
        var detailHtml = WebUtility.HtmlDecode(rawDetailHtml);
        Assert.Contains("Rol / Cargo", detailHtml);
        Assert.Contains("Otro", detailHtml);
        Assert.Contains("Notas", detailHtml);

        // 3. Modal de creación en la vista de lista
        Assert.Contains("field-rol", listHtml);
        Assert.Contains("field-otro", listHtml);
        Assert.Contains("field-notas", listHtml);
    }

    [Fact]
    public async Task VendorCatalog_RendersAttributes_InListAndDetails()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        // 1. Verificar listado de Vendor
        var listResponse = await client.GetAsync("/Administration/MasterTables/vendor");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var rawListHtml = await listResponse.Content.ReadAsStringAsync();
        var listHtml = WebUtility.HtmlDecode(rawListHtml);

        Assert.Contains("Nombre del vendor", listHtml);
        Assert.Contains("Descripción del vendor", listHtml);
        Assert.Contains("Tecnologías TSI", listHtml);
        Assert.Contains("vendor-tech-count-btn", listHtml);
        Assert.Contains("vendor-technologies-modal", listHtml);
        Assert.Contains("vendor-admin.js", listHtml);

        // 2. Verificar detalle de Vendor
        var detailResponse = await client.GetAsync("/Administration/MasterTables/vendor/details/1");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var rawDetailHtml = await detailResponse.Content.ReadAsStringAsync();
        var detailHtml = WebUtility.HtmlDecode(rawDetailHtml);

        Assert.Contains("Descripción del vendor", detailHtml);
        Assert.Contains("Tecnologías TSI Asociadas", detailHtml);
        Assert.Contains("Crowdstrike Falcon", detailHtml);
        Assert.Contains("Fortinet FortiGate", detailHtml);
    }

    [Fact]
    public async Task VendorTechnologiesEndpoint_ReturnsJsonWithTechnologies()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/vendor/1/technologies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(1, json.GetProperty("vendorId").GetInt32());
        Assert.Equal(2, json.GetProperty("totalCount").GetInt32());

        var items = json.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal("Crowdstrike Falcon", items[0].GetProperty("nombreCorporativo").GetString());
        Assert.Equal("Fortinet FortiGate", items[1].GetProperty("nombreCorporativo").GetString());
    }

    [Fact]
    public async Task PartnerCatalog_RendersAttributes_InListAndDetails()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        // 1. Verificar listado de Partner
        var listResponse = await client.GetAsync("/Administration/MasterTables/partner");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var rawListHtml = await listResponse.Content.ReadAsStringAsync();
        var listHtml = WebUtility.HtmlDecode(rawListHtml);

        Assert.Contains("Nombre del partner", listHtml);
        Assert.Contains("Descripción del partner", listHtml);
        Assert.Contains("Vendor", listHtml);
        Assert.Contains("Tecnología TSI", listHtml);

        // 2. Verificar detalle de Partner
        var detailResponse = await client.GetAsync("/Administration/MasterTables/partner/details/1");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var rawDetailHtml = await detailResponse.Content.ReadAsStringAsync();
        var detailHtml = WebUtility.HtmlDecode(rawDetailHtml);

        Assert.Contains("Nombre del partner", detailHtml);
        Assert.Contains("Descripción del partner", detailHtml);

        // 3. Verificar que aparece en el index de tablas maestras
        var indexResponse = await client.GetAsync("/Administration/MasterTables");
        Assert.Equal(HttpStatusCode.OK, indexResponse.StatusCode);
        var indexHtml = WebUtility.HtmlDecode(await indexResponse.Content.ReadAsStringAsync());
        Assert.Contains("Partner", indexHtml);
    }

    [Fact]
    public async Task VendorCatalog_RendersContactsHeaderAndModalTrigger()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/vendor");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawHtml = await response.Content.ReadAsStringAsync();
        var html = WebUtility.HtmlDecode(rawHtml);

        Assert.Contains("Contactos", html);
        Assert.Contains("data-bs-target=\"#contacts-modal\"", html);
        Assert.Contains("data-entity-type=\"vendor\"", html);
        Assert.Contains("id=\"contacts-modal\"", html);
        Assert.Contains("vendor-partner-contacts-admin.js", html);
    }

    [Fact]
    public async Task PartnerCatalog_RendersContactsHeaderAndModalTrigger()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/partner");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawHtml = await response.Content.ReadAsStringAsync();
        var html = WebUtility.HtmlDecode(rawHtml);

        Assert.Contains("Contactos", html);
        Assert.Contains("data-bs-target=\"#contacts-modal\"", html);
        Assert.Contains("data-entity-type=\"partner\"", html);
        Assert.Contains("id=\"contacts-modal\"", html);
        Assert.Contains("vendor-partner-contacts-admin.js", html);
    }

    [Fact]
    public async Task VendorContactsEndpoint_ReturnsJsonWithContacts()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/vendor/1/contacts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(1, json.GetProperty("entityId").GetInt32());
        Assert.Equal("vendor", json.GetProperty("entityType").GetString());
        Assert.True(json.TryGetProperty("items", out var items));
        Assert.Equal(System.Text.Json.JsonValueKind.Array, items.ValueKind);
    }

    [Fact]
    public async Task PartnerContactsEndpoint_ReturnsJsonWithContacts()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Administration/MasterTables/partner/1/contacts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(1, json.GetProperty("entityId").GetInt32());
        Assert.Equal("partner", json.GetProperty("entityType").GetString());
        Assert.True(json.TryGetProperty("items", out var items));
        Assert.Equal(System.Text.Json.JsonValueKind.Array, items.ValueKind);
    }

    [Fact]
    public async Task VendorContacts_CreateEditDelete_ApiWorkflow()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        // 1. Create a contact for Vendor 1
        var createDto = new SaveMasterContactDto
        {
            Nombre = "Contacto Test Vendor",
            Rol = "Especialista TSI",
            Email = "test@vendor.com",
            Telefono = "999888777",
            Notas = "Nota de prueba"
        };
        var createResponse = await client.PostAsJsonAsync("/Administration/MasterTables/vendor/1/contacts", createDto);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var createResult = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(createResult.GetProperty("success").GetBoolean());
        var contactId = createResult.GetProperty("id").GetInt32();

        // 2. Query contacts list
        var listResponse = await client.GetAsync("/Administration/MasterTables/vendor/1/contacts");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listJson = await listResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var items = listJson.GetProperty("items").EnumerateArray().ToList();
        Assert.Contains(items, item => item.GetProperty("nombre").GetString() == "Contacto Test Vendor");

        // 3. Edit contact
        var editDto = new SaveMasterContactDto
        {
            Nombre = "Contacto Actualizado",
            Rol = "Senior TSI Lead",
            Email = "lead@vendor.com"
        };
        var editResponse = await client.PostAsJsonAsync($"/Administration/MasterTables/vendor/1/contacts/{contactId}/edit", editDto);
        Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);

        // 4. Delete contact
        var deleteResponse = await client.PostAsync($"/Administration/MasterTables/vendor/1/contacts/{contactId}/delete", null);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task PartnerContacts_CreateEditDelete_ApiWorkflow()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        // 1. Create contact for Partner 1
        var createDto = new SaveMasterContactDto
        {
            Nombre = "Contacto Test Partner",
            Rol = "Account Manager",
            Email = "partner@canal.com",
            Telefono = "123456789"
        };
        var createResponse = await client.PostAsJsonAsync("/Administration/MasterTables/partner/1/contacts", createDto);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var createResult = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(createResult.GetProperty("success").GetBoolean());
        var contactId = createResult.GetProperty("id").GetInt32();

        // 2. Query contacts list
        var listResponse = await client.GetAsync("/Administration/MasterTables/partner/1/contacts");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listJson = await listResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var items = listJson.GetProperty("items").EnumerateArray().ToList();
        Assert.Contains(items, item => item.GetProperty("nombre").GetString() == "Contacto Test Partner");

        // 3. Edit contact
        var editDto = new SaveMasterContactDto
        {
            Nombre = "Partner Lead Actualizado",
            Rol = "Director"
        };
        var editResponse = await client.PostAsJsonAsync($"/Administration/MasterTables/partner/1/contacts/{contactId}/edit", editDto);
        Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);

        // 4. Delete contact
        var deleteResponse = await client.PostAsync($"/Administration/MasterTables/partner/1/contacts/{contactId}/delete", null);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }


    private static WebApplicationFactory<Program> CreateFactory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:LandscapeTsiDb", string.Empty);
        var inMemoryDbName = $"catalog-http-test-{Guid.NewGuid():N}";
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<CatalogDbContext>();
            services.AddDbContext<CatalogDbContext>(opt => opt.UseInMemoryDatabase(inMemoryDbName));
            services.RemoveAll<ICatalogManagementService>();
            services.AddSingleton<ICatalogManagementService, StubCatalogService>();
            services.RemoveAll<IDominioService>();
            services.AddSingleton<IDominioService, StubDominioService>();
            services.RemoveAll<IDeletionImpactService>();
            services.AddSingleton<IDeletionImpactService, StubDeletionImpactService>();
            services.RemoveAll<IBuildingBlockRelatedService>();
            services.AddSingleton<IBuildingBlockRelatedService, StubBuildingBlockRelatedService>();
            services.RemoveAll<IBuildingBlockTechnologyMappingService>();
            services.AddSingleton<IBuildingBlockTechnologyMappingService, StubTechnologyMappingService>();
            services.RemoveAll<IAssociationImpactService>();
            services.AddSingleton<IAssociationImpactService, StubAssociationImpactService>();
            services.RemoveAll<IAssignmentService>();
            services.AddSingleton<IAssignmentService, StubAssignmentService>();
            services.RemoveAll<IEffectiveAccessService>();
            services.AddSingleton<IEffectiveAccessService, StubAccessService>();
            services.AddHttpContextAccessor();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "MasterTablesTest";
                options.DefaultChallengeScheme = "MasterTablesTest";
            }).AddScheme<AuthenticationSchemeOptions, MasterTablesAuthHandler>("MasterTablesTest", _ => { });
        });
    });

    private static readonly string TestUserId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

    private sealed class MasterTablesAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Request.Query.ContainsKey("anonymous")) return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, TestUserId) };
            if (!Request.Query.ContainsKey("noPermission")) claims.Add(new Claim(CustomClaimTypes.Permission, Permissions.CatalogView));
            if (!Request.Query.ContainsKey("noPermission") && !Request.Query.ContainsKey("noCreate")) claims.Add(new Claim(CustomClaimTypes.Permission, Permissions.CatalogCreate));
            if (!Request.Query.ContainsKey("noPermission") && !Request.Query.ContainsKey("noEdit")) claims.Add(new Claim(CustomClaimTypes.Permission, Permissions.CatalogEdit));
            if (!Request.Query.ContainsKey("noPermission") && !Request.Query.ContainsKey("noDelete")) claims.Add(new Claim(CustomClaimTypes.Permission, Permissions.CatalogDelete));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }

    private sealed class StubAccessService(IHttpContextAccessor httpContextAccessor) : IEffectiveAccessService
    {
        public Task<bool> HasActiveCorporateScopeAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(!httpContextAccessor.HttpContext!.Request.Query.ContainsKey("noScope"));
        public Task<bool> IsAuthorizedAsync(Guid userId, string permission, int empresaSubsidiariaId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class StubCatalogService : ICatalogManagementService
    {
        public static string CurrentName { get; private set; } = "Familia de prueba";
        private static CatalogRow Row => new(1, new Dictionary<string, object?> { ["nombre"] = CurrentName, ["nombreCorporativo"] = CurrentName, ["nombreBuildingBlock"] = CurrentName, ["nombreVendor"] = "Vendor Test", ["descripcionVendor"] = "Descripción de prueba", ["tecnologia"] = "Crowdstrike", ["nombrePartner"] = "Partner Test", ["descripcionPartner"] = "Descripción del partner", ["vendor"] = "Vendor Test" }, new Dictionary<string, string?> { ["nombre"] = CurrentName, ["nombreCorporativo"] = CurrentName, ["nombreBuildingBlock"] = CurrentName, ["nombreVendor"] = "Vendor Test", ["descripcionVendor"] = "Descripción de prueba", ["tecnologia"] = "Crowdstrike", ["nombrePartner"] = "Partner Test", ["descripcionPartner"] = "Descripción del partner", ["vendor"] = "Vendor Test" });
        public Task<CatalogPageResult> ListAsync(MasterCatalogDefinition definition, string? search, int page, int pageSize, CancellationToken cancellationToken = default, string? sortColumn = null, string? sortDirection = null) => Task.FromResult(new CatalogPageResult([Row], 1, pageSize, 1));
        public Task<CatalogPageResult> ListRelatedAsync(MasterCatalogDefinition definition, CatalogColumnDefinition foreignKey, int parentId, string? search, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CatalogPageResult([new CatalogRow(1, new Dictionary<string, object?> { ["empresa"] = "BCP", ["tecnologia"] = "Crowdstrike", ["buildingBlock"] = "EDR", ["versionDesplegada"] = "v1" }, new Dictionary<string, string?> { ["empresa"] = "BCP", ["tecnologia"] = "Crowdstrike", ["buildingBlock"] = "EDR", ["versionDesplegada"] = "v1" })], 1, pageSize, 1));
        public Task<IReadOnlyList<CatalogRelationBucket>> GetRelationCountsAsync(MasterCatalogDefinition parent, MasterCatalogDefinition child, CatalogColumnDefinition foreignKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogRelationBucket>>([new CatalogRelationBucket(1, "BCP", 5), new CatalogRelationBucket(2, "Mibanco", 3)]);
        public Task<int> GetRelatedCountAsync(MasterCatalogDefinition child, CatalogColumnDefinition foreignKey, int parentId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<CatalogRow?> GetAsync(MasterCatalogDefinition definition, int id, CancellationToken cancellationToken = default) => Task.FromResult<CatalogRow?>(id is 1 or 999 ? Row : null);
        public Task<IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>>> GetOptionsAsync(MasterCatalogDefinition definition, CancellationToken cancellationToken = default)
        {
            var dict = new Dictionary<string, IReadOnlyList<CatalogOption>>
            {
                ["idBuildingBlock"] = [new CatalogOption(1, "Building Block 1"), new CatalogOption(2, "Building Block 2")],
                ["idCapacidad"] = [new CatalogOption(10, "Capacidad 10"), new CatalogOption(20, "Capacidad 20")]
            };
            return Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>>>(dict);
        }
        public Task<int> CreateAsync(MasterCatalogDefinition definition, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<bool> UpdateAsync(MasterCatalogDefinition definition, int id, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
        {
            if (values.TryGetValue("nombreCorporativo", out var name) && !string.IsNullOrWhiteSpace(name)) CurrentName = name;
            return Task.FromResult(true);
        }
        public Task<IReadOnlyDictionary<int, int>> GetVendorTechnologyCountsAsync(IEnumerable<int> vendorIds, CancellationToken cancellationToken = default)
        {
            var dict = vendorIds.ToDictionary(id => id, id => 2);
            return Task.FromResult<IReadOnlyDictionary<int, int>>(dict);
        }
        public Task<IReadOnlyList<VendorTechnologyDto>> GetVendorTechnologiesAsync(int vendorId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<VendorTechnologyDto> list =
            [
                new(1, "Crowdstrike Falcon", "Crowdstrike Local", "EDR", "Estratégica", "Suscripción anual", "Cloud"),
                new(2, "Fortinet FortiGate", "Fortinet FW", "Firewall", "Transición", "Perpetuo", "On-Premise")
            ];
            return Task.FromResult(list);
        }
        public Task<IReadOnlyDictionary<int, int>> GetVendorContactCountsAsync(IEnumerable<int> vendorIds, CancellationToken cancellationToken = default)
        {
            var dict = vendorIds.ToDictionary(id => id, _ => 3);
            return Task.FromResult<IReadOnlyDictionary<int, int>>(dict);
        }
        public Task<IReadOnlyDictionary<int, int>> GetPartnerContactCountsAsync(IEnumerable<int> partnerIds, CancellationToken cancellationToken = default)
        {
            var dict = partnerIds.ToDictionary(id => id, _ => 4);
            return Task.FromResult<IReadOnlyDictionary<int, int>>(dict);
        }
    }

    private sealed class StubDominioService : IDominioService
    {
        private static readonly DominioRecord Record = new(1, "ITEST_DOMINIO", "Descripción", null, null, null, null, null);
        public Task<PagedResult<DominioRecord>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(new PagedResult<DominioRecord>([Record], page, pageSize, 1));
        public Task<DominioRecord?> GetAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<DominioRecord?>(id == 1 ? Record : null);
        public Task<int> CreateAsync(DominioCommand command, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<bool> UpdateAsync(int id, DominioCommand command, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => Task.FromResult(id == 1);
    }

    private sealed class StubDeletionImpactService : IDeletionImpactService
    {
        private static readonly DeletionDependencyNode Root = new("Registro", "TRegistro", 1, 0, "Raíz", []);
        private static readonly DeletionImpactResult Impact = new("dominio", "TMDominio", 1, "ITEST", [], [], 0, 1, true, null, Root, false);
        public Task<DeletionImpactResult?> PreviewAsync(string entityCode, int rootId, CancellationToken cancellationToken = default) => Task.FromResult<DeletionImpactResult?>(rootId == 1 ? Impact with { RootEntity = entityCode } : null);
        public Task<DeletionExecutionResult> DeleteAsync(string entityCode, int rootId, string? confirmation, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => Task.FromResult(new DeletionExecutionResult(true, 1));
    }

    private sealed class StubBuildingBlockRelatedService : IBuildingBlockRelatedService
    {
        private static readonly CatalogPageResult Empty = new([], 1, 10, 0);
        public Task<BuildingBlockRelatedResult> GetAsync(
            int buildingBlockId,
            string? capabilitySearch,
            string? functionalitySearch,
            string? technologySearch,
            int capabilityPage,
            int functionalityPage,
            int technologyPage,
            int pageSize,
            string? functionalitySortBy = null,
            string? functionalitySortDirection = null,
            CancellationToken cancellationToken = default)
        {
            if (buildingBlockId == 999)
                return Task.FromResult(new BuildingBlockRelatedResult(Empty, Empty, Empty));

            var capRow = new CatalogRow(10, new Dictionary<string, object?> { ["capacidad"] = "Capacidad Alfa", ["estado"] = "Activo", ["descripcion"] = "Desc" }, new Dictionary<string, string?> { ["capacidad"] = "Capacidad Alfa", ["estado"] = "Activo", ["descripcion"] = "Desc" });
            var funcRow = new CatalogRow(20, new Dictionary<string, object?> { ["capacidad"] = "Capacidad Alfa", ["funcionalidad"] = "Funcionalidad Beta", ["estado"] = "Activo", ["idCapacidad"] = 10 }, new Dictionary<string, string?> { ["capacidad"] = "Capacidad Alfa", ["funcionalidad"] = "Funcionalidad Beta", ["estado"] = "Activo", ["idCapacidad"] = "10" });
            return Task.FromResult(new BuildingBlockRelatedResult(
                new CatalogPageResult([capRow], 1, 10, 1),
                new CatalogPageResult([funcRow], 1, 10, 1),
                Empty));
        }
    }

    private sealed class StubAssociationImpactService : IAssociationImpactService
    {
        public Task<CapabilityReassignmentImpactDto?> PreviewCapabilityReassignmentAsync(int capabilityId, int targetBuildingBlockId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CapabilityReassignmentImpactDto?>(new CapabilityReassignmentImpactDto(
                capabilityId, "Capacidad Alfa", 1, "BB Origen", targetBuildingBlockId, "BB Destino", 2, ["Func1", "Func2"], ["Tech1"], [], "token_cap_123"));

        public Task<FunctionalityReassignmentImpactDto?> PreviewFunctionalityReassignmentAsync(int functionalityId, int targetCapabilityId, CancellationToken cancellationToken = default) =>
            Task.FromResult<FunctionalityReassignmentImpactDto?>(new FunctionalityReassignmentImpactDto(
                functionalityId, "Funcionalidad Beta", 10, "Cap Origen", 1, "BB Origen", targetCapabilityId, "Cap Destino", 2, "BB Destino", true, [], "token_func_456"));

        public Task<IReadOnlyList<CapabilityCandidateDto>> GetCapabilityCandidatesAsync(int buildingBlockId, string? search = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CapabilityCandidateDto>>([
                new CapabilityCandidateDto(100, "Cap Orfana", "Activo", null, null, HierarchyAssignmentStatus.Unassigned),
                new CapabilityCandidateDto(101, "Cap De Otro", "Activo", 99, "Otro BB", HierarchyAssignmentStatus.AssignedToOther)
            ]);

        public Task<IReadOnlyList<FunctionalityCandidateDto>> GetFunctionalityCandidatesAsync(int buildingBlockId, int? capabilityId = null, string? search = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FunctionalityCandidateDto>>([
                new FunctionalityCandidateDto(200, "Func Orfana", "Activo", null, null, null, null, HierarchyAssignmentStatus.Unassigned)
            ]);

        public Task<string?> GetCapabilityConcurrencyTokenAsync(int capabilityId, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>("token_cap");

        public Task<string?> GetFunctionalityConcurrencyTokenAsync(int functionalityId, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>("token_func");
    }

    private sealed class StubAssignmentService : IAssignmentService
    {
        public Task<AssignmentResult> AssignCapabilityAsync(AssignCapabilityCommand command, CancellationToken cancellationToken = default)
        {
            if (command.ConcurrencyToken == "conflict")
                return Task.FromResult(AssignmentResult.ConcurrencyConflict("Conflicto"));
            return Task.FromResult(AssignmentResult.Success());
        }

        public Task<AssignmentResult> ReassignCapabilityAsync(ReassignCapabilityCommand command, CancellationToken cancellationToken = default)
        {
            if (command.ConcurrencyToken == "conflict")
                return Task.FromResult(AssignmentResult.ConcurrencyConflict("Conflicto"));
            return Task.FromResult(AssignmentResult.Success());
        }

        public Task<AssignmentResult> AssignFunctionalityAsync(AssignFunctionalityCommand command, CancellationToken cancellationToken = default)
        {
            if (command.ConcurrencyToken == "conflict")
                return Task.FromResult(AssignmentResult.ConcurrencyConflict("Conflicto"));
            return Task.FromResult(AssignmentResult.Success());
        }

        public Task<AssignmentResult> ReassignFunctionalityAsync(ReassignFunctionalityCommand command, CancellationToken cancellationToken = default)
        {
            if (command.ConcurrencyToken == "conflict")
                return Task.FromResult(AssignmentResult.ConcurrencyConflict("Conflicto"));
            return Task.FromResult(AssignmentResult.Success());
        }
    }

    private sealed class StubTechnologyMappingService : IBuildingBlockTechnologyMappingService
    {
        public Task<BuildingTechnologyRelationResult?> GetBuildingBlockRelationsAsync(int buildingBlockId, string? search = null, int? familyId = null, CancellationToken cancellationToken = default) => Task.FromResult<BuildingTechnologyRelationResult?>(null);
        public Task<TechnologyRelationResult?> GetTechnologyRelationsAsync(int technologyId, CancellationToken cancellationToken = default) => Task.FromResult<TechnologyRelationResult?>(null);
        public Task<TechnologyMappingPage> ListAsync(TechnologyMappingQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UnassignedTechnologyPage> ListUnassignedAsync(UnassignedTechnologyQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<TechnologyOption>> GetTechnologyOptionsAsync(int buildingBlockId, string? search = null, int? familyId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FamilyBuildingBlockReport> GetFamilyBuildingBlockReportAsync(int? familyId = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveTechnologyRelationsAsync(int technologyId, IReadOnlyCollection<int> buildingBlockIds, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveBuildingBlockRelationsAsync(int buildingBlockId, IReadOnlyCollection<int> technologyIds, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AssociateAsync(int buildingBlockId, int technologyId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DisassociateAsync(int buildingBlockId, int technologyId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
