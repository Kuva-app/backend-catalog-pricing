using System.Security.Claims;
using Kuva.CatalogPricing.Business;
using Kuva.CatalogPricing.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kuva.CatalogPricing.Service;

[ApiController]
[Route("api/v1/catalog-pricing/consumer/stores/{storeId:guid}")]
public sealed class ConsumerCatalogController(ICatalogQueryService catalog, IQuoteService quote) : ControllerBase
{
    [HttpGet("catalog")]
    [AllowAnonymous]
    public Task<StoreCatalogResponse> GetCatalog(Guid storeId, CancellationToken cancellationToken) =>
        catalog.GetStoreCatalogAsync(storeId, cancellationToken);

    [HttpGet("skus/{skuId:guid}")]
    [AllowAnonymous]
    public Task<SkuDetailResponse> GetSku(Guid storeId, Guid skuId, CancellationToken cancellationToken) =>
        catalog.GetSkuAsync(storeId, skuId, cancellationToken);

    [HttpPost("quote")]
    [AllowAnonymous]
    public Task<QuoteResponse> Quote(Guid storeId, QuoteRequest request, CancellationToken cancellationToken) =>
        quote.QuoteAsync(storeId, request, cancellationToken);
}

[ApiController]
[Authorize(Policy = "PriceEditPolicy")]
[Route("api/v1/catalog-pricing/merchant/stores/{storeId:guid}")]
public sealed class MerchantPricingController(IPriceService prices) : ControllerBase
{
    [HttpGet("prices/{skuId:guid}")]
    public async Task<StoreSkuPriceResponse> GetPrice(Guid storeId, Guid skuId, CancellationToken cancellationToken)
    {
        EnsureSameStore(storeId);
        return await prices.GetPriceAsync(storeId, skuId, cancellationToken);
    }

    [HttpPut("prices/{skuId:guid}")]
    public async Task<StoreSkuPriceResponse> UpsertPrice(Guid storeId, Guid skuId, UpsertPriceRequest request, CancellationToken cancellationToken)
    {
        EnsureSameStore(storeId);
        return await prices.UpsertPriceAsync(storeId, skuId, request, UserContextFromClaims(), cancellationToken);
    }

    [HttpPatch("prices/{skuId:guid}/activate")]
    public async Task<IActionResult> ActivatePrice(Guid storeId, Guid skuId, CancellationToken cancellationToken)
    {
        EnsureSameStore(storeId);
        await prices.ActivatePriceAsync(storeId, skuId, UserContextFromClaims(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("prices/{skuId:guid}/deactivate")]
    public async Task<IActionResult> DeactivatePrice(Guid storeId, Guid skuId, CancellationToken cancellationToken)
    {
        EnsureSameStore(storeId);
        await prices.DeactivatePriceAsync(storeId, skuId, UserContextFromClaims(), cancellationToken);
        return NoContent();
    }

    private void EnsureSameStore(Guid storeId)
    {
        var user = UserContextFromClaims();
        if (!user.IsKuvaAdmin && user.StoreId != storeId) throw CatalogPricingErrors.Forbidden("Loja da rota diferente da claim.");
    }

    private UserContext UserContextFromClaims() => ClaimsMapper.Map(User);
}

[ApiController]
[Authorize(Policy = "CatalogEditPolicy")]
[Route("api/v1/catalog-pricing/admin/skus")]
public sealed class AdminSkuController(ISkuManagementService skus) : ControllerBase
{
    [HttpPost]
    public Task<SkuResponse> CreateSku(CreateSkuRequest request, CancellationToken cancellationToken) =>
        skus.CreateSkuAsync(request, ClaimsMapper.Map(User), cancellationToken);

    [HttpPatch("{skuId:guid}")]
    public Task<SkuResponse> UpdateSku(Guid skuId, UpdateSkuRequest request, CancellationToken cancellationToken) =>
        skus.UpdateSkuAsync(skuId, request, ClaimsMapper.Map(User), cancellationToken);

    [HttpPatch("{skuId:guid}/activate")]
    public async Task<IActionResult> ActivateSku(Guid skuId, CancellationToken cancellationToken)
    {
        await skus.ActivateSkuAsync(skuId, ClaimsMapper.Map(User), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{skuId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateSku(Guid skuId, CancellationToken cancellationToken)
    {
        await skus.DeactivateSkuAsync(skuId, ClaimsMapper.Map(User), cancellationToken);
        return NoContent();
    }
}

internal static class ClaimsMapper
{
    public static UserContext Map(ClaimsPrincipal user)
    {
        var actorId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var parsedActor) ? parsedActor : Guid.Empty;
        var storeId = Guid.TryParse(user.FindFirstValue("storeId"), out var parsedStore) ? parsedStore : null as Guid?;
        var roles = user.Claims.Where(c => c.Type is ClaimTypes.Role or "roles" or "role").Select(c => c.Value).ToList();
        var permissions = user.Claims.Where(c => c.Type is "permissions" or "permission").Select(c => c.Value).ToList();
        var actorType = roles.Contains("KUVA_ADMIN", StringComparer.OrdinalIgnoreCase)
            ? AuditActorType.KuvaAdmin
            : roles.Contains("STORE_OWNER", StringComparer.OrdinalIgnoreCase)
                ? AuditActorType.StoreOwner
                : AuditActorType.StoreOperator;
        return new UserContext(actorId, storeId, actorType, roles, permissions);
    }
}
