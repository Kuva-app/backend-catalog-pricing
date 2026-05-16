using Kuva.CatalogPricing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics.CodeAnalysis;

[assembly: ExcludeFromCodeCoverage]

namespace Kuva.CatalogPricing.Repository;

public sealed class CatalogPricingDbContext(DbContextOptions<CatalogPricingDbContext> options) : DbContext(options)
{
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Sku> Skus => Set<Sku>();
    public DbSet<SkuAttribute> SkuAttributes => Set<SkuAttribute>();
    public DbSet<StoreSkuPrice> StoreSkuPrices => Set<StoreSkuPrice>();
    public DbSet<PriceHistory> PriceHistory => Set<PriceHistory>();
    public DbSet<CatalogAuditLog> CatalogAuditLogs => Set<CatalogAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("pricing");
        ConfigureModel(modelBuilder);
        SeedData(modelBuilder);
    }

    private static void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductCategory>(b =>
        {
            b.ToTable("product_categories");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(120).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(140).IsRequired();
            b.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("IX_product_categories_slug");
        });

        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("products");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(160).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(180).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500);
            b.Property(x => x.ProductType).HasMaxLength(80).IsRequired();
            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            b.HasIndex(x => x.CategoryId).HasDatabaseName("IX_products_category_id");
            b.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("IX_products_slug");
            b.HasIndex(x => x.Status).HasDatabaseName("IX_products_status");
        });

        modelBuilder.Entity<ProductVariant>(b =>
        {
            b.ToTable("product_variants");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(160).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500);
            b.HasIndex(x => x.ProductId).HasDatabaseName("IX_product_variants_product_id");
        });

        modelBuilder.Entity<Sku>(b =>
        {
            b.ToTable("skus");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(80).IsRequired();
            b.Property(x => x.Name).HasMaxLength(160).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500);
            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            b.HasIndex(x => x.ProductId).HasDatabaseName("IX_skus_product_id");
            b.HasIndex(x => x.VariantId).HasDatabaseName("IX_skus_variant_id");
            b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("IX_skus_code");
            b.HasIndex(x => x.Status).HasDatabaseName("IX_skus_status");
        });

        modelBuilder.Entity<SkuAttribute>(b =>
        {
            b.ToTable("sku_attributes");
            b.HasKey(x => x.Id);
            b.Property(x => x.AttributeName).HasMaxLength(80).IsRequired();
            b.Property(x => x.AttributeValue).HasMaxLength(120).IsRequired();
            b.Property(x => x.Unit).HasMaxLength(20);
            b.HasIndex(x => x.SkuId).HasDatabaseName("IX_sku_attributes_sku_id");
        });

        modelBuilder.Entity<StoreSkuPrice>(b =>
        {
            b.ToTable("store_sku_prices");
            b.HasKey(x => x.Id);
            b.Property(x => x.Price).HasPrecision(18, 2);
            b.Property(x => x.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
            b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            b.HasIndex(x => x.StoreId).HasDatabaseName("IX_store_sku_prices_store_id");
            b.HasIndex(x => x.SkuId).HasDatabaseName("IX_store_sku_prices_sku_id");
            b.HasIndex(x => new { x.StoreId, x.SkuId, x.Status, x.ValidFrom, x.ValidTo }).HasDatabaseName("IX_store_sku_prices_store_id_sku_id_status_valid_from_valid_to");
        });

        modelBuilder.Entity<PriceHistory>(b =>
        {
            b.ToTable("price_history");
            b.HasKey(x => x.Id);
            b.Property(x => x.PreviousPrice).HasPrecision(18, 2);
            b.Property(x => x.NewPrice).HasPrecision(18, 2);
            b.Property(x => x.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
            b.Property(x => x.Reason).HasMaxLength(300);
            b.HasIndex(x => new { x.StoreId, x.SkuId, x.ChangedAt }).HasDatabaseName("IX_price_history_store_id_sku_id_changed_at");
        });

        modelBuilder.Entity<CatalogAuditLog>(b =>
        {
            b.ToTable("catalog_audit_logs");
            b.HasKey(x => x.Id);
            b.Property(x => x.Action).HasMaxLength(120).IsRequired();
            b.Property(x => x.ResourceType).HasMaxLength(120).IsRequired();
            b.HasIndex(x => new { x.StoreId, x.CreatedAt }).HasDatabaseName("IX_catalog_audit_logs_store_id_created_at");
            b.HasIndex(x => new { x.ActorId, x.CreatedAt }).HasDatabaseName("IX_catalog_audit_logs_actor_id_created_at");
        });
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var categoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var productId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var now = new DateTimeOffset(2026, 5, 16, 0, 0, 0, TimeSpan.Zero);
        modelBuilder.Entity<ProductCategory>().HasData(new ProductCategory { Id = categoryId, Name = "Impressao fotografica", Slug = "impressao-fotografica", Active = true, CreatedAt = now });
        modelBuilder.Entity<Product>().HasData(new Product { Id = productId, CategoryId = categoryId, Name = "Foto impressa", Slug = "foto-impressa", Description = "Impressao fotografica em papel fotografico", ProductType = CatalogPricingConstants.ProductTypePhotoPrint, Status = ProductStatus.Active, CreatedAt = now });

        var specs = new[] { ("10x15", "10", "15"), ("13x18", "13", "18"), ("15x21", "15", "21") };
        var sort = 0;
        foreach (var (size, width, height) in specs)
        {
            var variantId = Guid.NewGuid();
            modelBuilder.Entity<ProductVariant>().HasData(new ProductVariant { Id = variantId, ProductId = productId, Name = $"Foto {size} cm", SortOrder = ++sort, Active = true, CreatedAt = now });
            foreach (var finish in CatalogPricingConstants.SupportedFinishes)
            {
                var skuId = Guid.NewGuid();
                var upperFinish = finish.ToUpperInvariant();
                modelBuilder.Entity<Sku>().HasData(new Sku { Id = skuId, ProductId = productId, VariantId = variantId, Code = $"FOTO-{size.ToUpperInvariant()}-{upperFinish}", Name = $"Foto {size} cm - {upperFinish}", Status = SkuStatus.Active, SortOrder = sort, CreatedAt = now });
                modelBuilder.Entity<SkuAttribute>().HasData(
                    new SkuAttribute { Id = Guid.NewGuid(), SkuId = skuId, AttributeName = "width", AttributeValue = width, SortOrder = 1, CreatedAt = now },
                    new SkuAttribute { Id = Guid.NewGuid(), SkuId = skuId, AttributeName = "height", AttributeValue = height, SortOrder = 2, CreatedAt = now },
                    new SkuAttribute { Id = Guid.NewGuid(), SkuId = skuId, AttributeName = "unit", AttributeValue = "cm", SortOrder = 3, CreatedAt = now },
                    new SkuAttribute { Id = Guid.NewGuid(), SkuId = skuId, AttributeName = "finish", AttributeValue = finish, SortOrder = 4, CreatedAt = now });
            }
        }
    }
}

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task AddAsync(Product product, CancellationToken cancellationToken);
}

public interface ISkuRepository
{
    Task<Sku?> GetByIdAsync(Guid skuId, CancellationToken cancellationToken);
    Task<Sku?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Sku>> GetActiveSkusByStoreAsync(Guid storeId, DateTimeOffset now, CancellationToken cancellationToken);
    Task AddAsync(Sku sku, CancellationToken cancellationToken);
}

public interface IStoreSkuPriceRepository
{
    Task<StoreSkuPrice?> GetCurrentPriceAsync(Guid storeId, Guid skuId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<StoreSkuPrice>> GetCurrentPricesByStoreAsync(Guid storeId, DateTimeOffset now, CancellationToken cancellationToken);
    Task AddAsync(StoreSkuPrice price, CancellationToken cancellationToken);
    Task UpdateAsync(StoreSkuPrice price, CancellationToken cancellationToken);
}

public interface IPriceHistoryRepository { Task AddAsync(PriceHistory history, CancellationToken cancellationToken); }
public interface IAuditLogRepository { Task AddAsync(CatalogAuditLog auditLog, CancellationToken cancellationToken); }
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
}

public sealed class ProductRepository(CatalogPricingDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken) =>
        db.Products.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);
    public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken) =>
        db.Products.FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);
    public async Task AddAsync(Product product, CancellationToken cancellationToken) => await db.Products.AddAsync(product, cancellationToken);
}

public sealed class SkuRepository(CatalogPricingDbContext db) : ISkuRepository
{
    public Task<Sku?> GetByIdAsync(Guid skuId, CancellationToken cancellationToken) =>
        db.Skus.Include(x => x.Product).Include(x => x.Attributes).FirstOrDefaultAsync(x => x.Id == skuId, cancellationToken);
    public Task<Sku?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        db.Skus.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    public async Task<IReadOnlyCollection<Sku>> GetActiveSkusByStoreAsync(Guid storeId, DateTimeOffset now, CancellationToken cancellationToken) =>
        await db.Skus.Include(x => x.Product).Include(x => x.Variant).Include(x => x.Attributes).Include(x => x.Prices)
            .Where(x => x.Status == SkuStatus.Active && x.Product != null && x.Product.Status == ProductStatus.Active && x.Prices.Any(p => p.StoreId == storeId && p.Status == PriceStatus.Active && p.ValidFrom <= now && (p.ValidTo == null || p.ValidTo > now)))
            .OrderBy(x => x.Product!.Name).ThenBy(x => x.Variant!.SortOrder).ThenBy(x => x.SortOrder).ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
    public async Task AddAsync(Sku sku, CancellationToken cancellationToken) => await db.Skus.AddAsync(sku, cancellationToken);
}

public sealed class StoreSkuPriceRepository(CatalogPricingDbContext db) : IStoreSkuPriceRepository
{
    public Task<StoreSkuPrice?> GetCurrentPriceAsync(Guid storeId, Guid skuId, DateTimeOffset now, CancellationToken cancellationToken) =>
        db.StoreSkuPrices.FirstOrDefaultAsync(x => x.StoreId == storeId && x.SkuId == skuId && x.Status == PriceStatus.Active && x.ValidFrom <= now && (x.ValidTo == null || x.ValidTo > now), cancellationToken);
    public async Task<IReadOnlyCollection<StoreSkuPrice>> GetCurrentPricesByStoreAsync(Guid storeId, DateTimeOffset now, CancellationToken cancellationToken) =>
        await db.StoreSkuPrices.Where(x => x.StoreId == storeId && x.Status == PriceStatus.Active && x.ValidFrom <= now && (x.ValidTo == null || x.ValidTo > now)).ToListAsync(cancellationToken);
    public async Task AddAsync(StoreSkuPrice price, CancellationToken cancellationToken) => await db.StoreSkuPrices.AddAsync(price, cancellationToken);
    public Task UpdateAsync(StoreSkuPrice price, CancellationToken cancellationToken) { db.StoreSkuPrices.Update(price); return Task.CompletedTask; }
}

public sealed class PriceHistoryRepository(CatalogPricingDbContext db) : IPriceHistoryRepository
{
    public async Task AddAsync(PriceHistory history, CancellationToken cancellationToken) => await db.PriceHistory.AddAsync(history, cancellationToken);
}

public sealed class AuditLogRepository(CatalogPricingDbContext db) : IAuditLogRepository
{
    public async Task AddAsync(CatalogAuditLog auditLog, CancellationToken cancellationToken) => await db.CatalogAuditLogs.AddAsync(auditLog, cancellationToken);
}

public sealed class UnitOfWork(CatalogPricingDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await operation(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
