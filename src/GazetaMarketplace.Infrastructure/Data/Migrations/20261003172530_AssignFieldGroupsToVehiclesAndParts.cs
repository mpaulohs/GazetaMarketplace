using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GazetaMarketplace.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AssignFieldGroupsToVehiclesAndParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "FieldGroup",
                value: "Parts");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 33,
                column: "FieldGroup",
                value: "Cars");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 34,
                column: "FieldGroup",
                value: "TrucksAndBuses");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 35,
                column: "FieldGroup",
                value: "TrucksAndBuses");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 36,
                column: "FieldGroup",
                value: "Motorcycles");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 37,
                column: "FieldGroup",
                value: "BoatsAndAircraft");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "FieldGroup",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 33,
                column: "FieldGroup",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 34,
                column: "FieldGroup",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 35,
                column: "FieldGroup",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 36,
                column: "FieldGroup",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 37,
                column: "FieldGroup",
                value: null);
        }
    }
}
