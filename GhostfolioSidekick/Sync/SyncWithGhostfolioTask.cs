using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Sync
{
	internal class SyncWithGhostfolioTask() : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SyncWithGhostfolio;

		public TimeSpan ExecutionFrequency => TimeSpan.FromMinutes(5);

		public Task DoWork()
		{
			return Task.CompletedTask;
		}
	}
}
