using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTotalTransactionValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencyTotalTransactionAmount",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "TotalTransactionAmount",
                table: "Activities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrencyTotalTransactionAmount",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalTransactionAmount",
                table: "Activities",
                type: "TEXT",
                nullable: true);
        }
    }
}
