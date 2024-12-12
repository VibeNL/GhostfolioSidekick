using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Holdings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartialSymbolIdentifiers = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holdings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Platforms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Platforms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SymbolProfiles",
                columns: table => new
                {
                    Symbol = table.Column<string>(type: "TEXT", nullable: false),
                    DataSource = table.Column<string>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    AssetClass = table.Column<string>(type: "TEXT", nullable: false),
                    AssetSubClass = table.Column<string>(type: "TEXT", nullable: true),
                    ISIN = table.Column<string>(type: "TEXT", nullable: true),
                    Identifiers = table.Column<string>(type: "TEXT", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    CountryWeight = table.Column<string>(type: "TEXT", nullable: false),
                    SectorWeights = table.Column<string>(type: "TEXT", nullable: false),
                    HoldingId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SymbolProfiles", x => new { x.Symbol, x.DataSource });
                    table.ForeignKey(
                        name: "FK_SymbolProfiles_Holdings_HoldingId",
                        column: x => x.HoldingId,
                        principalTable: "Holdings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    PlatformId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Platforms_PlatformId",
                        column: x => x.PlatformId,
                        principalTable: "Platforms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MarketData",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Close = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyClose = table.Column<string>(type: "TEXT", nullable: false),
                    Open = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyOpen = table.Column<string>(type: "TEXT", nullable: false),
                    High = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyHigh = table.Column<string>(type: "TEXT", nullable: false),
                    Low = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyLow = table.Column<string>(type: "TEXT", nullable: false),
                    TradingVolume = table.Column<decimal>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SymbolProfileDataSource = table.Column<string>(type: "TEXT", nullable: true),
                    SymbolProfileSymbol = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketData", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MarketData_SymbolProfiles_SymbolProfileSymbol_SymbolProfileDataSource",
                        columns: x => new { x.SymbolProfileSymbol, x.SymbolProfileDataSource },
                        principalTable: "SymbolProfiles",
                        principalColumns: new[] { "Symbol", "DataSource" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockSplits",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    BeforeSplit = table.Column<decimal>(type: "TEXT", nullable: false),
                    AfterSplit = table.Column<decimal>(type: "TEXT", nullable: false),
                    SymbolProfileDataSource = table.Column<string>(type: "TEXT", nullable: true),
                    SymbolProfileSymbol = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockSplits", x => x.ID);
                    table.ForeignKey(
                        name: "FK_StockSplits_SymbolProfiles_SymbolProfileSymbol_SymbolProfileDataSource",
                        columns: x => new { x.SymbolProfileSymbol, x.SymbolProfileDataSource },
                        principalTable: "SymbolProfiles",
                        principalColumns: new[] { "Symbol", "DataSource" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    HoldingId = table.Column<int>(type: "INTEGER", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<string>(type: "TEXT", nullable: false),
                    SortingPriority = table.Column<int>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Activities_Holdings_HoldingId",
                        column: x => x.HoldingId,
                        principalTable: "Holdings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Balances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Balances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Balances_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ActivityWithQuantityAndUnitPrice",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartialSymbolIdentifiers = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<string>(type: "TEXT", nullable: true),
                    AdjustedQuantity = table.Column<decimal>(type: "TEXT", nullable: true),
                    AdjustedUnitPrice = table.Column<string>(type: "TEXT", nullable: true),
                    AdjustedUnitPriceSource = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityWithQuantityAndUnitPrice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityWithQuantityAndUnitPrice_Activities_Id",
                        column: x => x.Id,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CashDepositWithdrawalActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashDepositWithdrawalActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashDepositWithdrawalActivity_Activities_Id",
                        column: x => x.Id,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DividendActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fees = table.Column<string>(type: "TEXT", nullable: false),
                    PartialSymbolIdentifiers = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<string>(type: "TEXT", nullable: false),
                    Taxes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DividendActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DividendActivity_Activities_Id",
                        column: x => x.Id,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeeActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeeActivity_Activities_Id",
                        column: x => x.Id,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterestActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterestActivity_Activities_Id",
                        column: x => x.Id,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnownBalanceActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownBalanceActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnownBalanceActivity_Activities_Id",
                        column: x => x.Id,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LiabilityActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartialSymbolIdentifiers = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiabilityActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiabilityActivity_Activities_Id",
                        column: x => x.Id,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepayBondActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartialSymbolIdentifiers = table.Column<string>(type: "TEXT", nullable: false),
                    TotalRepayAmount = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepayBondActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepayBondActivity_Activities_Id",
                        column: x => x.Id,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValuableActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartialSymbolIdentifiers = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValuableActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValuableActivity_Activities_Id",
                        column: x => x.Id,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BuySellActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fees = table.Column<string>(type: "TEXT", nullable: false),
                    Taxes = table.Column<string>(type: "TEXT", nullable: false),
                    TotalTransactionAmount = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuySellActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuySellActivity_ActivityWithQuantityAndUnitPrice_Id",
                        column: x => x.Id,
                        principalTable: "ActivityWithQuantityAndUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GiftActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftActivity_ActivityWithQuantityAndUnitPrice_Id",
                        column: x => x.Id,
                        principalTable: "ActivityWithQuantityAndUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SendAndReceiveActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fees = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SendAndReceiveActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SendAndReceiveActivity_ActivityWithQuantityAndUnitPrice_Id",
                        column: x => x.Id,
                        principalTable: "ActivityWithQuantityAndUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StakingRewardActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StakingRewardActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StakingRewardActivity_ActivityWithQuantityAndUnitPrice_Id",
                        column: x => x.Id,
                        principalTable: "ActivityWithQuantityAndUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_PlatformId",
                table: "Accounts",
                column: "PlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_AccountId",
                table: "Activities",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_HoldingId",
                table: "Activities",
                column: "HoldingId");

            migrationBuilder.CreateIndex(
                name: "IX_Balances_AccountId_Date",
                table: "Balances",
                columns: new[] { "AccountId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketData_SymbolProfileSymbol_SymbolProfileDataSource",
                table: "MarketData",
                columns: new[] { "SymbolProfileSymbol", "SymbolProfileDataSource" });

            migrationBuilder.CreateIndex(
                name: "IX_StockSplits_SymbolProfileSymbol_SymbolProfileDataSource",
                table: "StockSplits",
                columns: new[] { "SymbolProfileSymbol", "SymbolProfileDataSource" });

            migrationBuilder.CreateIndex(
                name: "IX_SymbolProfiles_HoldingId",
                table: "SymbolProfiles",
                column: "HoldingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Balances");

            migrationBuilder.DropTable(
                name: "BuySellActivity");

            migrationBuilder.DropTable(
                name: "CashDepositWithdrawalActivity");

            migrationBuilder.DropTable(
                name: "DividendActivity");

            migrationBuilder.DropTable(
                name: "FeeActivity");

            migrationBuilder.DropTable(
                name: "GiftActivity");

            migrationBuilder.DropTable(
                name: "InterestActivity");

            migrationBuilder.DropTable(
                name: "KnownBalanceActivity");

            migrationBuilder.DropTable(
                name: "LiabilityActivity");

            migrationBuilder.DropTable(
                name: "MarketData");

            migrationBuilder.DropTable(
                name: "RepayBondActivity");

            migrationBuilder.DropTable(
                name: "SendAndReceiveActivity");

            migrationBuilder.DropTable(
                name: "StakingRewardActivity");

            migrationBuilder.DropTable(
                name: "StockSplits");

            migrationBuilder.DropTable(
                name: "ValuableActivity");

            migrationBuilder.DropTable(
                name: "ActivityWithQuantityAndUnitPrice");

            migrationBuilder.DropTable(
                name: "SymbolProfiles");

            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Holdings");

            migrationBuilder.DropTable(
                name: "Platforms");
        }
    }
}
