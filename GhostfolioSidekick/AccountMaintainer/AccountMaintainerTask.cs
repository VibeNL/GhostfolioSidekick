using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.AccountMaintainer
{
	public class AccountMaintainerTask(
		ILogger<AccountMaintainerTask> logger,
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		IApplicationSettings applicationSettings) : IScheduledWork
	{
		private readonly ILogger<AccountMaintainerTask> logger = logger ?? throw new ArgumentNullException(nameof(logger));
		private readonly IApplicationSettings applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));

		public TaskPriority Priority => TaskPriority.AccountMaintainer;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

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
				else
				{
					await UpdateAccount(databaseContext, account, accountConfig, platforms?.SingleOrDefault(x => x.Name == accountConfig.Platform));
				}
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
				SyncActivities = accountConfig.SyncActivities,
				SyncBalance = accountConfig.SyncBalance
			});
			await databaseContext.SaveChangesAsync();
		}

		private async Task UpdateAccount(DatabaseContext databaseContext, Account account, AccountConfiguration accountConfig, PlatformConfiguration? platformConfiguration)
		{
			bool hasChanges = false;

			// Update sync settings
			if (account.SyncActivities != accountConfig.SyncActivities)
			{
				account.SyncActivities = accountConfig.SyncActivities;
				hasChanges = true;
			}

			if (account.SyncBalance != accountConfig.SyncBalance)
			{
				account.SyncBalance = accountConfig.SyncBalance;
				hasChanges = true;
			}

			// Update comment if different
			if (account.Comment != accountConfig.Comment)
			{
				account.Comment = accountConfig.Comment;
				hasChanges = true;
			}

			// Update platform if different
			var platform = await CreateOrUpdatePlatform(databaseContext, platformConfiguration);
			if (account.Platform?.Name != platform?.Name)
			{
				account.Platform = platform;
				hasChanges = true;
			}

			if (hasChanges)
			{
				await databaseContext.SaveChangesAsync();
			}
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
				catch
				{
					// Running against a managed instance?
					applicationSettings.AllowAdminCalls = false;
				}
			}
			else
			{
				// Update platform if URL has changed
				if (platform.Url != platformConfiguration.Url)
				{
					platform.Url = platformConfiguration.Url;
					// Entity Framework will track this change automatically
				}
			}

			return await databaseContext.Platforms.FirstOrDefaultAsync(x => x.Name == platformConfiguration.Name);
		}
	}
}