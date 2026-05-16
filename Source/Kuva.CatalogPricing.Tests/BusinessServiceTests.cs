using Kuva.CatalogPricing.Business;
using Kuva.CatalogPricing.Entities;
using Kuva.CatalogPricing.Repository;
using Moq;

namespace Kuva.CatalogPricing.Tests;

[TestFixture]
public sealed class BusinessServiceTests
{
    private static readonly Guid StoreId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherStoreId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SkuId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Now = new(2026, 5, 16, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Catalog_returns_only_sellable_skus_and_records_empty_metric()
    {
        var skuRepository = new Mock<ISkuRepository>(MockBehavior.Strict);
        var priceRepository = new Mock<IStoreSkuPriceRepository>(MockBehavior.Strict);
        var clock = Clock();
        var metrics = new SpyMetrics();
        var active = PhotoSku(SkuId, "brilho");
        var withoutPrice = PhotoSku(Guid.NewGuid(), "fosco");
        var price = ActivePrice(SkuId, 2.5m);

        skuRepository.Setup(x => x.GetActiveSkusByStoreAsync(StoreId, Now, It.IsAny<CancellationToken>())).ReturnsAsync([active, withoutPrice]);
        priceRepository.Setup(x => x.GetCurrentPricesByStoreAsync(StoreId, Now, It.IsAny<CancellationToken>())).ReturnsAsync([price]);

        var sut = new CatalogQueryService(skuRepository.Object, priceRepository.Object, clock.Object, metrics);
        var result = await sut.GetStoreCatalogAsync(StoreId, CancellationToken.None);

        Assert.That(result.Products, Has.Count.EqualTo(1));
        Assert.That(result.Products.Single().Skus, Has.Count.EqualTo(1));
        Assert.That(result.Products.Single().Skus.Single().Attributes["finish"], Is.EqualTo("brilho"));
        Assert.That(metrics.CatalogRequests, Is.EqualTo(1));
        Assert.That(metrics.EmptyCatalogs, Is.Zero);

        skuRepository.Setup(x => x.GetActiveSkusByStoreAsync(OtherStoreId, Now, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        priceRepository.Setup(x => x.GetCurrentPricesByStoreAsync(OtherStoreId, Now, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var empty = await sut.GetStoreCatalogAsync(OtherStoreId, CancellationToken.None);
        Assert.That(empty.Products, Is.Empty);
        Assert.That(metrics.EmptyCatalogs, Is.EqualTo(1));
    }

    [Test]
    public async Task GetSku_rejects_inactive_product_inactive_sku_and_missing_price()
    {
        var skuRepository = new Mock<ISkuRepository>(MockBehavior.Strict);
        var priceRepository = new Mock<IStoreSkuPriceRepository>(MockBehavior.Strict);
        var sut = new CatalogQueryService(skuRepository.Object, priceRepository.Object, Clock().Object, new SpyMetrics());

        var inactiveSku = PhotoSku(SkuId, "brilho", skuStatus: SkuStatus.Inactive);
        skuRepository.Setup(x => x.GetByIdAsync(SkuId, It.IsAny<CancellationToken>())).ReturnsAsync(inactiveSku);
        AssertCode(await ThrowsAsync(() => sut.GetSkuAsync(StoreId, SkuId, CancellationToken.None)), "sku-inactive");

        var inactiveProductSku = PhotoSku(SkuId, "brilho", productStatus: ProductStatus.Inactive);
        skuRepository.Setup(x => x.GetByIdAsync(SkuId, It.IsAny<CancellationToken>())).ReturnsAsync(inactiveProductSku);
        AssertCode(await ThrowsAsync(() => sut.GetSkuAsync(StoreId, SkuId, CancellationToken.None)), "product-inactive");

        var active = PhotoSku(SkuId, "fosco");
        skuRepository.Setup(x => x.GetByIdAsync(SkuId, It.IsAny<CancellationToken>())).ReturnsAsync(active);
        priceRepository.Setup(x => x.GetCurrentPriceAsync(StoreId, SkuId, Now, It.IsAny<CancellationToken>())).ReturnsAsync((StoreSkuPrice?)null);
        AssertCode(await ThrowsAsync(() => sut.GetSkuAsync(StoreId, SkuId, CancellationToken.None)), "price-not-available");
    }

    [Test]
    public async Task Quote_calculates_snapshot_for_single_and_multiple_items()
    {
        var skuRepository = new Mock<ISkuRepository>(MockBehavior.Strict);
        var priceRepository = new Mock<IStoreSkuPriceRepository>(MockBehavior.Strict);
        var metrics = new SpyMetrics();
        var glossy = PhotoSku(SkuId, "brilho");
        var matteId = Guid.NewGuid();
        var matte = PhotoSku(matteId, "fosco");
        skuRepository.Setup(x => x.GetByIdAsync(SkuId, It.IsAny<CancellationToken>())).ReturnsAsync(glossy);
        skuRepository.Setup(x => x.GetByIdAsync(matteId, It.IsAny<CancellationToken>())).ReturnsAsync(matte);
        priceRepository.Setup(x => x.GetCurrentPriceAsync(StoreId, SkuId, Now, It.IsAny<CancellationToken>())).ReturnsAsync(ActivePrice(SkuId, 2.5m));
        priceRepository.Setup(x => x.GetCurrentPriceAsync(StoreId, matteId, Now, It.IsAny<CancellationToken>())).ReturnsAsync(ActivePrice(matteId, 3m));

        var sut = new QuoteService(skuRepository.Object, priceRepository.Object, Clock().Object, metrics);
        var result = await sut.QuoteAsync(StoreId, new QuoteRequest([new(SkuId, 10), new(matteId, 5)]), CancellationToken.None);

        Assert.That(result.TotalAmount, Is.EqualTo(40m));
        Assert.That(result.Currency, Is.EqualTo("BRL"));
        Assert.That(result.Items.First().Subtotal, Is.EqualTo(25m));
        Assert.That(result.Items.First().ProductName, Is.EqualTo("Foto impressa"));
        Assert.That(result.Items.First().Attributes["finish"], Is.EqualTo("brilho"));
        Assert.That(result.Items.Last().Attributes["finish"], Is.EqualTo("fosco"));
        Assert.That(metrics.QuoteRequests, Is.EqualTo(1));
    }

    [TestCase(0, "invalid-quantity")]
    [TestCase(-1, "invalid-quantity")]
    [TestCase(1001, "quantity-too-high")]
    public async Task Quote_rejects_invalid_quantities(int quantity, string code)
    {
        var skuRepository = new Mock<ISkuRepository>(MockBehavior.Strict);
        var priceRepository = new Mock<IStoreSkuPriceRepository>(MockBehavior.Strict);
        var metrics = new SpyMetrics();
        var sut = new QuoteService(skuRepository.Object, priceRepository.Object, Clock().Object, metrics);

        AssertCode(await ThrowsAsync(() => sut.QuoteAsync(StoreId, new QuoteRequest([new(SkuId, quantity)]), CancellationToken.None)), code);
        Assert.That(metrics.QuoteFailures, Is.EqualTo(1));
    }

    [Test]
    public async Task Quote_rejects_empty_too_large_missing_sku_inactive_product_and_missing_price()
    {
        var skuRepository = new Mock<ISkuRepository>(MockBehavior.Strict);
        var priceRepository = new Mock<IStoreSkuPriceRepository>(MockBehavior.Strict);
        var sut = new QuoteService(skuRepository.Object, priceRepository.Object, Clock().Object, new SpyMetrics());

        AssertCode(await ThrowsAsync(() => sut.QuoteAsync(StoreId, new QuoteRequest([]), CancellationToken.None)), "empty-quote");
        AssertCode(await ThrowsAsync(() => sut.QuoteAsync(StoreId, new QuoteRequest(Enumerable.Range(1, 51).Select(_ => new QuoteItemRequest(Guid.NewGuid(), 1)).ToList()), CancellationToken.None)), "too-many-items");

        skuRepository.Setup(x => x.GetByIdAsync(SkuId, It.IsAny<CancellationToken>())).ReturnsAsync((Sku?)null);
        AssertCode(await ThrowsAsync(() => sut.QuoteAsync(StoreId, new QuoteRequest([new(SkuId, 1)]), CancellationToken.None)), "not-found");

        skuRepository.Setup(x => x.GetByIdAsync(SkuId, It.IsAny<CancellationToken>())).ReturnsAsync(PhotoSku(SkuId, "brilho", productStatus: ProductStatus.Inactive));
        AssertCode(await ThrowsAsync(() => sut.QuoteAsync(StoreId, new QuoteRequest([new(SkuId, 1)]), CancellationToken.None)), "product-inactive");

        skuRepository.Setup(x => x.GetByIdAsync(SkuId, It.IsAny<CancellationToken>())).ReturnsAsync(PhotoSku(SkuId, "brilho"));
        priceRepository.Setup(x => x.GetCurrentPriceAsync(StoreId, SkuId, Now, It.IsAny<CancellationToken>())).ReturnsAsync((StoreSkuPrice?)null);
        AssertCode(await ThrowsAsync(() => sut.QuoteAsync(StoreId, new QuoteRequest([new(SkuId, 1)]), CancellationToken.None)), "price-not-available");
    }

    [Test]
    public async Task Price_upsert_creates_initial_price_history_audit_and_transaction()
    {
        var skuRepository = new Mock<ISkuRepository>(MockBehavior.Strict);
        var priceRepository = new Mock<IStoreSkuPriceRepository>(MockBehavior.Strict);
        var historyRepository = new Mock<IPriceHistoryRepository>(MockBehavior.Strict);
        var unitOfWork = TransactionalUnitOfWork();
        var audit = new Mock<IAuditService>(MockBehavior.Strict);
        var metrics = new SpyMetrics();

        skuRepository.Setup(x => x.GetByIdAsync(SkuId, It.IsAny<CancellationToken>())).ReturnsAsync(PhotoSku(SkuId, "brilho"));
        priceRepository.Setup(x => x.GetCurrentPriceAsync(StoreId, SkuId, Now, It.IsAny<CancellationToken>())).ReturnsAsync((StoreSkuPrice?)null);
        priceRepository.Setup(x => x.AddAsync(It.Is<StoreSkuPrice>(p => p.Price == 2.7m), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        historyRepository.Setup(x => x.AddAsync(It.Is<PriceHistory>(h => h.PreviousPrice == null && h.NewPrice == 2.7m), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        audit.Setup(x => x.RegisterAsync(It.IsAny<CatalogAuditEntry>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = new PriceService(skuRepository.Object, priceRepository.Object, historyRepository.Object, unitOfWork.Object, audit.Object, Clock().Object, metrics);
        var result = await sut.UpsertPriceAsync(StoreId, SkuId, new UpsertPriceRequest(2.7m, "BRL", Now, null, "piloto"), PriceEditor(), CancellationToken.None);

        Assert.That(result.Price, Is.EqualTo(2.7m));
        Assert.That(metrics.PriceUpdates, Is.EqualTo(1));
        unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Price_upsert_closes_previous_price_and_rejects_invalid_or_forbidden_changes()
    {
        var skuRepository = new Mock<ISkuRepository>(MockBehavior.Strict);
        var priceRepository = new Mock<IStoreSkuPriceRepository>(MockBehavior.Strict);
        var historyRepository = new Mock<IPriceHistoryRepository>(MockBehavior.Strict);
        var unitOfWork = TransactionalUnitOfWork();
        var audit = new Mock<IAuditService>(MockBehavior.Strict);
        var previous = ActivePrice(SkuId, 2.5m);
        skuRepository.Setup(x => x.GetByIdAsync(SkuId, It.IsAny<CancellationToken>())).ReturnsAsync(PhotoSku(SkuId, "brilho"));
        priceRepository.Setup(x => x.GetCurrentPriceAsync(StoreId, SkuId, Now, It.IsAny<CancellationToken>())).ReturnsAsync(previous);
        priceRepository.Setup(x => x.UpdateAsync(previous, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        priceRepository.Setup(x => x.AddAsync(It.IsAny<StoreSkuPrice>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        historyRepository.Setup(x => x.AddAsync(It.Is<PriceHistory>(h => h.PreviousPrice == 2.5m), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        audit.Setup(x => x.RegisterAsync(It.IsAny<CatalogAuditEntry>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = new PriceService(skuRepository.Object, priceRepository.Object, historyRepository.Object, unitOfWork.Object, audit.Object, Clock().Object, new SpyMetrics());
        await sut.UpsertPriceAsync(StoreId, SkuId, new UpsertPriceRequest(3m, "BRL", Now.AddMinutes(1), null, null), PriceEditor(), CancellationToken.None);
        Assert.That(previous.Status, Is.EqualTo(PriceStatus.Inactive));
        Assert.That(previous.ValidTo, Is.EqualTo(Now.AddMinutes(1)));

        AssertCode(await ThrowsAsync(() => sut.UpsertPriceAsync(StoreId, SkuId, new UpsertPriceRequest(0, "BRL", Now, null, null), PriceEditor(), CancellationToken.None)), "invalid-price");
        AssertCode(await ThrowsAsync(() => sut.UpsertPriceAsync(StoreId, SkuId, new UpsertPriceRequest(-1, "BRL", Now, null, null), PriceEditor(), CancellationToken.None)), "invalid-price");
        AssertCode(await ThrowsAsync(() => sut.UpsertPriceAsync(StoreId, SkuId, new UpsertPriceRequest(1, "BR", Now, null, null), PriceEditor(), CancellationToken.None)), "invalid-currency");
        AssertCode(await ThrowsAsync(() => sut.UpsertPriceAsync(StoreId, SkuId, new UpsertPriceRequest(1, "BRL", Now, Now.AddDays(-1), null), PriceEditor(), CancellationToken.None)), "invalid-date-range");
        AssertCode(await ThrowsAsync(() => sut.UpsertPriceAsync(OtherStoreId, SkuId, new UpsertPriceRequest(1, "BRL", Now, null, null), PriceEditor(), CancellationToken.None)), "forbidden");
        AssertCode(await ThrowsAsync(() => sut.UpsertPriceAsync(StoreId, SkuId, new UpsertPriceRequest(1, "BRL", Now, null, null), NoPricePermission(), CancellationToken.None)), "forbidden");
    }

    [Test]
    public async Task Price_activate_deactivate_and_get_use_unit_of_work()
    {
        var price = ActivePrice(SkuId, 2.5m);
        var skuRepository = new Mock<ISkuRepository>(MockBehavior.Strict);
        var priceRepository = new Mock<IStoreSkuPriceRepository>(MockBehavior.Strict);
        var historyRepository = new Mock<IPriceHistoryRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var audit = new Mock<IAuditService>(MockBehavior.Strict);
        priceRepository.Setup(x => x.GetCurrentPriceAsync(StoreId, SkuId, Now, It.IsAny<CancellationToken>())).ReturnsAsync(price);
        priceRepository.Setup(x => x.UpdateAsync(price, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        audit.Setup(x => x.RegisterAsync(It.IsAny<CatalogAuditEntry>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = new PriceService(skuRepository.Object, priceRepository.Object, historyRepository.Object, unitOfWork.Object, audit.Object, Clock().Object, new SpyMetrics());

        var response = await sut.GetPriceAsync(StoreId, SkuId, CancellationToken.None);
        Assert.That(response.Price, Is.EqualTo(2.5m));
        await sut.DeactivatePriceAsync(StoreId, SkuId, PriceEditor(), CancellationToken.None);
        Assert.That(price.Status, Is.EqualTo(PriceStatus.Inactive));
        price.Status = PriceStatus.Active;
        await sut.ActivatePriceAsync(StoreId, SkuId, PriceEditor(), CancellationToken.None);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test]
    public async Task Sku_management_creates_updates_activates_deactivates_and_validates()
    {
        var skuRepository = new Mock<ISkuRepository>(MockBehavior.Strict);
        var productRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var audit = new Mock<IAuditService>(MockBehavior.Strict);
        var productId = Guid.NewGuid();
        productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(new Product { Id = productId, Status = ProductStatus.Active });
        skuRepository.Setup(x => x.GetByCodeAsync("FOTO-10X15-BRILHO", It.IsAny<CancellationToken>())).ReturnsAsync((Sku?)null);
        skuRepository.Setup(x => x.AddAsync(It.IsAny<Sku>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        audit.Setup(x => x.RegisterAsync(It.IsAny<CatalogAuditEntry>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = new SkuManagementService(skuRepository.Object, productRepository.Object, unitOfWork.Object, audit.Object);
        var create = new CreateSkuRequest(productId, null, "FOTO-10X15-BRILHO", "Foto 10x15", null, Attributes("brilho"), 1);
        var created = await sut.CreateSkuAsync(create, CatalogEditor(), CancellationToken.None);
        Assert.That(created.Attributes["finish"], Is.EqualTo("brilho"));

        var existing = PhotoSku(SkuId, "fosco");
        skuRepository.Setup(x => x.GetByIdAsync(SkuId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var updated = await sut.UpdateSkuAsync(SkuId, new UpdateSkuRequest("Foto 10x15 fosco", null, Attributes("fosco"), 2), CatalogEditor(), CancellationToken.None);
        Assert.That(updated.Attributes["finish"], Is.EqualTo("fosco"));
        await sut.DeactivateSkuAsync(SkuId, CatalogEditor(), CancellationToken.None);
        Assert.That(existing.Status, Is.EqualTo(SkuStatus.Inactive));
        await sut.ActivateSkuAsync(SkuId, CatalogEditor(), CancellationToken.None);
        Assert.That(existing.Status, Is.EqualTo(SkuStatus.Active));

        AssertCode(await ThrowsAsync(() => sut.CreateSkuAsync(create with { Code = "foto-10x15" }, CatalogEditor(), CancellationToken.None)), "invalid-sku-code");
        AssertCode(await ThrowsAsync(() => sut.CreateSkuAsync(create with { Name = "" }, CatalogEditor(), CancellationToken.None)), "missing-sku-name");
        AssertCode(await ThrowsAsync(() => sut.CreateSkuAsync(create with { ProductId = Guid.Empty }, CatalogEditor(), CancellationToken.None)), "missing-product");
        AssertCode(await ThrowsAsync(() => sut.CreateSkuAsync(create with { Attributes = new Dictionary<string, string>() }, CatalogEditor(), CancellationToken.None)), "missing-sku-attribute");
        AssertCode(await ThrowsAsync(() => sut.CreateSkuAsync(create, NoCatalogPermission(), CancellationToken.None)), "forbidden");
    }

    [Test]
    public async Task Sku_management_rejects_duplicate_missing_product_and_invalid_finish()
    {
        var skuRepository = new Mock<ISkuRepository>(MockBehavior.Strict);
        var productRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        var sut = new SkuManagementService(skuRepository.Object, productRepository.Object, new Mock<IUnitOfWork>().Object, new Mock<IAuditService>().Object);
        var productId = Guid.NewGuid();
        var request = new CreateSkuRequest(productId, null, "FOTO-10X15-BRILHO", "Foto", null, Attributes("acetinado"), 1);

        productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        AssertCode(await ThrowsAsync(() => sut.CreateSkuAsync(request, CatalogEditor(), CancellationToken.None)), "not-found");

        productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(new Product { Id = productId });
        skuRepository.Setup(x => x.GetByCodeAsync("FOTO-10X15-BRILHO", It.IsAny<CancellationToken>())).ReturnsAsync(PhotoSku(Guid.NewGuid(), "brilho"));
        AssertCode(await ThrowsAsync(() => sut.CreateSkuAsync(request, CatalogEditor(), CancellationToken.None)), "duplicated-sku-code");

        skuRepository.Setup(x => x.GetByCodeAsync("FOTO-10X15-BRILHO", It.IsAny<CancellationToken>())).ReturnsAsync((Sku?)null);
        AssertCode(await ThrowsAsync(() => sut.CreateSkuAsync(request, CatalogEditor(), CancellationToken.None)), "invalid-finish");
    }

    [Test]
    public void Validators_cover_slug_sku_currency_date_range_price_and_attributes()
    {
        Assert.That(CatalogValidators.IsSlug("foto-impressa-10"), Is.True);
        Assert.That(CatalogValidators.IsSlug("Foto"), Is.False);
        Assert.That(CatalogValidators.IsSkuCode("FOTO-10X15-BRILHO"), Is.True);
        Assert.That(CatalogValidators.IsSkuCode("foto-10x15"), Is.False);
        Assert.That(CatalogValidators.IsCurrency("BRL"), Is.True);
        Assert.That(CatalogValidators.IsCurrency("BR"), Is.False);
        Assert.That(CatalogValidators.IsDateRange(Now, Now.AddDays(1)), Is.True);
        Assert.That(CatalogValidators.IsDateRange(Now, Now.AddDays(-1)), Is.False);
        Assert.DoesNotThrow(() => CatalogValidators.EnsurePrice(1));
        Assert.DoesNotThrow(() => CatalogValidators.EnsurePhotoPrintAttributes(Attributes("brilho")));
    }

    [Test]
    public async Task Infrastructure_helpers_are_exercised_for_unit_coverage()
    {
        var noop = new NoopCatalogMetrics();
        Assert.DoesNotThrow(() =>
        {
            noop.CatalogRequested();
            noop.CatalogEmpty();
            noop.QuoteRequested();
            noop.QuoteFailed();
            noop.PriceUpdated();
            noop.PriceUpdateFailed();
        });

        Assert.That(new SystemClock().UtcNow, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
        Assert.That(ActivePrice(SkuId, 2.5m).IsCurrent(Now), Is.True);
        var inactive = ActivePrice(SkuId, 2.5m);
        inactive.Status = PriceStatus.Inactive;
        Assert.That(inactive.IsCurrent(Now), Is.False);

        var repository = new Mock<IAuditLogRepository>(MockBehavior.Strict);
        repository.Setup(x => x.AddAsync(It.Is<CatalogAuditLog>(a => a.Action == "SKU_CREATE"), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var audit = new AuditService(repository.Object);
        await audit.RegisterAsync(new CatalogAuditEntry(Guid.NewGuid(), AuditActorType.KuvaAdmin, StoreId, "SKU_CREATE", "Sku", SkuId, "{}"), CancellationToken.None);
    }

    private static Mock<IClock> Clock()
    {
        var clock = new Mock<IClock>(MockBehavior.Strict);
        clock.Setup(x => x.UtcNow).Returns(Now);
        return clock;
    }

    private static Mock<IUnitOfWork> TransactionalUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        unitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((operation, token) => operation(token));
        return unitOfWork;
    }

    private static Sku PhotoSku(Guid id, string finish, SkuStatus skuStatus = SkuStatus.Active, ProductStatus productStatus = ProductStatus.Active) =>
        new()
        {
            Id = id,
            Code = $"FOTO-10X15-{finish.ToUpperInvariant()}",
            Name = $"Foto 10x15 cm - {finish}",
            Status = skuStatus,
            Product = new Product { Id = Guid.NewGuid(), Name = "Foto impressa", ProductType = CatalogPricingConstants.ProductTypePhotoPrint, Status = productStatus },
            Attributes = Attributes(finish).Select((kv, i) => new SkuAttribute { AttributeName = kv.Key, AttributeValue = kv.Value, SortOrder = i + 1 }).ToList()
        };

    private static StoreSkuPrice ActivePrice(Guid skuId, decimal price) =>
        new() { Id = Guid.NewGuid(), StoreId = StoreId, SkuId = skuId, Price = price, Currency = "BRL", Status = PriceStatus.Active, ValidFrom = Now.AddDays(-1) };

    private static Dictionary<string, string> Attributes(string finish) =>
        new(StringComparer.OrdinalIgnoreCase) { ["width"] = "10", ["height"] = "15", ["unit"] = "cm", ["finish"] = finish };

    private static UserContext PriceEditor() => new(Guid.NewGuid(), StoreId, AuditActorType.StoreOperator, ["STORE_OPERATOR"], ["PRICE_EDIT", "CATALOG_VIEW"]);
    private static UserContext CatalogEditor() => new(Guid.NewGuid(), null, AuditActorType.KuvaAdmin, ["KUVA_ADMIN"], ["CATALOG_EDIT", "PRICE_EDIT", "SKU_ENABLE_DISABLE"]);
    private static UserContext NoPricePermission() => new(Guid.NewGuid(), StoreId, AuditActorType.StoreOperator, ["STORE_OPERATOR"], ["CATALOG_VIEW"]);
    private static UserContext NoCatalogPermission() => new(Guid.NewGuid(), null, AuditActorType.StoreOperator, ["STORE_OPERATOR"], ["CATALOG_VIEW"]);

    private static async Task<CatalogPricingException> ThrowsAsync(Func<Task> action)
    {
        var exception = Assert.ThrowsAsync<CatalogPricingException>(async () => await action());
        return await Task.FromResult(exception!);
    }

    private static void AssertCode(CatalogPricingException exception, string code) => Assert.That(exception.Code, Is.EqualTo(code));

    private sealed class SpyMetrics : ICatalogMetrics
    {
        public int CatalogRequests { get; private set; }
        public int EmptyCatalogs { get; private set; }
        public int QuoteRequests { get; private set; }
        public int QuoteFailures { get; private set; }
        public int PriceUpdates { get; private set; }
        public int PriceUpdateFailures { get; private set; }
        public void CatalogRequested() => CatalogRequests++;
        public void CatalogEmpty() => EmptyCatalogs++;
        public void QuoteRequested() => QuoteRequests++;
        public void QuoteFailed() => QuoteFailures++;
        public void PriceUpdated() => PriceUpdates++;
        public void PriceUpdateFailed() => PriceUpdateFailures++;
    }
}
