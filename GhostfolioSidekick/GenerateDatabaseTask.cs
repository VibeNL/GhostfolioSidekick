using GhostfolioSidekick.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace GhostfolioSidekick
{
	public class GenerateDatabaseTask(
		ILogger<GenerateDatabaseTask> logger,
		DbContext dbContext) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.GenerateDatabaseTask;

		public TimeSpan ExecutionFrequency => TimeSpan.MaxValue;

		public Task DoWork()
		{
			return GenerateDatabase();
		}

		private async Task GenerateDatabase()
		{
			logger.LogInformation("Generating / Updating database...");
			await dbContext.Database.MigrateAsync().ConfigureAwait(false);
			logger.LogInformation("Database generated / updated.");
		}
	}
}