using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick
{
	public class GenerateDatabaseTask(
		ILogger<GenerateDatabaseTask> logger,
		DatabaseContext dbContext) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.GenerateDatabase;

		public TimeSpan ExecutionFrequency => TimeSpan.MaxValue;

		public Task DoWork()
		{
			return GenerateDatabase();
		}

		private async Task GenerateDatabase()
		{
			logger.LogInformation("Generating / Updating database...");

			await dbContext.Database.MigrateAsync().ConfigureAwait(false);
			await dbContext.ExecutePragma("PRAGMA synchronous=FULL;");
			await dbContext.ExecutePragma("PRAGMA fullfsync=ON;");
			await dbContext.ExecutePragma("PRAGMA journal_mode=DELETE;");
			
			logger.LogInformation("Do integrety checks and vacuum...");
			await dbContext.ExecutePragma("PRAGMA integrity_check;");
			await dbContext.Database.ExecuteSqlRawAsync("VACUUM;");

			logger.LogInformation("Database generated / updated.");
		}
	}
}