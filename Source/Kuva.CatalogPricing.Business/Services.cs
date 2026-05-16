using Kuva.CatalogPricing.Entities;
using Kuva.CatalogPricing.Repository;
using Microsoft.EntityFrameworkCore;

namespace Kuva.CatalogPricing.Business;

public interface ICatalogQueryService
{
    Task<StoreCatalogResponse> GetStoreCatalogAsync(Guid storeId, CancellationToken cancellationToken);
    Task<SkuDetailResponse> GetSkuAsync(Guid storeId, Guid skuId, CancellationToken cancellationToken);
}

public interface IQuoteService { Task<QuoteResponse> QuoteAsync(Guid storeId, QuoteRequest request, CancellationToken cancellationToken); }
public interface IPriceService
{
    Task<StoreSkuPriceResponse> GetPriceAsync(Guid storeId, Guid skuId, CancellationToken cancellationToken);
    Task<StoreSkuPriceResponse> UpsertPriceAsync(Guid storeId, Guid skuId, UpsertPriceRequest request, UserContext user, CancellationToken cancellationToken);
    Task ActivatePriceAsync(Guid storeId, Guid skuId, UserContext user, CancellationToken cancellationToken);
    Task DeactivatePriceAsync(Guid storeId, Guid skuId, UserContext user, CancellationToken cancellationToken);
}

public interface ISkuManagementService
{
    Task<SkuResponse> CreateSkuAsync(CreateSkuRequest request, UserContext user, CancellationToken cancellationToken);
    Task<SkuResponse> UpdateSkuAsync(Guid skuId, UpdateSkuRequest request, UserContext user, CancellationToken cancellationToken);
    Task ActivateSkuAsync(Guid skuId, UserContext user, CancellationToken cancellationToken);
    Task DeactivateSkuAsync(Guid skuId, UserContext user, CancellationToken cancellationToken);
}

public interface IAuditService { Task RegisterAsync(CatalogAuditEntry entry, CancellationToken cancellationToken); }
public interface IClock { DateTimeOffset UtcNow { get; } }
public interface ICatalogMetrics
{
    void CatalogRequested();
    void CatalogEmpty();
    void QuoteRequested();
    void QuoteFailed();
    void PriceUpdated();
    void PriceUpdateFailed();
}

public sealed class SystemClock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
public sealed class NoopCatalogMetrics : ICatalogMetrics
{
    public void CatalogRequested() { }
    public void CatalogEmpty() { }
    public void QuoteRequested() { }
    public void QuoteFailed() { }
    public void PriceUpdated() { }
    public void PriceUpdateFailed() { }
}

public sealed class CatalogQueryService(ISkuRepository skus, IStoreSkuPriceRepository prices, IClock clock, ICatalogMetrics metrics) : ICatalogQueryService
{
    public async Task<StoreCatalogResponse> GetStoreCatalogAsync(Guid storeId, CancellationToken cancellationToken)
    {
        metrics.CatalogRequested();
        var now = clock.UtcNow;
        var activeSkus = await skus.GetActiveSkusByStoreAsync(storeId, now, cancellationToken);
        var currentPrices = (await prices.GetCurrentPricesByStoreAsync(storeId, now, cancellationToken)).ToDictionary(x => x.SkuId);
        var sellable = activeSkus.Where(s => s.Product?.Status == ProductStatus.Active && currentPrices.ContainsKey(s.Id)).ToList();
        if (sellable.Count == 0) metrics.CatalogEmpty();

        var products = sellable
            .GroupBy(s => s.Product!)
            .OrderBy(g => g.Key.Name)
            .Select(g => new CatalogProductResponse(
                g.Key.Id,
                g.Key.Name,
                g.Key.Description,
                g.Key.ProductType,
                g.OrderBy(s => s.Variant?.SortOrder ?? int.MaxValue).ThenBy(s => s.SortOrder).ThenBy(s => s.Code)
                    .Select(s => ToSkuResponse(s, currentPrices[s.Id], true))
                    .ToList()))
            .ToList();

        return new StoreCatalogResponse(storeId, CatalogPricingConstants.DefaultCurrency, products);
    }

    public async Task<SkuDetailResponse> GetSkuAsync(Guid storeId, Guid skuId, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var sku = await skus.GetByIdAsync(skuId, cancellationToken) ?? throw CatalogPricingErrors.NotFound("SKU");
        EnsureSellableSku(sku);
        var price = await prices.GetCurrentPriceAsync(storeId, skuId, now, cancellationToken) ?? throw CatalogPricingErrors.Conflict("price-not-available", "O SKU informado nao possui preco vigente para a loja.");
        return new SkuDetailResponse(sku.Id, sku.Code, sku.Name, sku.Product!.Name, ToAttributes(sku), price.Price, price.Currency, true);
    }

    internal static void EnsureSellableSku(Sku sku)
    {
        if (sku.Status != SkuStatus.Active) throw CatalogPricingErrors.Conflict("sku-inactive", "SKU inativo nao pode ser vendido.");
        if (sku.Product is null) throw CatalogPricingErrors.Conflict("product-not-loaded", "Produto do SKU nao foi carregado.");
        if (sku.Product.Status != ProductStatus.Active) throw CatalogPricingErrors.Conflict("product-inactive", "Produto inativo nao pode ser vendido.");
    }

    internal static SkuResponse ToSkuResponse(Sku sku, StoreSkuPrice? price, bool available) =>
        new(sku.Id, sku.Code, sku.Name, ToAttributes(sku), price?.Price, price?.Currency ?? CatalogPricingConstants.DefaultCurrency, available);

    internal static IReadOnlyDictionary<string, string> ToAttributes(Sku sku) =>
        sku.Attributes.OrderBy(a => a.SortOrder).ToDictionary(a => a.AttributeName, a => a.AttributeValue, StringComparer.OrdinalIgnoreCase);
}

public sealed class QuoteService(ISkuRepository skus, IStoreSkuPriceRepository prices, IClock clock, ICatalogMetrics metrics) : IQuoteService
{
    public async Task<QuoteResponse> QuoteAsync(Guid storeId, QuoteRequest request, CancellationToken cancellationToken)
    {
        metrics.QuoteRequested();
        try
        {
            if (request.Items.Count == 0) throw CatalogPricingErrors.BadRequest("empty-quote", "Quote precisa conter ao menos um item.");
            if (request.Items.Count > 50) throw CatalogPricingErrors.BadRequest("too-many-items", "Quote excede o limite de itens.");

            var now = clock.UtcNow;
            var items = new List<QuoteItemResponse>();
            foreach (var item in request.Items)
            {
                CatalogValidators.EnsureQuantity(item.Quantity);
                var sku = await skus.GetByIdAsync(item.SkuId, cancellationToken) ?? throw CatalogPricingErrors.NotFound("SKU");
                CatalogQueryService.EnsureSellableSku(sku);
                var price = await prices.GetCurrentPriceAsync(storeId, item.SkuId, now, cancellationToken) ?? throw CatalogPricingErrors.Conflict("price-not-available", "O SKU informado nao possui preco vigente para a loja.");
                var subtotal = decimal.Round(price.Price * item.Quantity, 2);
                items.Add(new QuoteItemResponse(sku.Id, sku.Code, sku.Product!.Name, sku.Name, CatalogQueryService.ToAttributes(sku), price.Price, item.Quantity, subtotal, price.Currency));
            }

            return new QuoteResponse(storeId, CatalogPricingConstants.DefaultCurrency, items, items.Sum(x => x.Subtotal));
        }
        catch
        {
            metrics.QuoteFailed();
            throw;
        }
    }
}

public sealed class PriceService(ISkuRepository skus, IStoreSkuPriceRepository prices, IPriceHistoryRepository history, IUnitOfWork unitOfWork, IAuditService audit, IClock clock, ICatalogMetrics metrics) : IPriceService
{
    public async Task<StoreSkuPriceResponse> GetPriceAsync(Guid storeId, Guid skuId, CancellationToken cancellationToken)
    {
        var price = await prices.GetCurrentPriceAsync(storeId, skuId, clock.UtcNow, cancellationToken) ?? throw CatalogPricingErrors.Conflict("price-not-available", "Preco vigente nao encontrado.");
        return ToResponse(price);
    }

    public async Task<StoreSkuPriceResponse> UpsertPriceAsync(Guid storeId, Guid skuId, UpsertPriceRequest request, UserContext user, CancellationToken cancellationToken)
    {
        EnsureCanEditPrice(storeId, user);
        CatalogValidators.EnsurePrice(request.Price);
        if (!CatalogValidators.IsCurrency(request.Currency)) throw CatalogPricingErrors.BadRequest("invalid-currency", "Moeda invalida.");
        if (!CatalogValidators.IsDateRange(request.ValidFrom, request.ValidTo)) throw CatalogPricingErrors.BadRequest("invalid-date-range", "Data final deve ser maior que data inicial.");
        var sku = await skus.GetByIdAsync(skuId, cancellationToken) ?? throw CatalogPricingErrors.NotFound("SKU");

        StoreSkuPrice? created = null;
        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var current = await prices.GetCurrentPriceAsync(storeId, skuId, clock.UtcNow, ct);
                if (current is not null)
                {
                    current.ValidTo = request.ValidFrom;
                    current.Status = PriceStatus.Inactive;
                    current.UpdatedAt = clock.UtcNow;
                    await prices.UpdateAsync(current, ct);
                }

                created = new StoreSkuPrice { StoreId = storeId, SkuId = sku.Id, Price = request.Price, Currency = request.Currency, Status = PriceStatus.Active, ValidFrom = request.ValidFrom, ValidTo = request.ValidTo, CreatedAt = clock.UtcNow, CreatedBy = user.ActorId };
                await prices.AddAsync(created, ct);
                await history.AddAsync(new PriceHistory { StoreId = storeId, SkuId = skuId, StoreSkuPriceId = created.Id, PreviousPrice = current?.Price, NewPrice = request.Price, Currency = request.Currency, ChangedBy = user.ActorId, ChangedAt = clock.UtcNow, Reason = request.Reason }, ct);
                await audit.RegisterAsync(new CatalogAuditEntry(user.ActorId, user.ActorType, storeId, "PRICE_UPSERT", "StoreSkuPrice", skuId, request.Reason), ct);
            }, cancellationToken);
            metrics.PriceUpdated();
            return ToResponse(created!);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            metrics.PriceUpdateFailed();
            throw CatalogPricingErrors.Conflict("concurrency-conflict", ex.Message);
        }
        catch
        {
            metrics.PriceUpdateFailed();
            throw;
        }
    }

    public async Task ActivatePriceAsync(Guid storeId, Guid skuId, UserContext user, CancellationToken cancellationToken) => await ChangeStatusAsync(storeId, skuId, PriceStatus.Active, user, cancellationToken);
    public async Task DeactivatePriceAsync(Guid storeId, Guid skuId, UserContext user, CancellationToken cancellationToken) => await ChangeStatusAsync(storeId, skuId, PriceStatus.Inactive, user, cancellationToken);

    private async Task ChangeStatusAsync(Guid storeId, Guid skuId, PriceStatus status, UserContext user, CancellationToken cancellationToken)
    {
        EnsureCanEditPrice(storeId, user);
        var price = await prices.GetCurrentPriceAsync(storeId, skuId, clock.UtcNow, cancellationToken) ?? throw CatalogPricingErrors.Conflict("price-not-available", "Preco vigente nao encontrado.");
        price.Status = status;
        price.UpdatedAt = clock.UtcNow;
        await prices.UpdateAsync(price, cancellationToken);
        await audit.RegisterAsync(new CatalogAuditEntry(user.ActorId, user.ActorType, storeId, $"PRICE_{status.ToString().ToUpperInvariant()}", "StoreSkuPrice", skuId, null), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureCanEditPrice(Guid storeId, UserContext user)
    {
        if (!user.HasPermission("PRICE_EDIT") && !user.IsKuvaAdmin) throw CatalogPricingErrors.Forbidden("Usuario sem PRICE_EDIT.");
        if (!user.IsKuvaAdmin && user.StoreId != storeId) throw CatalogPricingErrors.Forbidden("Loja da rota diferente da claim.");
    }

    private static StoreSkuPriceResponse ToResponse(StoreSkuPrice price) => new(price.StoreId, price.SkuId, price.Price, price.Currency, price.Status, price.ValidFrom, price.ValidTo);
}

public sealed class SkuManagementService(ISkuRepository skus, IProductRepository products, IUnitOfWork unitOfWork, IAuditService audit) : ISkuManagementService
{
    public async Task<SkuResponse> CreateSkuAsync(CreateSkuRequest request, UserContext user, CancellationToken cancellationToken)
    {
        EnsureCanEditCatalog(user);
        if (!CatalogValidators.IsSkuCode(request.Code)) throw CatalogPricingErrors.BadRequest("invalid-sku-code", "Codigo SKU invalido.");
        if (string.IsNullOrWhiteSpace(request.Name)) throw CatalogPricingErrors.BadRequest("missing-sku-name", "Nome do SKU e obrigatorio.");
        if (request.ProductId == Guid.Empty) throw CatalogPricingErrors.BadRequest("missing-product", "Produto e obrigatorio.");
        if (await products.GetByIdAsync(request.ProductId, cancellationToken) is null) throw CatalogPricingErrors.NotFound("Produto");
        if (await skus.GetByCodeAsync(request.Code, cancellationToken) is not null) throw CatalogPricingErrors.Conflict("duplicated-sku-code", "Codigo SKU ja existe.");
        CatalogValidators.EnsurePhotoPrintAttributes(request.Attributes);

        var sku = new Sku { ProductId = request.ProductId, VariantId = request.VariantId, Code = request.Code, Name = request.Name, Description = request.Description, Status = SkuStatus.Active, SortOrder = request.SortOrder };
        sku.Attributes = request.Attributes.Select((kv, i) => new SkuAttribute { SkuId = sku.Id, AttributeName = kv.Key, AttributeValue = kv.Value, SortOrder = i + 1 }).ToList();
        await skus.AddAsync(sku, cancellationToken);
        await audit.RegisterAsync(new CatalogAuditEntry(user.ActorId, user.ActorType, null, "SKU_CREATE", "Sku", sku.Id, sku.Code), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CatalogQueryService.ToSkuResponse(sku, null, true);
    }

    public async Task<SkuResponse> UpdateSkuAsync(Guid skuId, UpdateSkuRequest request, UserContext user, CancellationToken cancellationToken)
    {
        EnsureCanEditCatalog(user);
        if (string.IsNullOrWhiteSpace(request.Name)) throw CatalogPricingErrors.BadRequest("missing-sku-name", "Nome do SKU e obrigatorio.");
        CatalogValidators.EnsurePhotoPrintAttributes(request.Attributes);
        var sku = await skus.GetByIdAsync(skuId, cancellationToken) ?? throw CatalogPricingErrors.NotFound("SKU");
        sku.Name = request.Name;
        sku.Description = request.Description;
        sku.SortOrder = request.SortOrder;
        sku.Attributes = request.Attributes.Select((kv, i) => new SkuAttribute { SkuId = sku.Id, AttributeName = kv.Key, AttributeValue = kv.Value, SortOrder = i + 1 }).ToList();
        await audit.RegisterAsync(new CatalogAuditEntry(user.ActorId, user.ActorType, null, "SKU_UPDATE", "Sku", sku.Id, sku.Code), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CatalogQueryService.ToSkuResponse(sku, null, sku.Status == SkuStatus.Active);
    }

    public async Task ActivateSkuAsync(Guid skuId, UserContext user, CancellationToken cancellationToken) => await ChangeStatusAsync(skuId, SkuStatus.Active, user, cancellationToken);
    public async Task DeactivateSkuAsync(Guid skuId, UserContext user, CancellationToken cancellationToken) => await ChangeStatusAsync(skuId, SkuStatus.Inactive, user, cancellationToken);

    private async Task ChangeStatusAsync(Guid skuId, SkuStatus status, UserContext user, CancellationToken cancellationToken)
    {
        EnsureCanEditCatalog(user);
        var sku = await skus.GetByIdAsync(skuId, cancellationToken) ?? throw CatalogPricingErrors.NotFound("SKU");
        sku.Status = status;
        await audit.RegisterAsync(new CatalogAuditEntry(user.ActorId, user.ActorType, null, $"SKU_{status.ToString().ToUpperInvariant()}", "Sku", sku.Id, sku.Code), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureCanEditCatalog(UserContext user)
    {
        if (!user.HasPermission("CATALOG_EDIT") && !user.IsKuvaAdmin) throw CatalogPricingErrors.Forbidden("Usuario sem CATALOG_EDIT.");
    }
}

public sealed class AuditService(IAuditLogRepository repository) : IAuditService
{
    public async Task RegisterAsync(CatalogAuditEntry entry, CancellationToken cancellationToken)
    {
        await repository.AddAsync(new CatalogAuditLog { ActorId = entry.ActorId, ActorType = entry.ActorType, StoreId = entry.StoreId, Action = entry.Action, ResourceType = entry.ResourceType, ResourceId = entry.ResourceId, MetadataJson = entry.MetadataJson, CreatedAt = DateTimeOffset.UtcNow }, cancellationToken);
    }
}
