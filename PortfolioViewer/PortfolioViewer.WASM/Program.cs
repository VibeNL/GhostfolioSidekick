using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.PortfolioViewer.WASM.AI;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;

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
				// Fallback to the current host for relative URLs
				client.BaseAddress = new Uri(hostEnvironment.BaseAddress);
			}
		});

		builder.Services.AddOidcAuthentication(options =>
		{
			// Configure your authentication provider options here.
			// For more information, see https://aka.ms/blazor-standalone-auth
			builder.Configuration.Bind("Local", options.ProviderOptions);
		});

		builder.Services.AddSingleton<SqlitePersistence>();
		
		builder.Services.AddBesqlDbContextFactory<DatabaseContext>(options =>
		{
			options.UseSqlite($"Data Source={DatabaseContext.DbFileName}");
			options.UseLazyLoadingProxies();
		});
		
		builder.Services.AddWebChatClient();

		// Register PortfolioClient for DI
		builder.Services.AddScoped<Clients.PortfolioClient>();

		builder.Logging.SetMinimumLevel(LogLevel.Trace);

		// Performance Calculations
		builder.Services.AddScoped<IHoldingsDataService, HoldingsDataService>();

		var app = builder.Build();

		// Initialize the database after building the app
		var serviceScope = app.Services.CreateScope();
		var sqlitePersistence = serviceScope.ServiceProvider.GetRequiredService<SqlitePersistence>();
		await sqlitePersistence.InitializeDatabase();

		await app.RunAsync();
	}
}
