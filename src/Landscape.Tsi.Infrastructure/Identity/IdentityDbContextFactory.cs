using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Landscape.Tsi.Infrastructure.Identity;

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";
        var webProjectPath = FindWebProjectPath();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(webProjectPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets<IdentityDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("LandscapeTsiDb")
            ?? throw new InvalidOperationException(
                "Configure ConnectionStrings:LandscapeTsiDb para usar IdentityDbContext en tiempo de diseño.");

        DatabaseSafetyValidator.Validate(connectionString, configuration);
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new IdentityDbContext(options);
    }

    private static string FindWebProjectPath()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Landscape.Tsi.Web");
            if (File.Exists(Path.Combine(candidate, "Landscape.Tsi.Web.csproj")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("No se encontró el proyecto Landscape.Tsi.Web para cargar su configuración.");
    }

}
