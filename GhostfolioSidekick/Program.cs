using GhostfolioSidekick.AccountMaintainer;
using GhostfolioSidekick.Activities;
using GhostfolioSidekick.Activities.Strategies;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.ExternalDataProvider.CoinGecko;
using GhostfolioSidekick.ExternalDataProvider.Yahoo;
using GhostfolioSidekick.MarketDataMaintainer;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Parsers;
using GhostfolioSidekick.Parsers.Bitvavo;
using GhostfolioSidekick.Parsers.Bunq;
using GhostfolioSidekick.Parsers.CentraalBeheer;
using GhostfolioSidekick.Parsers.Coinbase;
using GhostfolioSidekick.Parsers.DeGiro;
using GhostfolioSidekick.Parsers.Generic;
using GhostfolioSidekick.Parsers.MacroTrends;
using GhostfolioSidekick.Parsers.Nexo;
using GhostfolioSidekick.Parsers.NIBC;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using GhostfolioSidekick.Parsers.ScalableCaptial;
using GhostfolioSidekick.Parsers.TradeRepublic;
using GhostfolioSidekick.Parsers.Trading212;
using GhostfolioSidekick.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestSharp;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick
{
	internal static class Program
	{
		[ExcludeFromCodeCoverage]
		static async Task Main(string[] args)
		{
			IHostBuilder hostBuilder = CreateHostBuilder();

			await hostBuilder.RunConsoleAsync();
		}

		internal static IHostBuilder CreateHostBuilder()
		{
			return new HostBuilder()
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

							services.AddSingleton<IRestClient, RestClient>(x =>
							{
								var settings = x.GetService<IApplicationSettings>();
								var options = new RestClientOptions(settings!.GhostfolioUrl)
								{
									ThrowOnAnyError = false,
									ThrowOnDeserializationError = false,
								};

								return new RestClient(options);
							});

							//services.AddSingleton(x =>
							//{
							//	var settings = x.GetService<IApplicationSettings>();
							//	return new RestCall(x.GetService<IRestClient>()!,
							//						x.GetService<MemoryCache>()!,
							//						x.GetService<ILogger<RestCall>>()!,
							//						settings!.GhostfolioUrl,
							//						settings!.GhostfolioAccessToken,
							//						new RestCallOptions() { TrottleTimeout = TimeSpan.FromSeconds(settings!.TrottleTimeout) });
							//});
							services.AddSingleton(x =>
							{
								var settings = x.GetService<IApplicationSettings>();
								return settings!.ConfigurationInstance.Settings;
							});
							//services.AddDbContext<DatabaseContext>(options =>
							//{
							//	var settings = services.BuildServiceProvider().GetService<IApplicationSettings>();
							//	options.UseSqlite($"Data Source={settings!.FileImporterPath}/ghostfoliosidekick.db");
							//});
							services.AddDbContextFactory<DatabaseContext>(options =>
							{
								var settings = services.BuildServiceProvider().GetService<IApplicationSettings>();
								options.UseSqlite($"Data Source={settings!.FileImporterPath}/ghostfoliosidekick.db");
							});

							services.AddScoped<IAccountRepository, AccountRepository>();
							services.AddScoped<IActivityRepository, ActivityRepository>();
							services.AddSingleton<IMarketDataRepository, MarketDataRepository>();
							services.AddSingleton<ICurrencyMapper, SymbolMapper>();
							services.AddSingleton<ICurrencyExchange, CurrencyExchange>();

							services.AddSingleton<YahooRepository>();
							services.AddSingleton<CoinGeckoRepository>();

							services.AddSingleton<ICurrencyRepository>(sp => sp.GetRequiredService<YahooRepository>());
							services.AddSingleton<ISymbolMatcher[]>(sp => [ sp.GetRequiredService<YahooRepository>(), sp.GetRequiredService<CoinGeckoRepository>() ]);
							services.AddSingleton<IStockPriceRepository[]>(sp => [sp.GetRequiredService<YahooRepository>(), sp.GetRequiredService<CoinGeckoRepository>()]);
							services.AddSingleton<IStockSplitRepository[]>(sp => [sp.GetRequiredService<YahooRepository>()]);

							////services.AddSingleton<IExchangeRateService, ExchangeRateService>();
							//services.AddSingleton<IActivitiesService, ActivitiesService>();
							//services.AddSingleton<IAccountService, AccountService>();
							//services.AddSingleton<IMarketDataService, MarketDataService>();
							//services.AddSingleton<IStockSplitRepository, StockSplitRepository>();

							services.AddScoped<IHostedService, TimedHostedService>();
							services.AddScoped<IScheduledWork, DisplayInformationTask>();
							services.AddScoped<IScheduledWork, GenerateDatabaseTask>();
							services.AddScoped<IScheduledWork, AccountMaintainerTask>();
							services.AddScoped<IScheduledWork, FileImporterTask>();
							services.AddScoped<IScheduledWork, SymbolMatcherTask>();
							services.AddScoped<IScheduledWork, CurrencyGathererTask>();
							services.AddScoped<IScheduledWork, BalanceMaintainerTask>();
							services.AddScoped<IScheduledWork, MarketDataGathererTask>();
							services.AddScoped<IScheduledWork, MarketDataStockSplitTask>();
							services.AddScoped<IScheduledWork, CalculatePriceTask>();
							services.AddScoped<IScheduledWork, CleanupDatabaseTask>();
							services.AddScoped<IScheduledWork, SyncWithGhostfolioTask>();
							////services.AddScoped<IScheduledWork, CreateManualSymbolTask>();
							////services.AddScoped<IScheduledWork, SetManualPricesTask>();
							////services.AddScoped<IScheduledWork, SetBenchmarksTask>();
							////services.AddScoped<IScheduledWork, SetTrackingInsightOnSymbolsTask>();
							////services.AddScoped<IScheduledWork, DeleteUnusedSymbolsTask>();
							////services.AddScoped<IScheduledWork, GatherAllDataTask>();

							services.AddScoped<IHoldingStrategy, StockSplitStrategy>();


							services.AddScoped<IPdfToWordsParser, PdfToWordsParser>();
							services.AddScoped<IFileImporter, BitvavoParser>();
							services.AddScoped<IFileImporter, BunqParser>();
							services.AddScoped<IFileImporter, CentraalBeheerParser>();
							services.AddScoped<IFileImporter, CoinbaseParser>();
							services.AddScoped<IFileImporter, DeGiroParserNL>();
							services.AddScoped<IFileImporter, DeGiroParserEN>();
							services.AddScoped<IFileImporter, DeGiroParserPT>();
							services.AddScoped<IFileImporter, GenericParser>();
							services.AddScoped<IFileImporter, StockSplitParser>();
							services.AddScoped<IFileImporter, MacroTrendsParser>();
							services.AddScoped<IFileImporter, NexoParser>();
							services.AddScoped<IFileImporter, NIBCParser>();
							services.AddScoped<IFileImporter, ScalableCapitalRKKParser>();
							services.AddScoped<IFileImporter, ScalableCapitalWUMParser>();
							services.AddScoped<IFileImporter, ScalableCapitalPrimeParser>();
							services.AddScoped<IFileImporter, TradeRepublicInvoiceParserEN>();
							services.AddScoped<IFileImporter, TradeRepublicInvoiceParserNL>();
							services.AddScoped<IFileImporter, TradeRepublicStatementParserNL>();
							services.AddScoped<IFileImporter, Trading212Parser>();

							//services.AddScoped<IHoldingStrategy, AddStakeRewardsToPreviousBuyActivity>();
							//services.AddScoped<IHoldingStrategy, ApplyDustCorrection>();
							//services.AddScoped<IHoldingStrategy, DeterminePrice>();
							//services.AddScoped<IHoldingStrategy, HandleTaxesOnDividends>();
							//services.AddScoped<IHoldingStrategy, NotNativeSupportedTransactionsInGhostfolio>();
							//services.AddScoped<IHoldingStrategy, RoundStrategy>();
							//services.AddScoped<IHoldingStrategy, StockSplitStrategy>();
						});
		}
	}
}
