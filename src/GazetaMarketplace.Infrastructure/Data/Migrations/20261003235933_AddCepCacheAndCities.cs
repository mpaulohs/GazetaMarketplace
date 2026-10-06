using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GazetaMarketplace.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCepCacheAndCities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CepCache",
                columns: table => new
                {
                    Cep = table.Column<string>(type: "char(8)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Uf = table.Column<string>(type: "char(2)", nullable: false),
                    IbgeCode = table.Column<int>(type: "int", nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CepCache", x => x.Cep);
                    table.CheckConstraint("CK_CepCache_Cep", "[Cep] NOT LIKE '%[^0-9]%' AND LEN([Cep]) = 8");
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    IbgeCode = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Uf = table.Column<string>(type: "char(2)", nullable: false),
                    NameSearch = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.IbgeCode);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Cities_Uf_NameSearch",
                table: "Cities",
                columns: new[] { "Uf", "NameSearch" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CepCache");

            migrationBuilder.DropTable(
                name: "Cities");
        }
    }
}
