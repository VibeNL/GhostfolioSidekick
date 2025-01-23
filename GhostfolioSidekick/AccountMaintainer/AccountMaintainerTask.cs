using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.AccountMaintainer
{
	public class AccountMaintainerTask : IScheduledWork
	{
		private readonly ILogger<AccountMaintainerTask> logger;
		private readonly IDbContextFactory<DatabaseContext> databaseContextFactory;
		private readonly IApplicationSettings applicationSettings;

		public TaskPriority Priority => TaskPriority.AccountMaintainer;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public AccountMaintainerTask(
			ILogger<AccountMaintainerTask> logger,
			IDbContextFactory<DatabaseContext> databaseContextFactory,
			IApplicationSettings applicationSettings)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.databaseContextFactory = databaseContextFactory;
			this.applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
		}

		public async Task DoWork()
		{
			logger.LogDebug("{Name} Starting to do work", nameof(AccountMaintainerTask));

			try
			{
				await AddOrUpdateAccountsAndPlatforms();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "{Name} Failed", nameof(AccountMaintainerTask));
				return;
			}

			logger.LogDebug("{Name} Done", nameof(AccountMaintainerTask));
		}

		private async Task AddOrUpdateAccountsAndPlatforms()
		{
			var platforms = applicationSettings.ConfigurationInstance.Platforms;
			var accounts = applicationSettings.ConfigurationInstance.Accounts;
			
			using var databaseContext = databaseContextFactory.CreateDbContext();
			var existingAccounts = await databaseContext.Accounts.ToListAsync();

			foreach (var accountConfig in accounts ?? Enumerable.Empty<AccountConfiguration>())
			{
				var account = existingAccounts.SingleOrDefault(x => x.Name == accountConfig.Name);

				if (account == null)
				{
					await CreateAccount(accountConfig, platforms?.SingleOrDefault(x => x.Name == accountConfig.Platform));
				}

				// TODO Update account
			}
		}

		private async Task CreateAccount(AccountConfiguration accountConfig, PlatformConfiguration? platformConfiguration)
		{
			using var databaseContext = databaseContextFactory.CreateDbContext();
			
			var platform = await CreateOrUpdatePlatform(databaseContext, platformConfiguration);
			await databaseContext.Accounts.AddAsync(new Account(accountConfig.Name)
			{
				Comment = accountConfig.Comment,
				Platform = platform,
			});
			await databaseContext.SaveChangesAsync();
		}

		private async Task<Platform?> CreateOrUpdatePlatform(DatabaseContext databaseContext, PlatformConfiguration? platformConfiguration)
		{
			if (platformConfiguration is null || !applicationSettings.AllowAdminCalls)
			{
				return null;
			}

			var platform = await databaseContext.Platforms.FirstOrDefaultAsync(x => x.Name == platformConfiguration.Name);

			if (platform == null)
			{
				try
				{
					await databaseContext.Platforms.AddAsync(new Platform(platformConfiguration.Name)
					{
						Url = platformConfiguration.Url,
					});
				}
				catch //(NotAuthorizedException)
				{
					// Running against a managed instance?
					applicationSettings.AllowAdminCalls = false;
				}
			}

			// TODO Update platform
			return await databaseContext.Platforms.FirstOrDefaultAsync(x => x.Name == platformConfiguration.Name);
		}
	}
}