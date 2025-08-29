using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class SplitActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql("DELETE FROM Activities");

			migrationBuilder.DropTable(
                name: "BuySellActivityFees");

            migrationBuilder.DropTable(
                name: "BuySellActivityTaxes");

            migrationBuilder.DropTable(
                name: "SendAndReceiveActivityFees");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuyActivityFees");

            migrationBuilder.DropTable(
                name: "BuyActivityTaxes");

            migrationBuilder.DropTable(
                name: "ReceiveActivityFees");

            migrationBuilder.DropTable(
                name: "SellActivityFees");

            migrationBuilder.DropTable(
                name: "SellActivityTaxes");

            migrationBuilder.DropTable(
                name: "SendActivityFees");

            migrationBuilder.CreateTable(
                name: "BuySellActivityFees",
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
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: false),
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
                name: "SendAndReceiveActivityFees",
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
                    table.PrimaryKey("PK_SendAndReceiveActivityFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SendAndReceiveActivityFees_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuySellActivityFees_ActivityId",
                table: "BuySellActivityFees",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_BuySellActivityTaxes_ActivityId",
                table: "BuySellActivityTaxes",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_SendAndReceiveActivityFees_ActivityId",
                table: "SendAndReceiveActivityFees",
                column: "ActivityId");
        }
    }
}
