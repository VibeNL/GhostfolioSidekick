using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.AccountMaintainer
{
	public class AccountMaintainerTask : IScheduledWork
	{
		private readonly ILogger<AccountMaintainerTask> logger;
		private readonly IAccountRepository accountRepository;
		private readonly IApplicationSettings applicationSettings;

		public TaskPriority Priority => TaskPriority.AccountCreation;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public AccountMaintainerTask(
			ILogger<AccountMaintainerTask> logger,
			IAccountRepository accountRepository,
			IApplicationSettings applicationSettings)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
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
			var existingAccounts = await accountRepository.GetAllAccounts();

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
			var platform = await CreateOrUpdatePlatform(platformConfiguration);

			await accountRepository.AddAccount(new Account(accountConfig.Name)
			{
				Comment = accountConfig.Comment,
				Platform = platform,
			});
		}

		private async Task<Platform?> CreateOrUpdatePlatform(PlatformConfiguration? platformConfiguration)
		{
			if (platformConfiguration is null || !applicationSettings.AllowAdminCalls)
			{
				return null;
			}

			var platform = await accountRepository.GetPlatformByName(platformConfiguration.Name);

			if (platform == null)
			{
				try
				{
					await accountRepository.AddPlatform(new Platform(platformConfiguration.Name)
					{
						Url = platformConfiguration.Url,
					});
				}
				catch (NotAuthorizedException)
				{
					// Running against a managed instance?
					applicationSettings.AllowAdminCalls = false;
				}
			}

			// TODO Update platform
			return await accountRepository.GetPlatformByName(platformConfiguration.Name);
		}
	}
}