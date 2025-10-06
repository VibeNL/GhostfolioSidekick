using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
	/// <inheritdoc />
	public partial class AddSyncSettingsToAccount : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<bool>(
				name: "SyncActivities",
				table: "Accounts",
				type: "INTEGER",
				nullable: false,
				defaultValue: true);

			migrationBuilder.AddColumn<bool>(
				name: "SyncBalance",
				table: "Accounts",
				type: "INTEGER",
				nullable: false,
				defaultValue: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "SyncActivities",
				table: "Accounts");

			migrationBuilder.DropColumn(
				name: "SyncBalance",
				table: "Accounts");
		}
	}
}
