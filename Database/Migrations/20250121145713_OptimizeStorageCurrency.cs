using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeStorageCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BuySellActivityFees_Activities_BuySellActivityId",
                table: "BuySellActivityFees");

            migrationBuilder.DropForeignKey(
                name: "FK_BuySellActivityTaxes_Activities_BuySellActivityId",
                table: "BuySellActivityTaxes");

            migrationBuilder.DropForeignKey(
                name: "FK_CalculatedPriceTrace_Activities_ActivityWithQuantityAndUnitPriceId",
                table: "CalculatedPriceTrace");

            migrationBuilder.DropForeignKey(
                name: "FK_DividendActivityFees_Activities_DividendActivityId",
                table: "DividendActivityFees");

            migrationBuilder.DropForeignKey(
                name: "FK_SendAndReceiveActivityFees_Activities_SendAndReceiveActivityId",
                table: "SendAndReceiveActivityFees");

            migrationBuilder.DropTable(
                name: "DividendActivityTaxes");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "SendAndReceiveActivityFees",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "Fees",
                table: "SendAndReceiveActivityFees",
                newName: "Money");

            migrationBuilder.RenameColumn(
                name: "CurrencyFees",
                table: "SendAndReceiveActivityFees",
                newName: "CurrencyMoney");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "DividendActivityFees",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "Fees",
                table: "DividendActivityFees",
                newName: "Money");

            migrationBuilder.RenameColumn(
                name: "CurrencyFees",
                table: "DividendActivityFees",
                newName: "CurrencyMoney");

            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "CalculatedPriceTrace",
                newName: "CurrencyNewPrice");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "CalculatedPriceTrace",
                newName: "NewPrice");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "BuySellActivityTaxes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "Taxes",
                table: "BuySellActivityTaxes",
                newName: "Money");

            migrationBuilder.RenameColumn(
                name: "CurrencyTaxes",
                table: "BuySellActivityTaxes",
                newName: "CurrencyMoney");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "BuySellActivityFees",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "Fees",
                table: "BuySellActivityFees",
                newName: "Money");

            migrationBuilder.RenameColumn(
                name: "CurrencyFees",
                table: "BuySellActivityFees",
                newName: "CurrencyMoney");

            migrationBuilder.AlterColumn<long>(
                name: "SendAndReceiveActivityId",
                table: "SendAndReceiveActivityFees",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "SendAndReceiveActivityFees",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Sqlite:Autoincrement", true)
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<long>(
                name: "DividendActivityId",
                table: "DividendActivityFees",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "DividendActivityFees",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Sqlite:Autoincrement", true)
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<long>(
                name: "ActivityWithQuantityAndUnitPriceId",
                table: "CalculatedPriceTrace",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "CurrencyNewPrice",
                table: "CalculatedPriceTrace",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "NewPrice",
                table: "CalculatedPriceTrace",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "BuySellActivityId",
                table: "BuySellActivityTaxes",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "BuySellActivityTaxes",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Sqlite:Autoincrement", true)
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<long>(
                name: "BuySellActivityId",
                table: "BuySellActivityFees",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "BuySellActivityFees",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Sqlite:Autoincrement", true)
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.CreateTable(
                name: "DividendActivityTaxex",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DividendActivityId = table.Column<long>(type: "INTEGER", nullable: true),
                    Money = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyMoney = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DividendActivityTaxex", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DividendActivityTaxex_Activities_DividendActivityId",
                        column: x => x.DividendActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DividendActivityTaxex_DividendActivityId",
                table: "DividendActivityTaxex",
                column: "DividendActivityId");

            migrationBuilder.AddForeignKey(
                name: "FK_BuySellActivityFees_Activities_BuySellActivityId",
                table: "BuySellActivityFees",
                column: "BuySellActivityId",
                principalTable: "Activities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BuySellActivityTaxes_Activities_BuySellActivityId",
                table: "BuySellActivityTaxes",
                column: "BuySellActivityId",
                principalTable: "Activities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CalculatedPriceTrace_Activities_ActivityWithQuantityAndUnitPriceId",
                table: "CalculatedPriceTrace",
                column: "ActivityWithQuantityAndUnitPriceId",
                principalTable: "Activities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DividendActivityFees_Activities_DividendActivityId",
                table: "DividendActivityFees",
                column: "DividendActivityId",
                principalTable: "Activities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SendAndReceiveActivityFees_Activities_SendAndReceiveActivityId",
                table: "SendAndReceiveActivityFees",
                column: "SendAndReceiveActivityId",
                principalTable: "Activities",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BuySellActivityFees_Activities_BuySellActivityId",
                table: "BuySellActivityFees");

            migrationBuilder.DropForeignKey(
                name: "FK_BuySellActivityTaxes_Activities_BuySellActivityId",
                table: "BuySellActivityTaxes");

            migrationBuilder.DropForeignKey(
                name: "FK_CalculatedPriceTrace_Activities_ActivityWithQuantityAndUnitPriceId",
                table: "CalculatedPriceTrace");

            migrationBuilder.DropForeignKey(
                name: "FK_DividendActivityFees_Activities_DividendActivityId",
                table: "DividendActivityFees");

            migrationBuilder.DropForeignKey(
                name: "FK_SendAndReceiveActivityFees_Activities_SendAndReceiveActivityId",
                table: "SendAndReceiveActivityFees");

            migrationBuilder.DropTable(
                name: "DividendActivityTaxex");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "SendAndReceiveActivityFees",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "Money",
                table: "SendAndReceiveActivityFees",
                newName: "Fees");

            migrationBuilder.RenameColumn(
                name: "CurrencyMoney",
                table: "SendAndReceiveActivityFees",
                newName: "CurrencyFees");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "DividendActivityFees",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "Money",
                table: "DividendActivityFees",
                newName: "Fees");

            migrationBuilder.RenameColumn(
                name: "CurrencyMoney",
                table: "DividendActivityFees",
                newName: "CurrencyFees");

            migrationBuilder.RenameColumn(
                name: "NewPrice",
                table: "CalculatedPriceTrace",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "CurrencyNewPrice",
                table: "CalculatedPriceTrace",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "BuySellActivityTaxes",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "Money",
                table: "BuySellActivityTaxes",
                newName: "Taxes");

            migrationBuilder.RenameColumn(
                name: "CurrencyMoney",
                table: "BuySellActivityTaxes",
                newName: "CurrencyTaxes");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "BuySellActivityFees",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "Money",
                table: "BuySellActivityFees",
                newName: "Fees");

            migrationBuilder.RenameColumn(
                name: "CurrencyMoney",
                table: "BuySellActivityFees",
                newName: "CurrencyFees");

            migrationBuilder.AlterColumn<long>(
                name: "SendAndReceiveActivityId",
                table: "SendAndReceiveActivityFees",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "SendAndReceiveActivityFees",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true)
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<long>(
                name: "DividendActivityId",
                table: "DividendActivityFees",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "DividendActivityFees",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true)
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<long>(
                name: "ActivityWithQuantityAndUnitPriceId",
                table: "CalculatedPriceTrace",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "CalculatedPriceTrace",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "CalculatedPriceTrace",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "BuySellActivityId",
                table: "BuySellActivityTaxes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "BuySellActivityTaxes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true)
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<long>(
                name: "BuySellActivityId",
                table: "BuySellActivityFees",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "BuySellActivityFees",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true)
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.CreateTable(
                name: "DividendActivityTaxes",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Taxes = table.Column<decimal>(type: "TEXT", nullable: false),
                    DividendActivityId = table.Column<long>(type: "INTEGER", nullable: false),
                    CurrencyTaxes = table.Column<string>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_DividendActivityTaxes_DividendActivityId",
                table: "DividendActivityTaxes",
                column: "DividendActivityId");

            migrationBuilder.AddForeignKey(
                name: "FK_BuySellActivityFees_Activities_BuySellActivityId",
                table: "BuySellActivityFees",
                column: "BuySellActivityId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BuySellActivityTaxes_Activities_BuySellActivityId",
                table: "BuySellActivityTaxes",
                column: "BuySellActivityId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CalculatedPriceTrace_Activities_ActivityWithQuantityAndUnitPriceId",
                table: "CalculatedPriceTrace",
                column: "ActivityWithQuantityAndUnitPriceId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DividendActivityFees_Activities_DividendActivityId",
                table: "DividendActivityFees",
                column: "DividendActivityId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SendAndReceiveActivityFees_Activities_SendAndReceiveActivityId",
                table: "SendAndReceiveActivityFees",
                column: "SendAndReceiveActivityId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
