using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalDataCacheEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalDataCacheEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    DataJson = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalDataCacheEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalDataCacheEntry_ExpiresAt",
                table: "ExternalDataCacheEntries",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalDataCacheEntry_Key",
                table: "ExternalDataCacheEntries",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalDataCacheEntries");
        }
    }
}
