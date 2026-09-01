using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Identity;

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
        var connectionString = configuration.GetConnectionString("IdentityDatabase")
            ?? DevelopmentSqlConnection.FromEnvironment();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            var databaseName = configuration["Identity:InMemoryDatabaseName"] ?? "LandscapeTsiIdentityLocal";
            services.AddDbContext<IdentityDbContext>(options => options.UseInMemoryDatabase(databaseName));
        }
        else
        {
            services.AddDataProtection();
            EnsureDevelopmentDatabase(connectionString);
            services.AddDbContext<IdentityDbContext>(options => options.UseSqlServer(connectionString));
        }

        services.AddIdentityCore<IamUsuario>(options =>
            {
                options.Password.RequiredLength = configuration.GetValue("Authentication:Local:PasswordRequiredLength", 12);
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
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
        services.AddScoped<IAuthenticationAuditWriter, AuthenticationAuditWriter>();
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

    private static void EnsureDevelopmentDatabase(string connectionString)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.InitialCatalog, "db-landscape-tsi-dev", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("IdentityDatabase solo puede apuntar a db-landscape-tsi-dev durante esta implementación.");
        }
    }
}
