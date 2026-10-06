using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GazetaMarketplace.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordRecoveryAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PasswordRecoveryAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Ip = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordRecoveryAttempts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordRecoveryAttempts_Email_RequestedAt",
                table: "PasswordRecoveryAttempts",
                columns: new[] { "Email", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordRecoveryAttempts_Ip_RequestedAt",
                table: "PasswordRecoveryAttempts",
                columns: new[] { "Ip", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordRecoveryAttempts_RequestedAt",
                table: "PasswordRecoveryAttempts",
                column: "RequestedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordRecoveryAttempts");
        }
    }
}
