# Kuva Catalog & Pricing Service

Backend do microservico `Kuva.CatalogPricing`, responsavel por catalogo vendavel, preco por loja, quote de carrinho e snapshot comercial para o Order Service.

## Arquitetura

O servico segue camadas separadas:

- `Kuva.CatalogPricing.Entities`: entidades, DTOs, enums, validacoes e excecoes de dominio.
- `Kuva.CatalogPricing.Repository`: EF Core, DbContext, mappings, repositories, seed inicial e Unit of Work.
- `Kuva.CatalogPricing.Business`: regras de catalogo, quote, preco, SKU e auditoria.
- `Kuva.CatalogPricing.Service`: API ASP.NET Core, JWT, policies, health checks e `/metrics`.
- `Kuva.CatalogPricing.EFMigrations`: migrations isoladas do EF Core.
- `Kuva.CatalogPricing.Tests`: testes unitarios com NUnit, Moq e coverlet.

Modelo comercial do MVP:

```text
Product -> ProductVariant -> SKU -> StoreSkuPrice
```

Produto inicial: `Foto impressa`. SKUs iniciais: 10x15, 13x18 e 15x21 com acabamento `brilho` e `fosco`.

## Requisitos

- .NET SDK 10
- Docker e Docker Compose
- SQL Server local ou container

## Rodar Local

```bash
dotnet restore Source/Kuva.CatalogPricing.sln
dotnet build Source/Kuva.CatalogPricing.sln -m:1
dotnet run --project Source/Kuva.CatalogPricing.Service
```

Configure a connection string por variavel de ambiente:

```bash
export ConnectionStrings__CatalogPricingDatabase="Server=localhost,1433;Database=KuvaCatalogPricing;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True"
```

## Docker

```bash
docker compose up --build
```

Servicos:

- API: `http://localhost:8083`
- OpenAPI dev: `http://localhost:8083/openapi/v1.json`
- Health: `http://localhost:8083/health`
- Metrics: `http://localhost:8083/metrics`
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000`

## Migrations

```bash
dotnet tool restore
dotnet tool run dotnet-ef migrations add InitialCreate \
  --project Source/Kuva.CatalogPricing.EFMigrations \
  --startup-project Source/Kuva.CatalogPricing.EFMigrations \
  --context CatalogPricingDbContext \
  --output-dir Migrations

dotnet tool run dotnet-ef database update \
  --project Source/Kuva.CatalogPricing.EFMigrations \
  --startup-project Source/Kuva.CatalogPricing.EFMigrations \
  --context CatalogPricingDbContext
```

## Endpoints

Base path: `/api/v1/catalog-pricing`

- `GET /consumer/stores/{storeId}/catalog`
- `GET /consumer/stores/{storeId}/skus/{skuId}`
- `POST /consumer/stores/{storeId}/quote`
- `GET /merchant/stores/{storeId}/prices/{skuId}`
- `PUT /merchant/stores/{storeId}/prices/{skuId}`
- `PATCH /merchant/stores/{storeId}/prices/{skuId}/activate`
- `PATCH /merchant/stores/{storeId}/prices/{skuId}/deactivate`
- `POST /admin/skus`
- `PATCH /admin/skus/{skuId}`
- `PATCH /admin/skus/{skuId}/activate`
- `PATCH /admin/skus/{skuId}/deactivate`

## Seguranca

JWT Bearer e configurado com policies:

- `CatalogViewPolicy`
- `CatalogEditPolicy`
- `PriceEditPolicy`
- `SkuEnableDisablePolicy`
- `KuvaAdminPolicy`

Operacoes merchant validam isolamento por loja: `claim.storeId == route.storeId`, exceto `KUVA_ADMIN`.

## Observabilidade

Endpoints:

- `/health`
- `/health/live`
- `/health/ready`
- `/metrics`

Metricas customizadas:

- `kuva_catalog_requests_total`
- `kuva_catalog_quote_requests_total`
- `kuva_catalog_quote_failures_total`
- `kuva_catalog_price_updates_total`
- `kuva_catalog_price_update_failures_total`
- `kuva_catalog_catalog_empty_total`

## Testes e Cobertura

```bash
dotnet test Source/Kuva.CatalogPricing.sln \
  --collect:"XPlat Code Coverage" \
  --settings Source/Kuva.CatalogPricing.Tests/Coverage.runsettings \
  --results-directory TestResults \
  -m:1
```

O comando gera `coverage.cobertura.xml` e `coverage.opencover.xml` em uma subpasta de
`TestResults`. Para o SonarScanner for .NET, aponte o relatorio OpenCover:

```bash
/d:sonar.cs.opencover.reportsPaths="TestResults/**/coverage.opencover.xml"
```

Ultima validacao local:

```text
Passed: 14 / Failed: 0
Line coverage: 100%
Branch coverage: 83.33%
```

## Troubleshooting

- Se o `dotnet test` com cobertura falhar por socket local em ambiente sandbox, rode fora do sandbox ou sem `--collect`.
- Se o SQL Server ainda nao estiver pronto no Docker, aguarde o health check do container `kuva-catalog-pricing-sql`.
- Swagger/OpenAPI fica exposto apenas em desenvolvimento.
