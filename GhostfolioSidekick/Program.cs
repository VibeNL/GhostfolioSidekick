using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick
{
	internal static class Program
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

		private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
		{
			services.AddSingleton<MemoryCache, MemoryCache>();
			services.AddSingleton<IMemoryCache>(x => x.GetRequiredService<MemoryCache>());
			services.AddSingleton<IApplicationSettings, ApplicationSettings>();

			services.AddDbContextFactory<DatabaseContext>(options =>
			{
				var settings = services.BuildServiceProvider().GetService<IApplicationSettings>();
				options.UseSqlite($"Data Source={settings!.FileImporterPath}/ghostfoliosidekick.db");
			});

			ProcessingService.Program.ConfigureForDocker(services);
			//PortfolioViewer.ApiService.Program.ConfigureForDocker(context, services);
			//PortfolioViewer.WASM.Program.ConfigureForDocker(services);
		}
	}
}
