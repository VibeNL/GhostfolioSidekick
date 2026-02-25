using GhostfolioSidekick.PortfolioViewer.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.ApiService.Controllers;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
    public class VersionController(DatabaseContext dbContext) : ControllerBase
    {
		[HttpGet]
        public IActionResult GetVersion()
        {
            return Ok(new { version = VersionInfo.Version });
        }

        [HttpGet("migration-status")]
        public async Task<IActionResult> GetMigrationStatus()
        {
            var applied = await dbContext.Database.GetAppliedMigrationsAsync();
            var pending = await dbContext.Database.GetPendingMigrationsAsync();
            var result = new MigrationStatusResponse
            {
                AppliedMigrations = [.. applied],
                PendingMigrations = [.. pending]
			};
            return Ok(result);
        }
    }
}
