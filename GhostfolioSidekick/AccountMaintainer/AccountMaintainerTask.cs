using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.AccountMaintainer
{
	public class AccountMaintainerTask : IScheduledWork
	{
		private readonly ILogger<AccountMaintainerTask> logger;
		private readonly IAccountService api;
		private readonly IApplicationSettings applicationSettings;

		public TaskPriority Priority => TaskPriority.AccountCreation;

		public AccountMaintainerTask(
			ILogger<AccountMaintainerTask> logger,
			IAccountService api,
			IApplicationSettings applicationSettings)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.api = api ?? throw new ArgumentNullException(nameof(api));
			this.applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
		}

		public async Task DoWork()
		{
			logger.LogInformation($"{nameof(AccountMaintainerTask)} Starting to do work");

			try
			{
				await AddOrUpdateAccountsAndPlatforms();
			}
			catch
			{
				logger.LogError($"{nameof(AccountMaintainerTask)} Failed");
				return;
			}

			logger.LogInformation($"{nameof(AccountMaintainerTask)} Done");
		}

		private async Task AddOrUpdateAccountsAndPlatforms()
		{
			var platforms = applicationSettings.ConfigurationInstance.Platforms;
			var accounts = applicationSettings.ConfigurationInstance.Accounts;
			var existingAccounts = await api.GetAllAccounts();

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

			await api.CreateAccount(new Account(
				accountConfig.Name,
				new Balance(new Money(new Currency(accountConfig.Currency), 0)))
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

			var platform = await api.GetPlatformByName(platformConfiguration.Name);

			if (platform == null)
			{
				try
				{
					await api.CreatePlatform(new Platform(platformConfiguration.Name)
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
			return await api.GetPlatformByName(platformConfiguration.Name);
		}
	}
}