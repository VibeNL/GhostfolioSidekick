using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.WASM.Clients;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM;

public class Program
{
	public static void ConfigureForDocker(IServiceCollection collection)
	{
		throw new NotImplementedException();
	}

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
			client =>
			{
				client.BaseAddress = new Uri("https+http://apiservice");
			});

		//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

		builder.Services.AddOidcAuthentication(options =>
        {
            // Configure your authentication provider options here.
            // For more information, see https://aka.ms/blazor-standalone-auth
            builder.Configuration.Bind("Local", options.ProviderOptions);
        });

		builder.Services.AddBesqlDbContextFactory<DatabaseContext>(options => 
			options.UseSqlite("Data Source=portfolio.db;Cache=Shared;Pooling=true;")
			);

		await builder.Build().RunAsync();
    }
}
