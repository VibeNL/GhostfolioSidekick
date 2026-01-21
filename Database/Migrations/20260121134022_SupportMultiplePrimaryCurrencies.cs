using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class SupportMultiplePrimaryCurrencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BalancePrimaryCurrencies");

            migrationBuilder.DropTable(
                name: "CalculatedSnapshotPrimaryCurrencies");

            migrationBuilder.DropColumn(
                name: "CurrencyAverageCostPrice",
                table: "CalculatedSnapshots");

            migrationBuilder.DropColumn(
                name: "CurrencyCurrentUnitPrice",
                table: "CalculatedSnapshots");

            migrationBuilder.DropColumn(
                name: "CurrencyTotalInvested",
                table: "CalculatedSnapshots");

            migrationBuilder.RenameColumn(
                name: "CurrencyTotalValue",
                table: "CalculatedSnapshots",
                newName: "Currency");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "CalculatedSnapshots",
                newName: "CurrencyTotalValue");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyAverageCostPrice",
                table: "CalculatedSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCurrentUnitPrice",
                table: "CalculatedSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyTotalInvested",
                table: "CalculatedSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BalancePrimaryCurrencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Money = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BalancePrimaryCurrencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BalancePrimaryCurrencies_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalculatedSnapshotPrimaryCurrencies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageCostPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrentUnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    HoldingAggregatedId = table.Column<long>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalInvested = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalValue = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculatedSnapshotPrimaryCurrencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalculatedSnapshotPrimaryCurrencies_HoldingAggregateds_HoldingAggregatedId",
                        column: x => x.HoldingAggregatedId,
                        principalTable: "HoldingAggregateds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BalancePrimaryCurrencies_AccountId_Date",
                table: "BalancePrimaryCurrencies",
                columns: new[] { "AccountId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BalancePrimaryCurrencies_Date",
                table: "BalancePrimaryCurrencies",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CalculatedSnapshotPrimaryCurrencies_AccountId_Date",
                table: "CalculatedSnapshotPrimaryCurrencies",
                columns: new[] { "AccountId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_CalculatedSnapshotPrimaryCurrencies_Date",
                table: "CalculatedSnapshotPrimaryCurrencies",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CalculatedSnapshotPrimaryCurrencies_HoldingAggregatedId_AccountId_Date",
                table: "CalculatedSnapshotPrimaryCurrencies",
                columns: new[] { "HoldingAggregatedId", "AccountId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalculatedSnapshotPrimaryCurrencies_HoldingAggregatedId_Date",
                table: "CalculatedSnapshotPrimaryCurrencies",
                columns: new[] { "HoldingAggregatedId", "Date" });
        }
    }
}
