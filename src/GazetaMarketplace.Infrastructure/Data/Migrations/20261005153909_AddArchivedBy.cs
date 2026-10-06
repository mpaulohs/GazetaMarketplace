using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GazetaMarketplace.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArchivedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                table: "Ads",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ads_ArchivedById",
                table: "Ads",
                column: "ArchivedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Ads_AspNetUsers_ArchivedById",
                table: "Ads",
                column: "ArchivedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ads_AspNetUsers_ArchivedById",
                table: "Ads");

            migrationBuilder.DropIndex(
                name: "IX_Ads_ArchivedById",
                table: "Ads");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                table: "Ads");
        }
    }
}
