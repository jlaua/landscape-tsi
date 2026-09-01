using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Landscape.Tsi.Infrastructure.Identity;

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=db-landscape-tsi-dev;Integrated Security=true;TrustServerCertificate=true")
            .Options;

        return new IdentityDbContext(options);
    }
}
