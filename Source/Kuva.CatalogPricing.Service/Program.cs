using System.Diagnostics;
using System.Security.Claims;
using Kuva.CatalogPricing.Business;
using Kuva.CatalogPricing.Entities;
using Kuva.CatalogPricing.Repository;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("CatalogPricingDatabase")
    ?? "Server=localhost,1433;Database=KuvaCatalogPricing;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True";

builder.Services.AddDbContext<CatalogPricingDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ISkuRepository, SkuRepository>();
builder.Services.AddScoped<IStoreSkuPriceRepository, StoreSkuPriceRepository>();
builder.Services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICatalogQueryService, CatalogQueryService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IPriceService, PriceService>();
builder.Services.AddScoped<ISkuManagementService, SkuManagementService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<ICatalogMetrics, PrometheusCatalogMetrics>();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddDbContextCheck<CatalogPricingDbContext>("sqlserver");

builder.Services.AddAuthentication("Bearer").AddJwtBearer("Bearer", options =>
{
    options.Authority = builder.Configuration["Jwt:Issuer"];
    options.Audience = builder.Configuration["Jwt:Audience"];
    options.RequireHttpsMetadata = false;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CatalogViewPolicy", p => p.RequireAssertion(c => HasPermission(c.User, "CATALOG_VIEW") || IsAdmin(c.User)));
    options.AddPolicy("CatalogEditPolicy", p => p.RequireAssertion(c => HasPermission(c.User, "CATALOG_EDIT") || IsAdmin(c.User)));
    options.AddPolicy("PriceEditPolicy", p => p.RequireAssertion(c => HasPermission(c.User, "PRICE_EDIT") || IsAdmin(c.User)));
    options.AddPolicy("SkuEnableDisablePolicy", p => p.RequireAssertion(c => HasPermission(c.User, "SKU_ENABLE_DISABLE") || IsAdmin(c.User)));
    options.AddPolicy("KuvaAdminPolicy", p => p.RequireAssertion(c => IsAdmin(c.User)));
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var catalogException = exception as CatalogPricingException;
        context.Response.StatusCode = catalogException?.StatusCode ?? StatusCodes.Status500InternalServerError;
        var problem = new ProblemDetails
        {
            Type = $"https://kuva.com.br/errors/{catalogException?.Code ?? "internal-error"}",
            Title = catalogException?.Message ?? "Erro interno.",
            Status = context.Response.StatusCode,
            Detail = catalogException?.Message,
            Extensions = { ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier }
        };
        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers.TryGetValue("x-correlation-id", out var value) ? value.ToString() : Guid.NewGuid().ToString("N");
    context.Response.Headers["x-correlation-id"] = correlationId;
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapMetrics("/metrics");
app.MapControllers();

app.Run();

static bool HasPermission(ClaimsPrincipal user, string permission) =>
    user.Claims.Where(c => c.Type is "permissions" or "permission").Any(c => string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));

static bool IsAdmin(ClaimsPrincipal user) =>
    user.Claims.Where(c => c.Type is ClaimTypes.Role or "roles" or "role").Any(c => string.Equals(c.Value, "KUVA_ADMIN", StringComparison.OrdinalIgnoreCase));

public sealed class PrometheusCatalogMetrics : ICatalogMetrics
{
    private static readonly Counter CatalogRequests = Metrics.CreateCounter("kuva_catalog_requests_total", "Total catalog requests.");
    private static readonly Counter QuoteRequests = Metrics.CreateCounter("kuva_catalog_quote_requests_total", "Total quote requests.");
    private static readonly Counter QuoteFailures = Metrics.CreateCounter("kuva_catalog_quote_failures_total", "Total quote failures.");
    private static readonly Counter PriceUpdates = Metrics.CreateCounter("kuva_catalog_price_updates_total", "Total price updates.");
    private static readonly Counter PriceUpdateFailures = Metrics.CreateCounter("kuva_catalog_price_update_failures_total", "Total price update failures.");
    private static readonly Counter EmptyCatalogResponses = Metrics.CreateCounter("kuva_catalog_catalog_empty_total", "Total empty catalog responses.");
    public void CatalogRequested() => CatalogRequests.Inc();
    public void CatalogEmpty() => EmptyCatalogResponses.Inc();
    public void QuoteRequested() => QuoteRequests.Inc();
    public void QuoteFailed() => QuoteFailures.Inc();
    public void PriceUpdated() => PriceUpdates.Inc();
    public void PriceUpdateFailed() => PriceUpdateFailures.Inc();
}
