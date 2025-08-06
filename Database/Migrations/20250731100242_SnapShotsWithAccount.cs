using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class SnapShotsWithAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			// Delete old snapshots without account ID
			migrationBuilder.Sql("DELETE FROM CalculatedSnapshots");

			migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "CalculatedSnapshots",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "CalculatedSnapshots");
        }
    }
}
