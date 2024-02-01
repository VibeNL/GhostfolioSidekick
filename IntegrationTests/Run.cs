using GhostfolioSidekick;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IntegrationTests
{
	public class Run
	{
		[Fact]
		public async Task SetupInstance()
		{
			// Arrange
			Environment.SetEnvironmentVariable("GHOSTFOLIO_URL", "https://ghostfol.io/");
			Environment.SetEnvironmentVariable("GHOSTFOLIO_ACCESTOKEN", "2026bcfd9b1f6e5ad608750babc5a949b84786394832173fe266428ea0c14263471c220a277ecc983e9e91234e3e4657fcf29112e5fe1d7a1e5e105ec1c9fe7b");
			Environment.SetEnvironmentVariable("FILEIMPORTER_PATH", "./Files/");
			Environment.SetEnvironmentVariable("CONFIGURATIONFILE_PATH", "./Files/config.json");

			var testLogger = new TestLogger("Service has executed.");
			var testHost = Program
			.CreateHostBuilder()
			.ConfigureServices((hostContext, services) =>
			{
				services.AddSingleton<ILogger<TimedHostedService>>(testLogger);
			})
			.Build();

			var host = testHost.Services.GetService<IHostedService>();
			var c = new CancellationToken();

			// Act
			await host!.StartAsync(c);

			while (!testLogger.IsTriggered)
			{
				await Task.Delay(1000);
			}

			// Assert
		}
	}
}