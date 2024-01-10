using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.Ghostfolio.API;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	public class AccountMaintainerTask : IScheduledWork
	{
		private readonly ILogger<FileImporterTask> logger;
		private readonly IGhostfolioAPI api;
		private readonly ConfigurationInstance configurationInstance;

		public int Priority => 1;

		public AccountMaintainerTask(
			ILogger<FileImporterTask> logger,
			IGhostfolioAPI api,
			IApplicationSettings applicationSettings)
		{
			ArgumentNullException.ThrowIfNull(applicationSettings);

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.api = api ?? throw new ArgumentNullException(nameof(api));
			this.configurationInstance = applicationSettings.ConfigurationInstance;
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
			var platforms = configurationInstance.Platforms;
			var accounts = configurationInstance.Accounts;

			foreach (var accountConfig in accounts ?? Enumerable.Empty<AccountConfiguration>())
			{
				var account = await api.GetAccountByName(accountConfig.Name);

				if (account == null)
				{
					await CreateAccount(accountConfig, platforms?.SingleOrDefault(x => x.Name == accountConfig.Platform));
				}

				//UpdateAccount(accountConfig, platforms.SingleOrDefault(x => x.Name == accountConfig.Platform));
			}
		}

		private async Task CreateAccount(AccountConfiguration accountConfig, PlatformConfiguration? platformConfiguration)
		{
			await CreateOrUpdatePlatform(platformConfiguration);

			await api.CreateAccount(new Model.Account(
				Guid.Empty.ToString(),
				accountConfig.Name,
				new Model.Balance(new Model.Money(accountConfig.Currency, 0, DateTime.Now)),
				accountConfig.Comment,
				accountConfig.Platform,
				[]));
		}

		private async Task CreateOrUpdatePlatform(PlatformConfiguration? platformConfiguration)
		{
			if (platformConfiguration is null)
			{
				return;
			}

			var platform = await api.GetPlatformByName(platformConfiguration.Name);

			if (platform == null)
			{
				await api.CreatePlatform(new Model.Platform(null, platformConfiguration.Name, platformConfiguration.Url));
			}

			// TODO Update
		}
	}
}