using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenamedCalculatedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CalculatedUnitPriceSource",
                table: "Activities",
                newName: "AdjustedUnitPriceSource");

            migrationBuilder.RenameColumn(
                name: "CalculatedUnitPrice",
                table: "Activities",
                newName: "AdjustedUnitPrice");

            migrationBuilder.RenameColumn(
                name: "CalculatedQuantity",
                table: "Activities",
                newName: "AdjustedQuantity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AdjustedUnitPriceSource",
                table: "Activities",
                newName: "CalculatedUnitPriceSource");

            migrationBuilder.RenameColumn(
                name: "AdjustedUnitPrice",
                table: "Activities",
                newName: "CalculatedUnitPrice");

            migrationBuilder.RenameColumn(
                name: "AdjustedQuantity",
                table: "Activities",
                newName: "CalculatedQuantity");
        }
    }
}
