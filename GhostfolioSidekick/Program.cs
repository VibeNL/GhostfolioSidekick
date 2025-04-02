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

		private static void ConfigureServices(HostBuilderContext context, IServiceCollection collection)
		{
			ProcessingService.Program.ConfigureForDocker(context, collection);
			PortfolioViewer.ApiService.Program.ConfigureForDocker(context, collection);
			PortfolioViewer.WASM.Program.ConfigureForDocker(collection);
					}
	}
}
