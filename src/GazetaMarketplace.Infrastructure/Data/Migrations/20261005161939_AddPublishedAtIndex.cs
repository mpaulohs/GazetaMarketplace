using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GazetaMarketplace.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Ads_Status_PublishedAt_Id",
                table: "Ads",
                columns: new[] { "Status", "PublishedAt", "Id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ads_Status_PublishedAt_Id",
                table: "Ads");
        }
    }
}
