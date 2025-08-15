using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.PortfolioViewer.WASM.AI;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
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
			client.BaseAddress = new Uri(hostEnvironment.BaseAddress);
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

		builder.Services.AddWebChatClient();

		// Register PortfolioClient for DI
		builder.Services.AddScoped<Clients.PortfolioClient>();

		builder.Services.AddSingleton<ITestContextService, TestContextService>();

		builder.Logging.SetMinimumLevel(LogLevel.Trace);

		// Performance Calculations
		builder.Services.AddMemoryCache();
		builder.Services.AddScoped<ICurrencyExchange, CurrencyExchange>();
		builder.Services.AddScoped<IHoldingsDataService, HoldingsDataService>();

		var app = builder.Build();

		// Initialize the database after building the app
		var serviceScope = app.Services.CreateScope();
		var sqlitePersistence = serviceScope.ServiceProvider.GetRequiredService<SqlitePersistence>();
		await sqlitePersistence.InitializeDatabase();

		await app.RunAsync();
	}
}
