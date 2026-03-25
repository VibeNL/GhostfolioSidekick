using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class CleanupEmptyPartialIdentifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove any rows where Identifier was stored as NULL or empty string,
            // which would cause the PartialSymbolIdentifier constructor to throw
            // during EF Core materialization.
            migrationBuilder.Sql(
                "DELETE FROM PartialSymbolIdentifiers WHERE Identifier IS NULL OR TRIM(Identifier) = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data deletion cannot be reversed.
        }
    }
}
