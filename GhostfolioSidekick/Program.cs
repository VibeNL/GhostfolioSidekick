using GhostfolioSidekick.AccountMaintainer;
using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.FileImporter.Bitvavo;
using GhostfolioSidekick.FileImporter.Bunq;
using GhostfolioSidekick.FileImporter.Coinbase;
using GhostfolioSidekick.FileImporter.DeGiro;
using GhostfolioSidekick.FileImporter.Generic;
using GhostfolioSidekick.FileImporter.Nexo;
using GhostfolioSidekick.FileImporter.NIBC;
using GhostfolioSidekick.FileImporter.ScalableCaptial;
using GhostfolioSidekick.FileImporter.Trading212;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.MarketDataMaintainer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick
{
	internal class Program
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

				services.AddScoped<IHostedService, TimedHostedService>();
				services.AddSingleton<IGhostfolioAPI, GhostfolioAPI>();
				services.AddScoped<IScheduledWork, FileImporterTask>();
				services.AddScoped<IScheduledWork, DisplayInformationTask>();
				services.AddScoped<IScheduledWork, MarketDataMaintainerTask>();
				services.AddScoped<IScheduledWork, AccountMaintainerTask>();

				services.AddScoped<IFileImporter, BitvavoParser>();
				services.AddScoped<IFileImporter, BunqParser>();
				services.AddScoped<IFileImporter, CoinbaseParser>();
				services.AddScoped<IFileImporter, DeGiroParserNL>();
				services.AddScoped<IFileImporter, DeGiroParserPT>();
				services.AddScoped<IFileImporter, GenericParser>();
				services.AddScoped<IFileImporter, NexoParser>();
				services.AddScoped<IFileImporter, NIBCParser>();
				services.AddScoped<IFileImporter, ScalableCapitalParser>();
				services.AddScoped<IFileImporter, Trading212Parser>();

			});

			await hostBuilder.RunConsoleAsync();
		}
	}
}
