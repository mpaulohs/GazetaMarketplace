using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GazetaMarketplace.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateAuditEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActorId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    TargetType = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    TargetId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    PreviousValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Result = table.Column<byte>(type: "tinyint", nullable: false),
                    CorrelationId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_ActorId_OccurredAt",
                table: "AuditEntries",
                columns: new[] { "ActorId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_OccurredAt",
                table: "AuditEntries",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_TargetType_TargetId",
                table: "AuditEntries",
                columns: new[] { "TargetType", "TargetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries");
        }
    }
}
