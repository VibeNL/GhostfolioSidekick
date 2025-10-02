using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPrimaryCurrencyTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql("DELETE FROM Activities");

			migrationBuilder.DropIndex(
                name: "IX_CalculatedSnapshots_HoldingAggregatedId",
                table: "CalculatedSnapshots");

            migrationBuilder.DropColumn(
                name: "CurrencyPrice",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CurrencyTotalRepayAmount",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "TotalRepayAmount",
                table: "Activities");

            migrationBuilder.CreateTable(
                name: "BalancePrimaryCurrencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Money = table.Column<decimal>(type: "TEXT", nullable: false),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false)
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
                    HoldingAggregatedId = table.Column<long>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    AverageCostPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrentUnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
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
                name: "IX_CalculatedSnapshots_AccountId_Date",
                table: "CalculatedSnapshots",
                columns: new[] { "AccountId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_CalculatedSnapshots_Date",
                table: "CalculatedSnapshots",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CalculatedSnapshots_HoldingAggregatedId_AccountId_Date",
                table: "CalculatedSnapshots",
                columns: new[] { "HoldingAggregatedId", "AccountId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_Date",
                table: "Activities",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_BalancePrimaryCurrencies_AccountId_Date",
                table: "BalancePrimaryCurrencies",
                columns: new[] { "AccountId", "Date" },
                unique: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BalancePrimaryCurrencies");

            migrationBuilder.DropTable(
                name: "CalculatedSnapshotPrimaryCurrencies");

            migrationBuilder.DropIndex(
                name: "IX_CalculatedSnapshots_AccountId_Date",
                table: "CalculatedSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_CalculatedSnapshots_Date",
                table: "CalculatedSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_CalculatedSnapshots_HoldingAggregatedId_AccountId_Date",
                table: "CalculatedSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_Activities_Date",
                table: "Activities");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyPrice",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyTotalRepayAmount",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalRepayAmount",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalculatedSnapshots_HoldingAggregatedId",
                table: "CalculatedSnapshots",
                column: "HoldingAggregatedId");
        }
    }
}
