using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;

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
public sealed partial class CatalogConcurrencyIntegrationTests
{
    private static readonly string TestUserId = Guid.NewGuid().ToString();

    [Fact]
    public async Task TwoAuthenticatedSessions_WithValidAntiforgery_RejectStaleMvcEdit()
    {
        await using var factory = CreateFactory();
        using var sessionA = CreateSession(factory);
        using var sessionB = CreateSession(factory);

        var tokenA = await GetAntiforgeryTokenAsync(sessionA);
        var tokenB = await GetAntiforgeryTokenAsync(sessionB);

        var committed = await PostEditAsync(sessionB, tokenB, "ITEST_SESSION_B");
        var stale = await PostEditAsync(sessionA, tokenA, "ITEST_SESSION_A");

        Assert.Equal(HttpStatusCode.Redirect, committed.StatusCode);
        Assert.Equal("/Administration/MasterTables/tecnologia-tsi/details/1", committed.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect, stale.StatusCode);
        Assert.Equal("/Administration/MasterTables/tecnologia-tsi/edit/1", stale.Headers.Location?.OriginalString);

        var service = factory.Services.GetRequiredService<ConcurrentCatalogService>();
        Assert.Equal("ITEST_SESSION_B", service.CurrentName);
        Assert.Equal(1, service.UpdateCount);

        var conflictPage = await sessionA.GetStringAsync(stale.Headers.Location);
        Assert.Contains("El registro fue modificado por otro usuario", conflictPage);
    }

    [Fact]
    public async Task EditPost_WithoutAntiforgeryToken_IsRejectedBeforeMutation()
    {
        await using var factory = CreateFactory();
        using var client = CreateSession(factory);

        var response = await client.PostAsync(
            "/Administration/MasterTables/tecnologia-tsi/edit/1",
            EditForm("", "ITEST_WITHOUT_CSRF"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.Services.GetRequiredService<ConcurrentCatalogService>().UpdateCount);
    }

    [Theory]
    [InlineData("noEdit")]
    [InlineData("noScope")]
    public async Task EditPost_WithoutPermissionOrCorporateScope_IsForbiddenWithoutMutation(string denial)
    {
        await using var factory = CreateFactory();
        using var client = CreateSession(factory);
        var token = await GetAntiforgeryTokenAsync(client);

        var response = await client.PostAsync(
            $"/Administration/MasterTables/tecnologia-tsi/edit/1?{denial}=true",
            EditForm(token, $"ITEST_DENIED_{denial}"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, factory.Services.GetRequiredService<ConcurrentCatalogService>().UpdateCount);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var service = new ConcurrentCatalogService();
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:LandscapeTsiDb", string.Empty);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICatalogManagementService>();
                services.AddSingleton(service);
                services.AddSingleton<ICatalogManagementService>(service);
                services.RemoveAll<IEffectiveAccessService>();
                services.AddSingleton<IEffectiveAccessService, TestAccessService>();
                services.AddHttpContextAccessor();
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "ConcurrencyTest";
                    options.DefaultChallengeScheme = "ConcurrencyTest";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("ConcurrencyTest", _ => { });
            });
        });
    }

    private static HttpClient CreateSession(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/Administration/MasterTables/tecnologia-tsi/edit/1");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, "The edit form did not render an antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static Task<HttpResponseMessage> PostEditAsync(HttpClient client, string token, string name) =>
        client.PostAsync("/Administration/MasterTables/tecnologia-tsi/edit/1", EditForm(token, name));

    private static FormUrlEncodedContent EditForm(string antiforgeryToken, string name) => new(
    [
        new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken),
        new KeyValuePair<string, string>("ConcurrencyToken", ConcurrentCatalogService.InitialToken),
        new KeyValuePair<string, string>("Values[nombreCorporativo]", name)
    ]);

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryTokenRegex();

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, TestUserId),
                new(CustomClaimTypes.Permission, Permissions.CatalogView),
                new(CustomClaimTypes.Permission, Permissions.CatalogCreate),
                new(CustomClaimTypes.Permission, Permissions.CatalogDelete)
            };
            if (!Request.Query.ContainsKey("noEdit"))
            {
                claims.Add(new Claim(CustomClaimTypes.Permission, Permissions.CatalogEdit));
            }
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }

    private sealed class TestAccessService(IHttpContextAccessor accessor) : IEffectiveAccessService
    {
        public Task<bool> HasActiveCorporateScopeAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(!accessor.HttpContext!.Request.Query.ContainsKey("noScope"));

        public Task<bool> IsAuthorizedAsync(Guid userId, string permission, int empresaSubsidiariaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class ConcurrentCatalogService : ICatalogManagementService
    {
        private readonly object sync = new();
        private string currentName = "ITEST_ORIGINAL";

        public static string InitialToken => Landscape.Tsi.Web.Models.CatalogConcurrencyToken.Create(CreateRow("ITEST_ORIGINAL"));
        public string CurrentName { get { lock (sync) return currentName; } }
        public int UpdateCount { get; private set; }

        public Task<CatalogRow?> GetAsync(MasterCatalogDefinition definition, int id, CancellationToken cancellationToken = default)
        {
            lock (sync)
            {
                return Task.FromResult<CatalogRow?>(id == 1 ? CreateRow(currentName) : null);
            }
        }

        public Task<bool> UpdateAsync(MasterCatalogDefinition definition, int id, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
        {
            lock (sync)
            {
                currentName = values["nombreCorporativo"]!;
                UpdateCount++;
                return Task.FromResult(true);
            }
        }

        private static CatalogRow CreateRow(string name) => new(
            1,
            new Dictionary<string, object?> { ["nombreCorporativo"] = name },
            new Dictionary<string, string?> { ["nombreCorporativo"] = name });

        public Task<CatalogPageResult> ListAsync(MasterCatalogDefinition definition, string? search, int page, int pageSize, CancellationToken cancellationToken = default, string? sortColumn = null, string? sortDirection = null) => throw new NotSupportedException();
        public Task<CatalogPageResult> ListRelatedAsync(MasterCatalogDefinition definition, CatalogColumnDefinition foreignKey, int parentId, string? search, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CatalogRelationBucket>> GetRelationCountsAsync(MasterCatalogDefinition parent, MasterCatalogDefinition child, CatalogColumnDefinition foreignKey, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> GetRelatedCountAsync(MasterCatalogDefinition child, CatalogColumnDefinition foreignKey, int parentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>>> GetOptionsAsync(MasterCatalogDefinition definition, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>>>(new Dictionary<string, IReadOnlyList<CatalogOption>>());
        public Task<int> CreateAsync(MasterCatalogDefinition definition, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<int, int>> GetVendorTechnologyCountsAsync(IEnumerable<int> vendorIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<int, int>>(new Dictionary<int, int>());
        public Task<IReadOnlyList<VendorTechnologyDto>> GetVendorTechnologiesAsync(int vendorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<VendorTechnologyDto>>([]);
    }
}