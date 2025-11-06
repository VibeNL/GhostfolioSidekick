using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick
{
	public class CleanupDatabaseTask(
		DatabaseContext dbContext) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.CleanupDatabase;

		public TimeSpan ExecutionFrequency => Frequencies.Daily;

		public bool ExceptionsAreFatal => true;

		public string Name => "Cleanup Database";

		public async Task DoWork(ILogger logger)
		{
			logger.LogInformation("Cleanup database...");
			await dbContext.ExecutePragma("PRAGMA integrity_check;");
			await dbContext.Database.ExecuteSqlRawAsync("VACUUM;");
			logger.LogInformation("Database cleaned.");
		}
	}
}