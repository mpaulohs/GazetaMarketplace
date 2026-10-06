using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GazetaMarketplace.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PriceCents = table.Column<long>(type: "bigint", nullable: true),
                    Cep = table.Column<string>(type: "char(8)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Uf = table.Column<string>(type: "char(2)", nullable: true),
                    LocationManual = table.Column<bool>(type: "bit", nullable: false),
                    Attributes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VehicleBrandId = table.Column<int>(type: "int", nullable: true, computedColumnSql: "TRY_CAST(JSON_VALUE([Attributes], '$.brandId') AS int)", stored: true),
                    VehicleModelId = table.Column<int>(type: "int", nullable: true, computedColumnSql: "TRY_CAST(JSON_VALUE([Attributes], '$.modelId') AS int)", stored: true),
                    ModelYear = table.Column<int>(type: "int", nullable: true, computedColumnSql: "TRY_CAST(JSON_VALUE([Attributes], '$.modelYear') AS int)", stored: true),
                    Km = table.Column<int>(type: "int", nullable: true, computedColumnSql: "TRY_CAST(JSON_VALUE([Attributes], '$.km') AS int)", stored: true),
                    AreaM2 = table.Column<decimal>(type: "decimal(12,2)", nullable: true, computedColumnSql: "TRY_CAST(JSON_VALUE([Attributes], '$.areaM2') AS decimal(12,2))", stored: true),
                    TitleSearch = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DescriptionSearch = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorId = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedById = table.Column<int>(type: "int", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedById = table.Column<int>(type: "int", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ads", x => x.Id);
                    table.CheckConstraint("CK_Ads_Attributes", "ISJSON([Attributes]) = 1 AND LEFT(LTRIM([Attributes]), 1) = '{'");
                    table.CheckConstraint("CK_Ads_Cep", "[Cep] IS NULL OR ([Cep] NOT LIKE '%[^0-9]%' AND LEN([Cep]) = 8)");
                    table.CheckConstraint("CK_Ads_Description", "[Description] IS NULL OR LEN([Description]) <= 6000");
                    table.CheckConstraint("CK_Ads_PriceCents", "[PriceCents] IS NULL OR ([PriceCents] > 0 AND [PriceCents] <= 9999999999)");
                    table.CheckConstraint("CK_Ads_Status", "[Status] BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_Ads_Title", "LEN(LTRIM(RTRIM([Title]))) > 0");
                    table.CheckConstraint("CK_Ads_Uf", "[Uf] IS NULL OR ([Uf] NOT LIKE '%[^A-Za-z]%' AND LEN([Uf]) = 2)");
                    table.ForeignKey(
                        name: "FK_Ads_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ads_AspNetUsers_PublishedById",
                        column: x => x.PublishedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ads_AspNetUsers_RejectedById",
                        column: x => x.RejectedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ads_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    StorageKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    SizeBytes = table.Column<int>(type: "int", nullable: false),
                    OriginalKey = table.Column<string>(type: "varchar(260)", unicode: false, maxLength: 260, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdPhotos", x => x.Id);
                    table.CheckConstraint("CK_AdPhotos_Size", "[Width] > 0 AND [Height] > 0 AND [SizeBytes] > 0");
                    table.CheckConstraint("CK_AdPhotos_SortOrder", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_AdPhotos_Ads_AdId",
                        column: x => x.AdId,
                        principalTable: "Ads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdPhotos_AdId_SortOrder",
                table: "AdPhotos",
                columns: new[] { "AdId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UQ_AdPhotos_StorageKey",
                table: "AdPhotos",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ads_AreaM2",
                table: "Ads",
                column: "AreaM2");

            migrationBuilder.CreateIndex(
                name: "IX_Ads_AuthorId_Status_UpdatedAt",
                table: "Ads",
                columns: new[] { "AuthorId", "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Ads_CategoryId",
                table: "Ads",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Ads_Km",
                table: "Ads",
                column: "Km");

            migrationBuilder.CreateIndex(
                name: "IX_Ads_PublishedById",
                table: "Ads",
                column: "PublishedById");

            migrationBuilder.CreateIndex(
                name: "IX_Ads_RejectedById",
                table: "Ads",
                column: "RejectedById");

            migrationBuilder.CreateIndex(
                name: "IX_Ads_Status_CategoryId_PublishedAt",
                table: "Ads",
                columns: new[] { "Status", "CategoryId", "PublishedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Ads_Status_PriceCents",
                table: "Ads",
                columns: new[] { "Status", "PriceCents" },
                filter: "[PriceCents] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Ads_Status_Uf_City",
                table: "Ads",
                columns: new[] { "Status", "Uf", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_Ads_VehicleBrandId_ModelYear",
                table: "Ads",
                columns: new[] { "VehicleBrandId", "ModelYear" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdPhotos");

            migrationBuilder.DropTable(
                name: "Ads");
        }
    }
}
