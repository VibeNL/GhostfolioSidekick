using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class SwitchToGuidsForSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			// Delete all CalculatedSnapshots
			migrationBuilder.Sql("DELETE FROM CalculatedSnapshots");

			migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "CalculatedSnapshots",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "integer")
                .OldAnnotation("Sqlite:Autoincrement", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "CalculatedSnapshots",
                type: "integer",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT")
                .Annotation("Sqlite:Autoincrement", true);
        }
    }
}
