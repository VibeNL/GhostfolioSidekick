using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.WASM.AI;
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
			var apiServiceHttp = configuration.GetSection("Services:apiservice:http").Get<string[]>()?.SingleOrDefault();
			if (!string.IsNullOrWhiteSpace(apiServiceHttp))
			{
				client.BaseAddress = new Uri("http://apiservice");
			}
			else
			{
				client.BaseAddress = new Uri(hostEnvironment.BaseAddress);
			}
		});

		builder.Services.AddOidcAuthentication(options =>
		{
			// Configure your authentication provider options here.
			// For more information, see https://aka.ms/blazor-standalone-auth
			builder.Configuration.Bind("Local", options.ProviderOptions);
		});

		builder.Services.AddBesqlDbContextFactory<DatabaseContext>(options =>
			options.UseSqlite("Data Source=portfolio.db;Cache=Shared;Pooling=true;")
			);

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
