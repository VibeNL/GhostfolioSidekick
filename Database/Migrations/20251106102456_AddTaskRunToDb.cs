using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskRunToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskRuns",
                columns: table => new
                {
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    LastUpdate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Scheduled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    NextSchedule = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    InProgress = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastException = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskRuns", x => x.Type);
                });

            migrationBuilder.CreateTable(
                name: "TaskRunLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskRunType = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskRunLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskRunLogs_TaskRuns_TaskRunType",
                        column: x => x.TaskRunType,
                        principalTable: "TaskRuns",
                        principalColumn: "Type",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskRunLogs_TaskRunType",
                table: "TaskRunLogs",
                column: "TaskRunType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskRunLogs");

            migrationBuilder.DropTable(
                name: "TaskRuns");
        }
    }
}
