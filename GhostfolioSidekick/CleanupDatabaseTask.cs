﻿using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;

namespace GhostfolioSidekick
{
	public class CleanupDatabaseTask(
		ILogger<CleanupDatabaseTask> logger,
		DatabaseContext dbContext) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.CleanupDatabase;

		public TimeSpan ExecutionFrequency => TimeSpan.FromDays(1);

		public Task DoWork()
		{
			return CleanupDatabase();
		}

		private async Task CleanupDatabase()
		{
			logger.LogInformation("Cleanup database...");
			await dbContext.Database.ExecuteSqlRawAsync("VACUUM;");
			logger.LogInformation("Database cleaned.");
		}
	}
}