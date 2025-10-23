using GhostfolioSidekick.AI.Agents;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.PortfolioViewer.WASM.AI;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.Api;
using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioViewer.WASM;

public static class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebAssemblyHostBuilder.CreateDefault(args);
		builder.RootComponents.Add<App>("#app");
		builder.RootComponents.Add<HeadOutlet>("head::after");

		builder.Services.AddServiceDiscovery();
		builder.Services.ConfigureHttpClientDefaults(static http =>
		{
			http.AddServiceDiscovery();
		});

		// Get HostEnvironment and Configuration before registering services
		var hostEnvironment = builder.HostEnvironment;
		var configuration = builder.Configuration;

		// Configure the default HttpClient for all consumers
		builder.Services.AddHttpClient(string.Empty, client =>
		{
			var apiServiceHttps = configuration.GetSection("Services:apiservice:https").Get<string[]>()?.FirstOrDefault();
			var apiServiceHttp = configuration.GetSection("Services:apiservice:http").Get<string[]>()?.FirstOrDefault();

			// In production/development, use the configured API service URLs
			if (!string.IsNullOrWhiteSpace(apiServiceHttps))
			{
				client.BaseAddress = new Uri(apiServiceHttps);
			}
			else if (!string.IsNullOrWhiteSpace(apiServiceHttp))
			{
				client.BaseAddress = new Uri(apiServiceHttp);
			}
			else
			{
				client.BaseAddress = new Uri(hostEnvironment.BaseAddress);
			}
		});

		// Configure custom authentication
		builder.Services.AddAuthorizationCore();
		builder.Services.AddScoped<ITokenValidationService, TokenValidationService>();
		builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
		builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
		builder.Services.AddCascadingAuthenticationState();

		builder.Services.AddSingleton<SqlitePersistence>();

		builder.Services.AddBesqlDbContextFactory<DatabaseContext>(options =>
		{
			options.UseSqlite($"Data Source={DatabaseContext.DbFileName}");
			options.UseLazyLoadingProxies();
		});

		// Register Chat clients - prefer WebLLM by default
		builder.Services.AddWebChatClient();
		// Also register ApiChatClient if needed (registers ICustomChatClient implementation)
		////builder.Services.AddApiChatClient();

		builder.Services.AddAgents();

		// Register ServerConfigurationService for DI
		builder.Services.AddSingleton<IServerConfigurationService, ServerConfigurationService>();

		// Register PortfolioClient for DI
		builder.Services.AddScoped<Clients.PortfolioClient>();

		// Register SyncTrackingService for DI
		builder.Services.AddScoped<ISyncTrackingService, SyncTrackingService>();

		// Register WakeLockService for DI
		builder.Services.AddScoped<IWakeLockService, WakeLockService>();

		builder.Services.AddSingleton<ITestContextService, TestContextService>();

		builder.Logging.SetMinimumLevel(LogLevel.Trace);

		// Performance Calculations
		builder.Services.AddSingleton<MemoryCache, MemoryCache>();
		builder.Services.AddSingleton<IMemoryCache>(x => x.GetRequiredService<MemoryCache>());

		builder.Services.AddSingleton<ICurrencyExchange, CurrencyExchange>();
		builder.Services.AddScoped<IHoldingsDataService, HoldingsDataService>();
		builder.Services.AddScoped<IAccountDataService, AccountDataService>();
		builder.Services.AddScoped<ITransactionService, TransactionService>();

		// Data Issues Service
		builder.Services.AddScoped<IDataIssuesService, DataIssuesService>();

		var app = builder.Build();

		// Initialize the database after building the app
		var serviceScope = app.Services.CreateScope();
		var sqlitePersistence = serviceScope.ServiceProvider.GetRequiredService<SqlitePersistence>();
		await sqlitePersistence.InitializeDatabase();

		// Preload server configuration to avoid blocking calls later
		try
		{
			var serverConfigService = app.Services.GetRequiredService<IServerConfigurationService>();
			await serverConfigService.GetPrimaryCurrencyAsync();
		}
		catch (Exception)
		{
			// Ignore errors during preload - the service will fall back to defaults
		}

		await app.RunAsync();
	}
}
