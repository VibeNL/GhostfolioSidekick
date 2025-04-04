using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick
{
	public class CleanupDatabaseTask(
		ILogger<CleanupDatabaseTask> logger,
		DatabaseContext dbContext) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.CleanupDatabase;

		public TimeSpan ExecutionFrequency => Frequencies.Daily;
		
		public bool ExceptionsAreFatal => true;
		
		public Task DoWork()
		{
			return CleanupDatabase();
		}

		private async Task CleanupDatabase()
		{
			logger.LogInformation("Cleanup database...");
			await dbContext.ExecutePragma("PRAGMA integrity_check;");
			await dbContext.Database.ExecuteSqlRawAsync("VACUUM;");
			logger.LogInformation("Database cleaned.");
		}
	}
}