using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GazetaMarketplace.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleBrands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Source = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleBrands", x => new { x.Id, x.Kind });
                    table.CheckConstraint("CK_VehicleBrands_Kind", "[Kind] IN ('car', 'moto')");
                });

            migrationBuilder.CreateTable(
                name: "VehicleModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: false),
                    BrandId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Source = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleModels", x => new { x.Id, x.Kind });
                    table.CheckConstraint("CK_VehicleModels_Kind", "[Kind] IN ('car', 'moto')");
                    table.ForeignKey(
                        name: "FK_VehicleModels_VehicleBrands",
                        columns: x => new { x.BrandId, x.Kind },
                        principalTable: "VehicleBrands",
                        principalColumns: new[] { "Id", "Kind" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleModelYears",
                columns: table => new
                {
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: false),
                    Source = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleModelYears", x => new { x.ModelId, x.Year, x.Kind });
                    table.CheckConstraint("CK_VehicleModelYears_Kind", "[Kind] IN ('car', 'moto')");
                    table.ForeignKey(
                        name: "FK_VehicleModelYears_VehicleModels",
                        columns: x => new { x.ModelId, x.Kind },
                        principalTable: "VehicleModels",
                        principalColumns: new[] { "Id", "Kind" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: false),
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Source = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleVersions", x => new { x.Id, x.Kind });
                    table.CheckConstraint("CK_VehicleVersions_Kind", "[Kind] IN ('car', 'moto')");
                    table.ForeignKey(
                        name: "FK_VehicleVersions_VehicleModelYears",
                        columns: x => new { x.ModelId, x.Year, x.Kind },
                        principalTable: "VehicleModelYears",
                        principalColumns: new[] { "ModelId", "Year", "Kind" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModels_BrandId_Kind",
                table: "VehicleModels",
                columns: new[] { "BrandId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModelYears_ModelId_Kind",
                table: "VehicleModelYears",
                columns: new[] { "ModelId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleVersions_ModelId_Year_Kind",
                table: "VehicleVersions",
                columns: new[] { "ModelId", "Year", "Kind" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleVersions");

            migrationBuilder.DropTable(
                name: "VehicleModelYears");

            migrationBuilder.DropTable(
                name: "VehicleModels");

            migrationBuilder.DropTable(
                name: "VehicleBrands");
        }
    }
}
