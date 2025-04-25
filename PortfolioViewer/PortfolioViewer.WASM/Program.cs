using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.WASM.AI;
using GhostfolioSidekick.PortfolioViewer.WASM.Clients;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM;

public class Program
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

		builder.Services.AddHttpClient<PortfolioClient>(
			(sp, client ) =>
		{
			var config = sp.GetRequiredService<IConfiguration>();
			// Read the "Services.apiservice.http" value from the configuration
			var apiServiceHttp = config.GetSection("Services:apiservice:http").Get<string[]>()?.SingleOrDefault();
			if (!string.IsNullOrWhiteSpace(apiServiceHttp))
			{
				client.BaseAddress = new Uri("http://apiservice");
			}
			else
			{
				client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
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
		var app =builder.Build();

		var context = app.Services.GetRequiredService<DatabaseContext>();
		var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
		if (pendingMigrations.Any())
		{
			await context.Database.MigrateAsync();
		}

		await app.RunAsync();
	}
}
