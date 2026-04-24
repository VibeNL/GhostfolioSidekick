using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskRunStartEndTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndTime",
                table: "TaskRuns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartTime",
                table: "TaskRuns",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "TaskRuns");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "TaskRuns");
        }
    }
}
