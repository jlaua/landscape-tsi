using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Application.Reporting;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Catalogs;
using Landscape.Tsi.Infrastructure.Identity;
using Landscape.Tsi.Infrastructure.Reporting;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Landscape.Tsi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LandscapeTsiDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            var databaseName = configuration["Identity:InMemoryDatabaseName"] ?? "LandscapeTsiIdentityLocal";
            services.AddDbContext<IdentityDbContext>(options => options.UseInMemoryDatabase(databaseName));
        }
        else
        {
            services.AddDataProtection();
            DatabaseSafetyValidator.Validate(connectionString, configuration);
            services.AddDbContext<IdentityDbContext>(options => options.UseSqlServer(connectionString));
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var catalogDatabaseName = configuration["Identity:InMemoryDatabaseName"] ?? "LandscapeTsiIdentityLocal";
            services.AddDbContext<CatalogDbContext>(options => options.UseInMemoryDatabase(catalogDatabaseName));
        }
        else
        {
            services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer(connectionString));
        }

        services.AddIdentityCore<IamUsuario>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.MaxFailedAccessAttempts = configuration.GetValue("Authentication:Local:MaxFailedAccessAttempts", 5);
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(configuration.GetValue("Authentication:Local:LockoutMinutes", 15));
                options.User.RequireUniqueEmail = false;
            })
            .AddSignInManager()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddClaimsPrincipalFactory<LandscapeClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        services.Configure<BootstrapAdminOptions>(configuration.GetSection(BootstrapAdminOptions.SectionName));
        services.AddScoped<IIdentityAccessService, IdentityAccessService>();
        services.AddScoped<IExternalIdentityService, ExternalIdentityService>();
        services.AddScoped<IEffectiveAccessService, EffectiveAccessService>();
        services.AddSingleton<IResourcePolicyEvaluator, ResourcePolicyEvaluator>();
        services.AddScoped<ISeparationOfDutiesEvaluator, SeparationOfDutiesEvaluator>();
        services.AddScoped<IBreakGlassService, BreakGlassService>();
        services.AddScoped<IIdentityUserAdministration, IdentityUserAdministration>();
        services.AddScoped<IRoleStore<IamRol>, LandscapeRoleStore>();
        services.AddScoped<RoleManager<IamRol>>();
        services.AddScoped<ILocalUserAdministration, LocalUserAdministration>();
        services.AddScoped<IUserRoleAssignmentService, UserRoleAssignmentService>();
        services.AddScoped<IAuthorizationAuditQuery, AuthorizationAuditQuery>();
        services.AddScoped<IIdentityAdministrationOverview, IdentityAdministrationOverviewService>();
        services.AddScoped<IOrganizationScopeAdministration, OrganizationScopeAdministrationService>();
        services.AddScoped<IAuthenticationAuditWriter, AuthenticationAuditWriter>();
        services.AddScoped<IAuditTrailService, AuditTrailService>();
        services.AddScoped<IAuditRestoreService, AuditRestoreService>();
        services.AddScoped<IDominioService, DominioService>();
        services.AddScoped<ICatalogManagementService, CatalogManagementService>();
        services.AddScoped<IBuildingBlockRelatedService, BuildingBlockRelatedService>();
        services.AddScoped<IBuildingBlockTechnologyMappingService, BuildingBlockTechnologyMappingService>();
        services.AddScoped<IDeletionImpactService, DeletionImpactService>();
        services.AddScoped<IReportingService, CatalogReportingService>();
        services.AddScoped<LandscapeCookieAuthenticationEvents>();
        services.AddScoped<BootstrapAdminInitializer>();
        return services;
    }

    public static async Task InitializeIdentityInfrastructureAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        if (dbContext.Database.ProviderName?.Contains("InMemory", StringComparison.Ordinal) == true)
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        await scope.ServiceProvider.GetRequiredService<BootstrapAdminInitializer>().InitializeAsync();
    }

}