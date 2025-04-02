using CoinGecko.Net.Clients;
using CoinGecko.Net.Interfaces;
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
using GhostfolioSidekick.ProcessingService.Activities.Strategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestSharp;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.ProcessingService
{
	public static class Program
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
				.ConfigureAppConfiguration(ConfigureApp)
				.ConfigureLogging(ConfigureLogging)
				.ConfigureServices(ConfigureServices);
		}

		private static void ConfigureApp(HostBuilderContext hostContext, IConfigurationBuilder configBuilder)
		{
			configBuilder.SetBasePath(Directory.GetCurrentDirectory());
			configBuilder.AddJsonFile("appsettings.json", optional: true);
			configBuilder.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
			configBuilder.AddEnvironmentVariables();
		}

		private static void ConfigureLogging(HostBuilderContext hostContext, ILoggingBuilder configLogging)
		{
			configLogging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
			configLogging.AddConsole();
		}

		private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
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
				options.UseSqlite($"Data Source={settings!.FileImporterPath}/ghostfoliosidekick.db");
			});

			RegisterServices(services);
			RegisterRepositories(services);
			RegisterHostedServices(services);
			RegisterParsers(services);
		}

		private static void RegisterServices(IServiceCollection services)
		{
			services.AddSingleton<ICurrencyMapper, SymbolMapper>();
			services.AddSingleton<ICurrencyExchange, CurrencyExchange>();
			services.AddSingleton<IApiWrapper, ApiWrapper>();
			services.AddSingleton<IGhostfolioSync, GhostfolioSync>();
			services.AddSingleton<IGhostfolioMarketData, GhostfolioMarketData>();
		}

		private static void RegisterRepositories(IServiceCollection services)
		{
			services.AddSingleton<YahooRepository>();
			services.AddSingleton<CoinGeckoRepository>();
			services.AddSingleton<GhostfolioSymbolMatcher>();
			services.AddSingleton<ManualSymbolMatcher>();
			services.AddTransient<ICoinGeckoRestClient, CoinGeckoRestClient>();

			services.AddSingleton<ICurrencyRepository>(sp => sp.GetRequiredService<YahooRepository>());
			services.AddSingleton<ISymbolMatcher[]>(sp =>
			[
				sp.GetRequiredService<YahooRepository>(),
				sp.GetRequiredService<CoinGeckoRepository>(),
				sp.GetRequiredService<GhostfolioSymbolMatcher>(),
				sp.GetRequiredService<ManualSymbolMatcher>()
			]);
			services.AddSingleton<IStockPriceRepository[]>(sp =>
			[
				sp.GetRequiredService<YahooRepository>(),
				sp.GetRequiredService<CoinGeckoRepository>()
			]);
			services.AddSingleton<IStockSplitRepository[]>(sp =>
			[
				sp.GetRequiredService<YahooRepository>()
			]);
		}

		private static void RegisterHostedServices(IServiceCollection services)
		{
			services.AddScoped<IHostedService, TimedHostedService>();
			RegisterAllWithInterface<IScheduledWork>(services);
			RegisterAllWithInterface<IHoldingStrategy>(services);
			RegisterAllWithInterface<IFileImporter>(services);
		}

		private static void RegisterParsers(IServiceCollection services)
		{
			services.AddScoped<IPdfToWordsParser, PdfToWordsParser>();
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

		public static void ConfigureForDocker(HostBuilderContext context, IServiceCollection collection)
		{
			throw new NotImplementedException();
		}
	}
}
