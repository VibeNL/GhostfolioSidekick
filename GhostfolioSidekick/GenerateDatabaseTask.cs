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
			await dbContext.ExecutePragma("PRAGMA synchronous=FULL");
			await dbContext.ExecutePragma("PRAGMA fullfsync=ON");
			logger.LogInformation("Database generated / updated.");
		}
	}
}