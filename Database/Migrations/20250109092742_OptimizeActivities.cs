using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(@"DELETE FROM Activities");

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
                name: "RepayBondActivity");

            migrationBuilder.DropTable(
                name: "SendAndReceiveActivity");

            migrationBuilder.DropTable(
                name: "StakingRewardActivity");

            migrationBuilder.DropTable(
                name: "ValuableActivity");

            migrationBuilder.DropTable(
                name: "ActivityWithQuantityAndUnitPrice");

            migrationBuilder.AddColumn<decimal>(
                name: "AdjustedQuantity",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AdjustedUnitPrice",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyAdjustedUnitPrice",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyAmount",
                table: "Activities",
                type: "TEXT",
                nullable: true);

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

            migrationBuilder.AddColumn<string>(
                name: "CurrencyTotalTransactionAmount",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyUnitPrice",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Activities",
                type: "TEXT",
                maxLength: 34,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalRepayAmount",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalTransactionAmount",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BuySellActivityFees",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fees = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyFees = table.Column<string>(type: "TEXT", nullable: false),
                    BuySellActivityId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuySellActivityFees", x => x.ID);
                    table.ForeignKey(
                        name: "FK_BuySellActivityFees_Activities_BuySellActivityId",
                        column: x => x.BuySellActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BuySellActivityTaxes",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Taxes = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyTaxes = table.Column<string>(type: "TEXT", nullable: false),
                    BuySellActivityId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuySellActivityTaxes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_BuySellActivityTaxes_Activities_BuySellActivityId",
                        column: x => x.BuySellActivityId,
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
                    Amount = table.Column<decimal>(type: "TEXT", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", nullable: true),
                    ActivityWithQuantityAndUnitPriceId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculatedPriceTrace", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CalculatedPriceTrace_Activities_ActivityWithQuantityAndUnitPriceId",
                        column: x => x.ActivityWithQuantityAndUnitPriceId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DividendActivityFees",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fees = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyFees = table.Column<string>(type: "TEXT", nullable: false),
                    DividendActivityId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DividendActivityFees", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DividendActivityFees_Activities_DividendActivityId",
                        column: x => x.DividendActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DividendActivityTaxes",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Taxes = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyTaxes = table.Column<string>(type: "TEXT", nullable: false),
                    DividendActivityId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DividendActivityTaxes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DividendActivityTaxes_Activities_DividendActivityId",
                        column: x => x.DividendActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "SendAndReceiveActivityFees",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fees = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyFees = table.Column<string>(type: "TEXT", nullable: false),
                    SendAndReceiveActivityId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SendAndReceiveActivityFees", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SendAndReceiveActivityFees_Activities_SendAndReceiveActivityId",
                        column: x => x.SendAndReceiveActivityId,
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
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PartialSymbolIdentifierActivity_PartialSymbolIdentifiers_PartialSymbolIdentifierId",
                        column: x => x.PartialSymbolIdentifierId,
                        principalTable: "PartialSymbolIdentifiers",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuySellActivityFees_BuySellActivityId",
                table: "BuySellActivityFees",
                column: "BuySellActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_BuySellActivityTaxes_BuySellActivityId",
                table: "BuySellActivityTaxes",
                column: "BuySellActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_CalculatedPriceTrace_ActivityWithQuantityAndUnitPriceId",
                table: "CalculatedPriceTrace",
                column: "ActivityWithQuantityAndUnitPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_DividendActivityFees_DividendActivityId",
                table: "DividendActivityFees",
                column: "DividendActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_DividendActivityTaxes_DividendActivityId",
                table: "DividendActivityTaxes",
                column: "DividendActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_PartialSymbolIdentifierActivity_ActivityId",
                table: "PartialSymbolIdentifierActivity",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_PartialSymbolIdentifierActivity_PartialSymbolIdentifierId",
                table: "PartialSymbolIdentifierActivity",
                column: "PartialSymbolIdentifierId");

            migrationBuilder.CreateIndex(
                name: "IX_SendAndReceiveActivityFees_SendAndReceiveActivityId",
                table: "SendAndReceiveActivityFees",
                column: "SendAndReceiveActivityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "PartialSymbolIdentifierActivity");

            migrationBuilder.DropTable(
                name: "SendAndReceiveActivityFees");

            migrationBuilder.DropTable(
                name: "PartialSymbolIdentifiers");

            migrationBuilder.DropColumn(
                name: "AdjustedQuantity",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "AdjustedUnitPrice",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CurrencyAdjustedUnitPrice",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CurrencyAmount",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CurrencyPrice",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CurrencyTotalRepayAmount",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CurrencyTotalTransactionAmount",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CurrencyUnitPrice",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "TotalRepayAmount",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "TotalTransactionAmount",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "Activities");

            migrationBuilder.CreateTable(
                name: "ActivityWithQuantityAndUnitPrice",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AdjustedQuantity = table.Column<decimal>(type: "TEXT", nullable: true),
                    AdjustedUnitPrice = table.Column<string>(type: "TEXT", nullable: true),
                    AdjustedUnitPriceSource = table.Column<string>(type: "TEXT", nullable: false),
                    PartialSymbolIdentifiers = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<string>(type: "TEXT", nullable: true)
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
                    Amount = table.Column<string>(type: "TEXT", nullable: false),
                    Fees = table.Column<string>(type: "TEXT", nullable: false),
                    PartialSymbolIdentifiers = table.Column<string>(type: "TEXT", nullable: false),
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
        }
    }
}
