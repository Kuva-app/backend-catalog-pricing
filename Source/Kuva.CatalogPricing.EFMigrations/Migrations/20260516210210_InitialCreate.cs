using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kuva.CatalogPricing.EFMigrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pricing");

            migrationBuilder.CreateTable(
                name: "catalog_audit_logs",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorType = table.Column<int>(type: "int", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "product_categories",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProductType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_products_product_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "pricing",
                        principalTable: "product_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_variants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_variants_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "pricing",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skus",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_skus_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "pricing",
                        principalTable: "product_variants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_skus_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "pricing",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sku_attributes",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AttributeValue = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sku_attributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sku_attributes_skus_SkuId",
                        column: x => x.SkuId,
                        principalSchema: "pricing",
                        principalTable: "skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store_sku_prices",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ValidTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_sku_prices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_store_sku_prices_skus_SkuId",
                        column: x => x.SkuId,
                        principalSchema: "pricing",
                        principalTable: "skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "price_history",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreSkuPriceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NewPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ChangedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_price_history_store_sku_prices_StoreSkuPriceId",
                        column: x => x.StoreSkuPriceId,
                        principalSchema: "pricing",
                        principalTable: "store_sku_prices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "pricing",
                table: "product_categories",
                columns: new[] { "Id", "Active", "CreatedAt", "Name", "Slug", "UpdatedAt" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), true, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Impressao fotografica", "impressao-fotografica", null });

            migrationBuilder.InsertData(
                schema: "pricing",
                table: "products",
                columns: new[] { "Id", "CategoryId", "CreatedAt", "Description", "Name", "ProductType", "Slug", "Status", "UpdatedAt" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Impressao fotografica em papel fotografico", "Foto impressa", "PHOTO_PRINT", "foto-impressa", 1, null });

            migrationBuilder.InsertData(
                schema: "pricing",
                table: "product_variants",
                columns: new[] { "Id", "Active", "CreatedAt", "Description", "Name", "ProductId", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("33c2e1f8-4e9a-4771-8f43-6e0c19bf2ed0"), true, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Foto 13x18 cm", new Guid("22222222-2222-2222-2222-222222222222"), 2, null },
                    { new Guid("41de4db1-31a2-4cbf-9e20-7083ba6f8ac5"), true, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Foto 15x21 cm", new Guid("22222222-2222-2222-2222-222222222222"), 3, null },
                    { new Guid("acb5f4a9-35fb-406a-a020-9bc5aaffa8c4"), true, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Foto 10x15 cm", new Guid("22222222-2222-2222-2222-222222222222"), 1, null }
                });

            migrationBuilder.InsertData(
                schema: "pricing",
                table: "skus",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "Name", "ProductId", "SortOrder", "Status", "UpdatedAt", "VariantId" },
                values: new object[,]
                {
                    { new Guid("1334ef2a-c20f-49ea-9b27-3e76998f2588"), "FOTO-13X18-FOSCO", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Foto 13x18 cm - FOSCO", new Guid("22222222-2222-2222-2222-222222222222"), 2, 1, null, new Guid("33c2e1f8-4e9a-4771-8f43-6e0c19bf2ed0") },
                    { new Guid("3b3e2c02-7107-4361-8ad9-ff57dc8ebbd1"), "FOTO-15X21-FOSCO", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Foto 15x21 cm - FOSCO", new Guid("22222222-2222-2222-2222-222222222222"), 3, 1, null, new Guid("41de4db1-31a2-4cbf-9e20-7083ba6f8ac5") },
                    { new Guid("4ca128f5-c7f4-4366-85d3-f4400f973b63"), "FOTO-10X15-FOSCO", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Foto 10x15 cm - FOSCO", new Guid("22222222-2222-2222-2222-222222222222"), 1, 1, null, new Guid("acb5f4a9-35fb-406a-a020-9bc5aaffa8c4") },
                    { new Guid("7dd70b64-d8d0-406c-8332-1bd4a56a4473"), "FOTO-15X21-BRILHO", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Foto 15x21 cm - BRILHO", new Guid("22222222-2222-2222-2222-222222222222"), 3, 1, null, new Guid("41de4db1-31a2-4cbf-9e20-7083ba6f8ac5") },
                    { new Guid("8104052d-5abd-4f7d-a225-9d691e71f30e"), "FOTO-13X18-BRILHO", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Foto 13x18 cm - BRILHO", new Guid("22222222-2222-2222-2222-222222222222"), 2, 1, null, new Guid("33c2e1f8-4e9a-4771-8f43-6e0c19bf2ed0") },
                    { new Guid("cb454a3f-c8fd-462f-8b86-5855c71ef573"), "FOTO-10X15-BRILHO", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Foto 10x15 cm - BRILHO", new Guid("22222222-2222-2222-2222-222222222222"), 1, 1, null, new Guid("acb5f4a9-35fb-406a-a020-9bc5aaffa8c4") }
                });

            migrationBuilder.InsertData(
                schema: "pricing",
                table: "sku_attributes",
                columns: new[] { "Id", "AttributeName", "AttributeValue", "CreatedAt", "SkuId", "SortOrder", "Unit" },
                values: new object[,]
                {
                    { new Guid("10bff357-a96b-4bff-a156-7f7c508acdfb"), "finish", "fosco", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3b3e2c02-7107-4361-8ad9-ff57dc8ebbd1"), 4, null },
                    { new Guid("2b2ca25a-4884-4a94-b705-f033a865743a"), "unit", "cm", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("4ca128f5-c7f4-4366-85d3-f4400f973b63"), 3, null },
                    { new Guid("2d6dda52-9561-41cf-afce-ff1606545e0c"), "height", "18", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("8104052d-5abd-4f7d-a225-9d691e71f30e"), 2, null },
                    { new Guid("3c748606-0c57-457f-aa75-2c2c9749bdbd"), "height", "21", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("7dd70b64-d8d0-406c-8332-1bd4a56a4473"), 2, null },
                    { new Guid("4426c0a6-9c70-40e0-9972-503e72639e89"), "finish", "brilho", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("7dd70b64-d8d0-406c-8332-1bd4a56a4473"), 4, null },
                    { new Guid("5e5af714-2846-4784-80c0-7a4fa6c7fef8"), "width", "13", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("8104052d-5abd-4f7d-a225-9d691e71f30e"), 1, null },
                    { new Guid("5e73cac3-7091-418e-81b2-71ca5947e37e"), "height", "15", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("4ca128f5-c7f4-4366-85d3-f4400f973b63"), 2, null },
                    { new Guid("61801972-8754-4de5-8c69-ef60502588eb"), "unit", "cm", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3b3e2c02-7107-4361-8ad9-ff57dc8ebbd1"), 3, null },
                    { new Guid("6e8813de-59b4-423b-bd00-0d01c9ff1ee2"), "height", "15", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("cb454a3f-c8fd-462f-8b86-5855c71ef573"), 2, null },
                    { new Guid("79df32f8-50a8-4b9f-8b19-b8d8a3191fdb"), "unit", "cm", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("1334ef2a-c20f-49ea-9b27-3e76998f2588"), 3, null },
                    { new Guid("7a65925e-f335-4e9d-89dd-13fa67ce384a"), "finish", "brilho", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("8104052d-5abd-4f7d-a225-9d691e71f30e"), 4, null },
                    { new Guid("7b1cd0a3-5d5e-41d2-a7cf-c5dba2b07d0e"), "unit", "cm", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("7dd70b64-d8d0-406c-8332-1bd4a56a4473"), 3, null },
                    { new Guid("8176baf1-7fe0-4b88-af64-f1c72b6fa57b"), "width", "15", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("7dd70b64-d8d0-406c-8332-1bd4a56a4473"), 1, null },
                    { new Guid("86f710ba-e399-47af-82ea-b99961151396"), "unit", "cm", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("8104052d-5abd-4f7d-a225-9d691e71f30e"), 3, null },
                    { new Guid("8e198952-6b47-4ce3-a5cc-d14d911efa02"), "width", "10", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("cb454a3f-c8fd-462f-8b86-5855c71ef573"), 1, null },
                    { new Guid("9386dce9-5cb4-4682-86bf-2aa48fc39a08"), "height", "18", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("1334ef2a-c20f-49ea-9b27-3e76998f2588"), 2, null },
                    { new Guid("a18a2ec1-d16c-4c24-acfd-7f54daf9cfb0"), "width", "15", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3b3e2c02-7107-4361-8ad9-ff57dc8ebbd1"), 1, null },
                    { new Guid("a4413521-155c-44e5-bb3a-f5974a32dbb2"), "height", "21", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3b3e2c02-7107-4361-8ad9-ff57dc8ebbd1"), 2, null },
                    { new Guid("ad7c2170-4b62-4703-ae9b-b4a0c026bd4e"), "finish", "brilho", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("cb454a3f-c8fd-462f-8b86-5855c71ef573"), 4, null },
                    { new Guid("c001bdfe-1473-4e8f-9814-16bb2697cd9b"), "unit", "cm", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("cb454a3f-c8fd-462f-8b86-5855c71ef573"), 3, null },
                    { new Guid("e1459915-728f-47a4-b747-c7a3bf9af0f9"), "width", "10", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("4ca128f5-c7f4-4366-85d3-f4400f973b63"), 1, null },
                    { new Guid("e9583566-bb35-4eb6-9c5b-ba46b2f46887"), "finish", "fosco", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("4ca128f5-c7f4-4366-85d3-f4400f973b63"), 4, null },
                    { new Guid("f41c17ab-fcda-43b1-8d4a-1f99f09aad20"), "finish", "fosco", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("1334ef2a-c20f-49ea-9b27-3e76998f2588"), 4, null },
                    { new Guid("f7eda4e5-df7d-44a0-858d-9904660ff374"), "width", "13", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("1334ef2a-c20f-49ea-9b27-3e76998f2588"), 1, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_audit_logs_actor_id_created_at",
                schema: "pricing",
                table: "catalog_audit_logs",
                columns: new[] { "ActorId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_audit_logs_store_id_created_at",
                schema: "pricing",
                table: "catalog_audit_logs",
                columns: new[] { "StoreId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_price_history_store_id_sku_id_changed_at",
                schema: "pricing",
                table: "price_history",
                columns: new[] { "StoreId", "SkuId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_price_history_StoreSkuPriceId",
                schema: "pricing",
                table: "price_history",
                column: "StoreSkuPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_product_categories_slug",
                schema: "pricing",
                table: "product_categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_product_id",
                schema: "pricing",
                table: "product_variants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_products_category_id",
                schema: "pricing",
                table: "products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_products_slug",
                schema: "pricing",
                table: "products",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_status",
                schema: "pricing",
                table: "products",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_sku_attributes_sku_id",
                schema: "pricing",
                table: "sku_attributes",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_skus_code",
                schema: "pricing",
                table: "skus",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_skus_product_id",
                schema: "pricing",
                table: "skus",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_skus_status",
                schema: "pricing",
                table: "skus",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_skus_variant_id",
                schema: "pricing",
                table: "skus",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_store_sku_prices_sku_id",
                schema: "pricing",
                table: "store_sku_prices",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_store_sku_prices_store_id",
                schema: "pricing",
                table: "store_sku_prices",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_store_sku_prices_store_id_sku_id_status_valid_from_valid_to",
                schema: "pricing",
                table: "store_sku_prices",
                columns: new[] { "StoreId", "SkuId", "Status", "ValidFrom", "ValidTo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_audit_logs",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "price_history",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "sku_attributes",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "store_sku_prices",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "skus",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "product_variants",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "products",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "product_categories",
                schema: "pricing");
        }
    }
}
