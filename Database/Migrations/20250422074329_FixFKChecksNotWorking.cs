using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
	/// <inheritdoc />
	public partial class FixFKChecksNotWorking : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// Delete records with null ActivityId
			migrationBuilder.Sql("DELETE FROM SendAndReceiveActivityFees WHERE ActivityId IS NULL");
			migrationBuilder.Sql("DELETE FROM DividendActivityTaxes WHERE ActivityId IS NULL");
			migrationBuilder.Sql("DELETE FROM DividendActivityFees WHERE ActivityId IS NULL");
			migrationBuilder.Sql("DELETE FROM CalculatedPriceTrace WHERE ActivityId IS NULL");
			migrationBuilder.Sql("DELETE FROM BuySellActivityTaxes WHERE ActivityId IS NULL");
			migrationBuilder.Sql("DELETE FROM BuySellActivityFees WHERE ActivityId IS NULL");

			// Delete records with null AccountId
			migrationBuilder.Sql("DELETE FROM Balances WHERE AccountId IS NULL");

			migrationBuilder.DropForeignKey(
				name: "FK_Balances_Accounts_AccountId",
				table: "Balances");

			migrationBuilder.AlterColumn<long>(
				name: "ActivityId",
				table: "SendAndReceiveActivityFees",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0L,
				oldClrType: typeof(long),
				oldType: "INTEGER",
				oldNullable: true);

			migrationBuilder.AlterColumn<long>(
				name: "ActivityId",
				table: "DividendActivityTaxes",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0L,
				oldClrType: typeof(long),
				oldType: "INTEGER",
				oldNullable: true);

			migrationBuilder.AlterColumn<long>(
				name: "ActivityId",
				table: "DividendActivityFees",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0L,
				oldClrType: typeof(long),
				oldType: "INTEGER",
				oldNullable: true);

			migrationBuilder.AlterColumn<long>(
				name: "ActivityId",
				table: "CalculatedPriceTrace",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0L,
				oldClrType: typeof(long),
				oldType: "INTEGER",
				oldNullable: true);

			migrationBuilder.AlterColumn<long>(
				name: "ActivityId",
				table: "BuySellActivityTaxes",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0L,
				oldClrType: typeof(long),
				oldType: "INTEGER",
				oldNullable: true);

			migrationBuilder.AlterColumn<long>(
				name: "ActivityId",
				table: "BuySellActivityFees",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0L,
				oldClrType: typeof(long),
				oldType: "INTEGER",
				oldNullable: true);

			migrationBuilder.AlterColumn<int>(
				name: "AccountId",
				table: "Balances",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0,
				oldClrType: typeof(int),
				oldType: "INTEGER",
				oldNullable: true);

			migrationBuilder.AddForeignKey(
				name: "FK_Balances_Accounts_AccountId",
				table: "Balances",
				column: "AccountId",
				principalTable: "Accounts",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_Balances_Accounts_AccountId",
				table: "Balances");

			migrationBuilder.AlterColumn<long>(
				name: "ActivityId",
				table: "SendAndReceiveActivityFees",
				type: "INTEGER",
				nullable: true,
				oldClrType: typeof(long),
				oldType: "INTEGER");

			migrationBuilder.AlterColumn<long>(
				name: "ActivityId",
				table: "DividendActivityTaxes",
				type: "INTEGER",
				nullable: true,
				oldClrType: typeof(long),
				oldType: "INTEGER");

			migrationBuilder.AlterColumn<long>(
				name: "ActivityId",
				table: "DividendActivityFees",
				type: "INTEGER",
				nullable: true,
				oldClrType: typeof(long),
				oldType: "INTEGER");

			migrationBuilder.AlterColumn<long>(
				name: "ActivityId",
				table: "CalculatedPriceTrace",
				type: "INTEGER",
				nullable: true,
				oldClrType: typeof(long),
				oldType: "INTEGER");

			migrationBuilder.AlterColumn<long>(
				name: "ActivityId",
				table: "BuySellActivityTaxes",
				type: "INTEGER",
				nullable: true,
				oldClrType: typeof(long),
				oldType: "INTEGER");

			migrationBuilder.AlterColumn<long>(
				name: "ActivityId",
				table: "BuySellActivityFees",
				type: "INTEGER",
				nullable: true,
				oldClrType: typeof(long),
				oldType: "INTEGER");

			migrationBuilder.AlterColumn<int>(
				name: "AccountId",
				table: "Balances",
				type: "INTEGER",
				nullable: true,
				oldClrType: typeof(int),
				oldType: "INTEGER");

			migrationBuilder.AddForeignKey(
				name: "FK_Balances_Accounts_AccountId",
				table: "Balances",
				column: "AccountId",
				principalTable: "Accounts",
				principalColumn: "Id");
		}
	}
}
