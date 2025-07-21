using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.WASM.AI;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Reflection;

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

		builder.Services.AddBesqlDbContextFactory<DatabaseContext>(async (sp, options) =>
		{
			var js = sp.GetRequiredService<IJSRuntime>();

			// Step 1: Import the JavaScript module that contains IndexedDB functions
			var module = await js.InvokeAsync<IJSObjectReference>("import", "./js/sqlite-persistence.js");
			Console.WriteLine("JavaScript module loaded");

			// Step 2: Restore database from IndexedDB to the virtual filesystem
			// This step is critical - it must happen BEFORE we open the database
			// to ensure we don't lose data across page refreshes
			await module.InvokeVoidAsync("setupDatabase", DatabaseContext.DbFileName);
			Console.WriteLine("Database setup completed");
			
			options.UseSqlite($"Data Source={DatabaseContext.DbFileName};Cache=Shared;Pooling=true;");
		}); 

		builder.Services.AddWebChatClient();

		// Register PortfolioClient for DI
		builder.Services.AddScoped<Clients.PortfolioClient>();

		builder.Logging.SetMinimumLevel(LogLevel.Trace);

		var app = builder.Build();

		var context = app.Services.GetRequiredService<DatabaseContext>();
		var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
		if (pendingMigrations.Any())
		{
			await context.Database.MigrateAsync();
		}

		await app.RunAsync();
	}
}
