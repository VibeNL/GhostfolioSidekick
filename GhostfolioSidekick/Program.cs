using GhostfolioSidekick.AccountMaintainer;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Cryptocurrency;
using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.GhostfolioAPI.Strategies;
using GhostfolioSidekick.MarketDataMaintainer;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Parsers;
using GhostfolioSidekick.Parsers.Bitvavo;
using GhostfolioSidekick.Parsers.Bunq;
using GhostfolioSidekick.Parsers.Coinbase;
using GhostfolioSidekick.Parsers.DeGiro;
using GhostfolioSidekick.Parsers.Generic;
using GhostfolioSidekick.Parsers.Nexo;
using GhostfolioSidekick.Parsers.NIBC;
using GhostfolioSidekick.Parsers.ScalableCaptial;
using GhostfolioSidekick.Parsers.Trading212;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick
{
	internal static class Program
	{
		static async Task Main(string[] args)
		{
			var hostBuilder = new HostBuilder()
			.ConfigureAppConfiguration((hostContext, configBuilder) =>
			{
				configBuilder.SetBasePath(Directory.GetCurrentDirectory());
				configBuilder.AddJsonFile("appsettings.json", optional: true);
				configBuilder.AddJsonFile(
					$"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
					optional: true);
				configBuilder.AddEnvironmentVariables();
			})
			.ConfigureLogging((hostContext, configLogging) =>
			{
				configLogging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
				configLogging.AddConsole();
			})
			.ConfigureServices((hostContext, services) =>
			{
				services.AddSingleton<MemoryCache, MemoryCache>();
				services.AddSingleton<IMemoryCache>(x => x.GetRequiredService<MemoryCache>());
				services.AddSingleton<IApplicationSettings, ApplicationSettings>();

				services.AddSingleton(x =>
				{
					var settings = x.GetService<IApplicationSettings>();
					return new RestCall(
										x.GetService<MemoryCache>()!,
										x.GetService<ILogger<RestCall>>()!,
										settings!.GhostfolioUrl,
										settings!.GhostfolioAccessToken);
				});
				services.AddSingleton<IExchangeRateService, ExchangeRateService>();
				services.AddSingleton<IActivitiesService, ActivitiesService>();
				services.AddSingleton<IAccountService, AccountService>();
				services.AddSingleton<IMarketDataService, MarketDataService>();

				services.AddScoped<IHostedService, TimedHostedService>();
				services.AddScoped<IScheduledWork, FileImporterTask>();
				services.AddScoped<IScheduledWork, DisplayInformationTask>();
				services.AddScoped<IScheduledWork, AccountMaintainerTask>();
				services.AddScoped<IScheduledWork, CreateManualSymbolTask>();
				services.AddScoped<IScheduledWork, SetBenchmarksTask>();
				services.AddScoped<IScheduledWork, SetTrackingInsightOnSymbolsTask>();

				services.AddScoped<IFileImporter, BitvavoParser>();
				services.AddScoped<IFileImporter, BunqParser>();
				services.AddScoped<IFileImporter, CoinbaseParser>();
				services.AddScoped<IFileImporter, DeGiroParserNL>();
				services.AddScoped<IFileImporter, DeGiroParserPT>();
				services.AddScoped<IFileImporter, GenericParser>();
				services.AddScoped<IFileImporter, NexoParser>();
				services.AddScoped<IFileImporter, NIBCParser>();
				services.AddScoped<IFileImporter, ScalableCapitalRKKParser>();
				services.AddScoped<IFileImporter, ScalableCapitalWUMParser>();
				services.AddScoped<IFileImporter, Trading212Parser>();

				////services.AddScoped<IHoldingStrategy, DeterminePrice>();
				////services.AddScoped<IHoldingStrategy, ApplyDustCorrectionWorkaround>();
				services.AddScoped<IHoldingStrategy, ////StakeAsDividendWorkaround>();
			});

			await hostBuilder.RunConsoleAsync();
		}
	}
}
