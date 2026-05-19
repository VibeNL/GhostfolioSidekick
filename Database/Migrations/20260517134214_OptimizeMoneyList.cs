using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeMoneyList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuyActivityFees");

            migrationBuilder.DropTable(
                name: "BuyActivityTaxes");

            migrationBuilder.DropTable(
                name: "DividendActivityFees");

            migrationBuilder.DropTable(
                name: "DividendActivityTaxes");

            migrationBuilder.DropTable(
                name: "ReceiveActivityFees");

            migrationBuilder.DropTable(
                name: "SellActivityFees");

            migrationBuilder.DropTable(
                name: "SellActivityTaxes");

            migrationBuilder.DropTable(
                name: "SendActivityFees");

            migrationBuilder.RenameColumn(
                name: "TotalTransactionAmount",
                table: "Activities",
                newName: "TransactionAmount");

            migrationBuilder.RenameColumn(
                name: "CurrencyTotalTransactionAmount",
                table: "Activities",
                newName: "CurrencyTransactionAmount");

            migrationBuilder.AddColumn<string>(
                name: "Fees",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Taxes",
                table: "Activities",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fees",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Taxes",
                table: "Activities");

            migrationBuilder.RenameColumn(
                name: "TransactionAmount",
                table: "Activities",
                newName: "TotalTransactionAmount");

            migrationBuilder.RenameColumn(
                name: "CurrencyTransactionAmount",
                table: "Activities",
                newName: "CurrencyTotalTransactionAmount");

            migrationBuilder.CreateTable(
                name: "BuyActivityFees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: false),
                    Money = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyMoney = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuyActivityFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuyActivityFees_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BuyActivityTaxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: false),
                    Money = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyMoney = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuyActivityTaxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuyActivityTaxes_Activities_ActivityId",
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
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: false),
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
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: false),
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
                name: "ReceiveActivityFees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: false),
                    Money = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyMoney = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiveActivityFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiveActivityFees_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SellActivityFees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: false),
                    Money = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyMoney = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellActivityFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellActivityFees_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SellActivityTaxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: false),
                    Money = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyMoney = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellActivityTaxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellActivityTaxes_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SendActivityFees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: false),
                    Money = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyMoney = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SendActivityFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SendActivityFees_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuyActivityFees_ActivityId",
                table: "BuyActivityFees",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_BuyActivityTaxes_ActivityId",
                table: "BuyActivityTaxes",
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
                name: "IX_ReceiveActivityFees_ActivityId",
                table: "ReceiveActivityFees",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_SellActivityFees_ActivityId",
                table: "SellActivityFees",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_SellActivityTaxes_ActivityId",
                table: "SellActivityTaxes",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_SendActivityFees_ActivityId",
                table: "SendActivityFees",
                column: "ActivityId");
        }
    }
}
