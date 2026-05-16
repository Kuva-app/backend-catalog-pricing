using Kuva.CatalogPricing.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kuva.CatalogPricing.EFMigrations;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CatalogPricingDbContext>
{
    public CatalogPricingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogPricingDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CatalogPricingDatabase")
            ?? "Server=localhost,1433;Database=KuvaCatalogPricing;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True";

        optionsBuilder.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsAssembly(typeof(DesignTimeDbContextFactory).Assembly.FullName);
            sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
        });

        return new CatalogPricingDbContext(optionsBuilder.Options);
    }
}
