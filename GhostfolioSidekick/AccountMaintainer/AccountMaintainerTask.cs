using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.AccountMaintainer
{
	public class AccountMaintainerTask : IScheduledWork
	{
		private readonly ILogger<FileImporterTask> logger;
		private readonly IAccountManager api;
		private readonly IApplicationSettings applicationSettings;

		public int Priority => 1;

		public AccountMaintainerTask(
			ILogger<FileImporterTask> logger,
			IAccountManager api,
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

			foreach (var accountConfig in accounts ?? Enumerable.Empty<AccountConfiguration>())
			{
				var account = await api.GetAccountByName(accountConfig.Name);

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
			if (platformConfiguration is null)
			{
				return null;
			}

			var platform = await api.GetPlatformByName(platformConfiguration.Name);

			if (platform == null)
			{
				await api.CreatePlatform(new Platform(platformConfiguration.Name)
				{
					Url = platformConfiguration.Url,
				});
			}

			// TODO Update platform

			return await api.GetPlatformByName(platformConfiguration.Name);
		}
	}
}