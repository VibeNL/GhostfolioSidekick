using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.GhostfolioAPI.API;
using Microsoft.EntityFrameworkCore;

namespace PortfolioViewer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            ConfigureServices(builder.Services);

            await builder.Build().RunAsync();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContextFactory<DatabaseContext>(options =>
            {
                options.UseSqlite("Data Source=ghostfoliosidekick.db");
            });

            services.AddScoped<IApiWrapper, ApiWrapper>();
        }
    }
}
