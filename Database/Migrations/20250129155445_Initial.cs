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
                name: "PartialSymbolIdentifiers",
                columns: table => new
                {
                    ID = table.Column<long>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Identifier = table.Column<string>(type: "TEXT", nullable: false),
                    AllowedAssetClasses = table.Column<string>(type: "TEXT", nullable: true),
                    AllowedAssetSubClasses = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartialSymbolIdentifiers", x => x.ID);
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
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    AssetClass = table.Column<string>(type: "TEXT", nullable: false),
                    AssetSubClass = table.Column<string>(type: "TEXT", nullable: true),
                    ISIN = table.Column<string>(type: "TEXT", nullable: true),
                    Identifiers = table.Column<string>(type: "TEXT", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    CountryWeight = table.Column<string>(type: "TEXT", nullable: false),
                    SectorWeights = table.Column<string>(type: "TEXT", nullable: false),
                    HoldingId = table.Column<int>(type: "INTEGER", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", nullable: false)
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
                    TradingVolume = table.Column<decimal>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SymbolProfileDataSource = table.Column<string>(type: "TEXT", nullable: true),
                    SymbolProfileSymbol = table.Column<string>(type: "TEXT", nullable: true),
                    Close = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyClose = table.Column<string>(type: "TEXT", nullable: false),
                    High = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyHigh = table.Column<string>(type: "TEXT", nullable: false),
                    Low = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyLow = table.Column<string>(type: "TEXT", nullable: false),
                    Open = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyOpen = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketData", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MarketData_SymbolProfiles_SymbolProfileSymbol_SymbolProfileDataSource",
                        columns: x => new { x.SymbolProfileSymbol, x.SymbolProfileDataSource },
                        principalTable: "SymbolProfiles",
                        principalColumns: ["Symbol", "DataSource"],
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
                        principalColumns: ["Symbol", "DataSource"],
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
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 34, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: true),
                    AdjustedQuantity = table.Column<decimal>(type: "TEXT", nullable: true),
                    AdjustedUnitPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    CurrencyAdjustedUnitPrice = table.Column<string>(type: "TEXT", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    CurrencyUnitPrice = table.Column<string>(type: "TEXT", nullable: true),
                    TotalTransactionAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    CurrencyTotalTransactionAmount = table.Column<string>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: true),
                    CurrencyAmount = table.Column<string>(type: "TEXT", nullable: true),
                    Price = table.Column<decimal>(type: "TEXT", nullable: true),
                    CurrencyPrice = table.Column<string>(type: "TEXT", nullable: true),
                    TotalRepayAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    CurrencyTotalRepayAmount = table.Column<string>(type: "TEXT", nullable: true)
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
                    AccountId = table.Column<int>(type: "INTEGER", nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false)
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
                name: "BuySellActivityFees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: true),
                    Money = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyMoney = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuySellActivityFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuySellActivityFees_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BuySellActivityTaxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: true),
                    Money = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyMoney = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuySellActivityTaxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuySellActivityTaxes_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalculatedPriceTrace",
                columns: table => new
                {
                    ID = table.Column<long>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: true),
                    NewPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyNewPrice = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculatedPriceTrace", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CalculatedPriceTrace_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DividendActivityFees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: true),
                    Money = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyMoney = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DividendActivityFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DividendActivityFees_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DividendActivityTaxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: true),
                    Money = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyMoney = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DividendActivityTaxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DividendActivityTaxes_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartialSymbolIdentifierActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: true),
                    PartialSymbolIdentifierId = table.Column<long>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartialSymbolIdentifierActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartialSymbolIdentifierActivity_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartialSymbolIdentifierActivity_PartialSymbolIdentifiers_PartialSymbolIdentifierId",
                        column: x => x.PartialSymbolIdentifierId,
                        principalTable: "PartialSymbolIdentifiers",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SendAndReceiveActivityFees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: true),
                    Money = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyMoney = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SendAndReceiveActivityFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SendAndReceiveActivityFees_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
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
                columns: ["AccountId", "Date"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuySellActivityFees_ActivityId",
                table: "BuySellActivityFees",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_BuySellActivityTaxes_ActivityId",
                table: "BuySellActivityTaxes",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_CalculatedPriceTrace_ActivityId",
                table: "CalculatedPriceTrace",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_DividendActivityFees_ActivityId",
                table: "DividendActivityFees",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_DividendActivityTaxes_ActivityId",
                table: "DividendActivityTaxes",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketData_SymbolProfileSymbol_SymbolProfileDataSource",
                table: "MarketData",
                columns: ["SymbolProfileSymbol", "SymbolProfileDataSource"]);

            migrationBuilder.CreateIndex(
                name: "IX_PartialSymbolIdentifierActivity_ActivityId",
                table: "PartialSymbolIdentifierActivity",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_PartialSymbolIdentifierActivity_PartialSymbolIdentifierId",
                table: "PartialSymbolIdentifierActivity",
                column: "PartialSymbolIdentifierId");

            migrationBuilder.CreateIndex(
                name: "IX_SendAndReceiveActivityFees_ActivityId",
                table: "SendAndReceiveActivityFees",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_StockSplits_SymbolProfileSymbol_SymbolProfileDataSource",
                table: "StockSplits",
                columns: ["SymbolProfileSymbol", "SymbolProfileDataSource"]);

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
                name: "BuySellActivityFees");

            migrationBuilder.DropTable(
                name: "BuySellActivityTaxes");

            migrationBuilder.DropTable(
                name: "CalculatedPriceTrace");

            migrationBuilder.DropTable(
                name: "DividendActivityFees");

            migrationBuilder.DropTable(
                name: "DividendActivityTaxes");

            migrationBuilder.DropTable(
                name: "MarketData");

            migrationBuilder.DropTable(
                name: "PartialSymbolIdentifierActivity");

            migrationBuilder.DropTable(
                name: "SendAndReceiveActivityFees");

            migrationBuilder.DropTable(
                name: "StockSplits");

            migrationBuilder.DropTable(
                name: "PartialSymbolIdentifiers");

            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "SymbolProfiles");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Holdings");

            migrationBuilder.DropTable(
                name: "Platforms");
        }
    }
}
