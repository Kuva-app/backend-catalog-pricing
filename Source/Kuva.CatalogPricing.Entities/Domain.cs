namespace Kuva.CatalogPricing.Entities;

public static class CatalogPricingConstants
{
    public const string DefaultCurrency = "BRL";
    public const string ProductTypePhotoPrint = "PHOTO_PRINT";
    public static readonly string[] RequiredPhotoPrintAttributes = ["width", "height", "unit", "finish"];
    public static readonly string[] SupportedFinishes = ["brilho", "fosco"];
}

public enum ProductStatus { Draft = 0, Active = 1, Inactive = 2, Archived = 3 }
public enum SkuStatus { Draft = 0, Active = 1, Inactive = 2, Archived = 3 }
public enum PriceStatus { Active = 1, Inactive = 2, Expired = 3 }
public enum AuditActorType { System = 0, Consumer = 1, StoreOperator = 2, StoreOwner = 3, KuvaAdmin = 4 }

public sealed class ProductCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<Product> Products { get; set; } = [];
}

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public ProductCategory? Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ProductType { get; set; } = CatalogPricingConstants.ProductTypePhotoPrint;
    public ProductStatus Status { get; set; } = ProductStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public List<ProductVariant> Variants { get; set; } = [];
    public List<Sku> Skus { get; set; } = [];
}

public sealed class ProductVariant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<Sku> Skus { get; set; } = [];
}

public sealed class Sku
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SkuStatus Status { get; set; } = SkuStatus.Draft;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public List<SkuAttribute> Attributes { get; set; } = [];
    public List<StoreSkuPrice> Prices { get; set; } = [];
}

public sealed class SkuAttribute
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SkuId { get; set; }
    public Sku? Sku { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public string AttributeValue { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class StoreSkuPrice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StoreId { get; set; }
    public Guid SkuId { get; set; }
    public Sku? Sku { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = CatalogPricingConstants.DefaultCurrency;
    public PriceStatus Status { get; set; } = PriceStatus.Active;
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public List<PriceHistory> History { get; set; } = [];

    public bool IsCurrent(DateTimeOffset now) =>
        Status == PriceStatus.Active && ValidFrom <= now && (ValidTo is null || ValidTo > now);
}

public sealed class PriceHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StoreId { get; set; }
    public Guid SkuId { get; set; }
    public Guid StoreSkuPriceId { get; set; }
    public StoreSkuPrice? StoreSkuPrice { get; set; }
    public decimal? PreviousPrice { get; set; }
    public decimal NewPrice { get; set; }
    public string Currency { get; set; } = CatalogPricingConstants.DefaultCurrency;
    public Guid ChangedBy { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Reason { get; set; }
}

public sealed class CatalogAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ActorId { get; set; }
    public AuditActorType ActorType { get; set; }
    public Guid? StoreId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public Guid? ResourceId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed record UserContext(
    Guid ActorId,
    Guid? StoreId,
    AuditActorType ActorType,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions)
{
    public bool HasPermission(string permission) => Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    public bool IsKuvaAdmin => Roles.Contains("KUVA_ADMIN", StringComparer.OrdinalIgnoreCase) || ActorType == AuditActorType.KuvaAdmin;
}

public sealed record CatalogAuditEntry(Guid? ActorId, AuditActorType ActorType, Guid? StoreId, string Action, string ResourceType, Guid? ResourceId, string? MetadataJson);
public sealed record QuoteItemRequest(Guid SkuId, int Quantity);
public sealed record QuoteRequest(IReadOnlyCollection<QuoteItemRequest> Items);
public sealed record UpsertPriceRequest(decimal Price, string Currency, DateTimeOffset ValidFrom, DateTimeOffset? ValidTo, string? Reason);
public sealed record CreateSkuRequest(Guid ProductId, Guid? VariantId, string Code, string Name, string? Description, IReadOnlyDictionary<string, string> Attributes, int SortOrder);
public sealed record UpdateSkuRequest(string Name, string? Description, IReadOnlyDictionary<string, string> Attributes, int SortOrder);
public sealed record StoreCatalogResponse(Guid StoreId, string Currency, IReadOnlyCollection<CatalogProductResponse> Products);
public sealed record CatalogProductResponse(Guid ProductId, string Name, string? Description, string ProductType, IReadOnlyCollection<SkuResponse> Skus);
public sealed record SkuResponse(Guid SkuId, string Code, string Name, IReadOnlyDictionary<string, string> Attributes, decimal? UnitPrice, string Currency, bool Available);
public sealed record SkuDetailResponse(Guid SkuId, string Code, string Name, string ProductName, IReadOnlyDictionary<string, string> Attributes, decimal UnitPrice, string Currency, bool Available);
public sealed record QuoteItemResponse(Guid SkuId, string SkuCode, string ProductName, string SkuName, IReadOnlyDictionary<string, string> Attributes, decimal UnitPrice, int Quantity, decimal Subtotal, string Currency);
public sealed record QuoteResponse(Guid StoreId, string Currency, IReadOnlyCollection<QuoteItemResponse> Items, decimal TotalAmount);
public sealed record StoreSkuPriceResponse(Guid StoreId, Guid SkuId, decimal Price, string Currency, PriceStatus Status, DateTimeOffset ValidFrom, DateTimeOffset? ValidTo);

public sealed class CatalogPricingException : Exception
{
    public CatalogPricingException(string code, string message, int statusCode = 400) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }
    public int StatusCode { get; }
}

public static class CatalogPricingErrors
{
    public static CatalogPricingException NotFound(string resource) => new("not-found", $"{resource} nao encontrado.", 404);
    public static CatalogPricingException Conflict(string code, string message) => new(code, message, 409);
    public static CatalogPricingException Forbidden(string message = "Operacao nao permitida.") => new("forbidden", message, 403);
    public static CatalogPricingException BadRequest(string code, string message) => new(code, message, 400);
}

public static class CatalogValidators
{
    public static bool IsSlug(string value) => !string.IsNullOrWhiteSpace(value) && value.All(c => char.IsLower(c) || char.IsDigit(c) || c == '-');
    public static bool IsSkuCode(string value) => !string.IsNullOrWhiteSpace(value) && value.All(c => char.IsUpper(c) || char.IsDigit(c) || c == '-');
    public static bool IsCurrency(string value) => value.Length == 3 && value.All(char.IsUpper);
    public static bool IsDateRange(DateTimeOffset from, DateTimeOffset? to) => to is null || to > from;
    public static void EnsurePrice(decimal price)
    {
        if (price <= 0) throw CatalogPricingErrors.BadRequest("invalid-price", "Preco deve ser maior que zero.");
    }

    public static void EnsureQuantity(int quantity, int maxQuantity = 1000)
    {
        if (quantity <= 0) throw CatalogPricingErrors.BadRequest("invalid-quantity", "Quantidade deve ser maior que zero.");
        if (quantity > maxQuantity) throw CatalogPricingErrors.BadRequest("quantity-too-high", "Quantidade excede o maximo permitido.");
    }

    public static void EnsurePhotoPrintAttributes(IReadOnlyDictionary<string, string> attributes)
    {
        foreach (var name in CatalogPricingConstants.RequiredPhotoPrintAttributes)
        {
            if (!attributes.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
                throw CatalogPricingErrors.BadRequest("missing-sku-attribute", $"Atributo obrigatorio ausente: {name}.");
        }

        if (!CatalogPricingConstants.SupportedFinishes.Contains(attributes["finish"], StringComparer.OrdinalIgnoreCase))
            throw CatalogPricingErrors.BadRequest("invalid-finish", "Acabamento deve ser brilho ou fosco.");
    }
}
