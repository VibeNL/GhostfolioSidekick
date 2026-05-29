using CoinGecko.Net.Clients;
using CoinGecko.Net.Interfaces;
using CoinGecko.Net.Objects.Options;
using Flurl.Http;
using GhostfolioSidekick.Activities.Strategies;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.ExternalDataProvider.CoinGecko;
using GhostfolioSidekick.ExternalDataProvider.DividendMax;
using GhostfolioSidekick.ExternalDataProvider.Manual;
using GhostfolioSidekick.ExternalDataProvider.Yahoo;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Parsers;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using GhostfolioSidekick.Parsers.TradeRepublic;
using GhostfolioSidekick.PerformanceCalculations;
using GhostfolioSidekick.PerformanceCalculations.Calculator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick
{
	internal static class Program
	{
		[ExcludeFromCodeCoverage]
		private static async Task Main()
		{
			IHostBuilder hostBuilder = CreateHostBuilder();
			using IHost host = hostBuilder.Build();
			FlurlHttp.Configure(settings =>
			{
				settings.HttpClientFactory = new InternalCacheHttpFactory(host.Services);
			});
			await host.RunAsync();
		}

		internal static IHostBuilder CreateHostBuilder()
		{
			return new HostBuilder()
						.ConfigureAppConfiguration((hostContext, configBuilder) =>
						{
							_ = configBuilder.SetBasePath(Directory.GetCurrentDirectory());
							_ = configBuilder.AddJsonFile("appsettings.json", optional: true);
							_ = configBuilder.AddJsonFile(
								$"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
								optional: true);
							_ = configBuilder.AddEnvironmentVariables();
						})
						.ConfigureLogging((hostContext, configLogging) =>
						{
							_ = configLogging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
							_ = configLogging.AddConsole();
						})
						.ConfigureServices((hostContext, services) =>
						{
							_ = services.AddHttpClient();

							_ = services.AddSingleton<MemoryCache, MemoryCache>();
							_ = services.AddSingleton<IMemoryCache>(x => x.GetRequiredService<MemoryCache>());
							_ = services.AddSingleton<IApplicationSettings, ApplicationSettings>();

							_ = services.AddSingleton<IRestClient, RestClient>(x =>
							{
								IApplicationSettings? settings = x.GetService<IApplicationSettings>();
								RestClientOptions options = new(settings!.GhostfolioUrl)
								{
									ThrowOnAnyError = false,
									ThrowOnDeserializationError = false,
								};

								return new RestClient(options);
							});

							_ = services.AddSingleton(x =>
							{
								IApplicationSettings? settings = x.GetService<IApplicationSettings>();
								return new RestCall(x.GetService<IRestClient>()!,
													x.GetService<MemoryCache>()!,
													x.GetService<ILogger<RestCall>>()!,
													settings!.GhostfolioUrl,
													settings!.GhostfolioAccessToken,
													new RestCallOptions() { TrottleTimeout = TimeSpan.FromSeconds(settings!.TrottleTimeout) });
							});
							_ = services.AddSingleton(x =>
							{
								IApplicationSettings? settings = x.GetService<IApplicationSettings>();
								return settings!.ConfigurationInstance.Settings;
							});
							_ = services.AddDbContextFactory<DatabaseContext>((sp, options) =>
							{
								IApplicationSettings? settings = sp.GetService<IApplicationSettings>();
								string? dbPath = settings?.DatabaseFilePath;
								_ = options.UseSqlite($"Data Source={dbPath}");
								_ = options.UseLazyLoadingProxies();
							});

							_ = services.AddSingleton<ICurrencyMapper, SymbolMapper>();
							_ = services.AddSingleton<ICurrencyExchange, CurrencyExchange>();
							_ = services.AddSingleton<IApiWrapper, ApiWrapper>();

							// Register ExternalDataCacheService for caching external data provider requests
							_ = services.AddSingleton<ExternalDataProvider.Cache.IExternalDataCacheService, ExternalDataProvider.Cache.ExternalDataCacheService>();

							AddHooksToCacheExternalServices(services);

							_ = services.AddSingleton<YahooRepository>();
							_ = services.AddSingleton<CoinGeckoRepository>();
							_ = services.AddSingleton<GhostfolioSymbolMatcher>();
							_ = services.AddSingleton<ManualSymbolRepository>();
							_ = services.AddSingleton<DividendMaxMatcher>();
							_ = services.AddTransient<ICoinGeckoRestClient>(sp =>
								new CoinGeckoRestClient(
									sp.GetRequiredService<HttpClient>(),
									sp.GetRequiredService<ILoggerFactory>(),
									Options.Create(new CoinGeckoRestOptions())));

							_ = services.AddSingleton<ICurrencyRepository>(sp => sp.GetRequiredService<YahooRepository>());
							_ = services.AddSingleton<ISymbolMatcher[]>(sp => [
									sp.GetRequiredService<YahooRepository>(),
									sp.GetRequiredService<CoinGeckoRepository>(),
									sp.GetRequiredService<GhostfolioSymbolMatcher>(),
									sp.GetRequiredService<ManualSymbolRepository>(),
									sp.GetRequiredService<DividendMaxMatcher>()
								]);
							_ = services.AddSingleton<IStockPriceRepository[]>(sp => [sp.GetRequiredService<YahooRepository>(), sp.GetRequiredService<CoinGeckoRepository>(), sp.GetRequiredService<ManualSymbolRepository>()]);
							_ = services.AddSingleton<IStockSplitRepository[]>(sp => [sp.GetRequiredService<YahooRepository>()]);
							_ = services.AddSingleton<IGhostfolioSync, GhostfolioSync>();
							_ = services.AddSingleton<IGhostfolioMarketData, GhostfolioMarketData>();

							_ = services.AddHttpClient<IDividendRepository, DividendMaxScraper>();

							_ = services.AddScoped<IHostedService, TimedHostedService>();
							RegisterAllWithInterface<IScheduledWork>(services);
							RegisterAllWithInterface<IHoldingStrategy>(services);
							RegisterAllWithInterface<IFileImporter>(services);
							RegisterAllWithInterface<ITradeRepublicActivityParser>(services);

							_ = services.AddScoped<IPerformanceCalculator, PerformanceCalculator>();

							_ = services.AddScoped<IPdfToWordsParser, PdfToWordsParser>();
						});
		}

		private static void AddHooksToCacheExternalServices(IServiceCollection services)
		{
			// Configure HttpClient with caching handler for external data providers (CoinGecko, DividendMax)
			_ = services.AddTransient<HttpCachingHandler>();
			_ = services.ConfigureAll<HttpClientFactoryOptions>(options =>
			{
				options.HttpMessageHandlerBuilderActions.Add(builder =>
				{
					// Only add caching handler if not already present
					if (!builder.AdditionalHandlers.Any(h => h is HttpCachingHandler))
					{
						IServiceProvider serviceProvider = builder.Services;
						builder.AdditionalHandlers.Add(new HttpCachingHandler(serviceProvider));
					}
				});
			});
		}

		private static void RegisterAllWithInterface<T>(IServiceCollection services)
		{
			IEnumerable<Type> types = typeof(T).Assembly.GetTypes()
				.Where(t => t.GetInterfaces().Contains(typeof(T)) && !t.IsInterface && !t.IsAbstract);
			foreach (Type type in types)
			{
				_ = services.AddScoped(typeof(T), type);
			}
		}
	}

	internal class InternalCacheHttpFactory(IServiceProvider serviceProvider) : Flurl.Http.Configuration.IHttpClientFactory
	{
		private readonly Flurl.Http.Configuration.DefaultHttpClientFactory defaultFactory = new();

		public HttpClient CreateHttpClient(HttpMessageHandler handler)
		{
			// Wrap the handler with our caching handler
			HttpCachingHandler cachingHandler = new(serviceProvider)
			{
				InnerHandler = handler
			};

			return new HttpClient(cachingHandler, disposeHandler: false);
		}

		public HttpMessageHandler CreateMessageHandler()
		{
			// Use default message handler creation
			return defaultFactory.CreateMessageHandler();
		}
	}
}
