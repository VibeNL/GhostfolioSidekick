﻿using CoinGecko.Net.Clients;
using CoinGecko.Net.Interfaces;
using GhostfolioSidekick.Activities.Strategies;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.ExternalDataProvider.CoinGecko;
using GhostfolioSidekick.ExternalDataProvider.Manual;
using GhostfolioSidekick.ExternalDataProvider.Yahoo;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Parsers;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using GhostfolioSidekick.PerformanceCalculations;
using GhostfolioSidekick.PerformanceCalculations.Calculator;
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

							services.AddSingleton(x =>
							{
								var settings = x.GetService<IApplicationSettings>();
								return new RestCall(x.GetService<IRestClient>()!,
													x.GetService<MemoryCache>()!,
													x.GetService<ILogger<RestCall>>()!,
													settings!.GhostfolioUrl,
													settings!.GhostfolioAccessToken,
													new RestCallOptions() { TrottleTimeout = TimeSpan.FromSeconds(settings!.TrottleTimeout) });
							});
							services.AddSingleton(x =>
							{
								var settings = x.GetService<IApplicationSettings>();
								return settings!.ConfigurationInstance.Settings;
							});
							services.AddDbContextFactory<DatabaseContext>(options =>
							{
								var settings = services.BuildServiceProvider().GetService<IApplicationSettings>();

								var dbPath = settings?.DatabasePath ?? settings?.FileImporterPath;
								if (string.IsNullOrEmpty(dbPath))
								{
									throw new InvalidOperationException("Database path is not configured.");
								}

								if (!dbPath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
								{
									dbPath = Path.Combine(dbPath, "ghostfolio.db");
								}

								options.UseSqlite($"Data Source={dbPath}");
								options.UseLazyLoadingProxies();
							});

							services.AddSingleton<ICurrencyMapper, SymbolMapper>();
							services.AddSingleton<ICurrencyExchange, CurrencyExchange>();
							services.AddSingleton<IApiWrapper, ApiWrapper>();

							services.AddSingleton<YahooRepository>();
							services.AddSingleton<CoinGeckoRepository>();
							services.AddSingleton<GhostfolioSymbolMatcher>();
							services.AddSingleton<ManualSymbolMatcher>();
							services.AddTransient<ICoinGeckoRestClient, CoinGeckoRestClient>();

							services.AddSingleton<ICurrencyRepository>(sp => sp.GetRequiredService<YahooRepository>());
							services.AddSingleton<ISymbolMatcher[]>(sp => [
									sp.GetRequiredService<YahooRepository>(),
									sp.GetRequiredService<CoinGeckoRepository>(),
									sp.GetRequiredService<GhostfolioSymbolMatcher>(),
									sp.GetRequiredService<ManualSymbolMatcher>()
								]);
							services.AddSingleton<IStockPriceRepository[]>(sp => [sp.GetRequiredService<YahooRepository>(), sp.GetRequiredService<CoinGeckoRepository>()]);
							services.AddSingleton<IStockSplitRepository[]>(sp => [sp.GetRequiredService<YahooRepository>()]);
							services.AddSingleton<IGhostfolioSync, GhostfolioSync>();
							services.AddSingleton<IGhostfolioMarketData, GhostfolioMarketData>();

							services.AddScoped<IHostedService, TimedHostedService>();
							RegisterAllWithInterface<IScheduledWork>(services);
							RegisterAllWithInterface<IHoldingStrategy>(services);
							RegisterAllWithInterface<IFileImporter>(services);

							services.AddScoped<IHoldingPerformanceCalculator, HoldingPerformanceCalculator>();

							services.AddScoped<IPdfToWordsParser, PdfToWordsParser>();
						});
		}

		private static void RegisterAllWithInterface<T>(IServiceCollection services)
		{
			var types = typeof(T).Assembly.GetTypes()
				.Where(t => t.GetInterfaces().Contains(typeof(T)) && !t.IsInterface && !t.IsAbstract);
			foreach (var type in types)
			{
				services.AddScoped(typeof(T), type);
			}
		}
	}
}
