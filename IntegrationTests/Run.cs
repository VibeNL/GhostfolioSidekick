using FluentAssertions;
using GhostfolioSidekick.GhostfolioAPI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.IntegrationTests
{
	public class Run
	{
		private Dictionary<string, int> AccountsWithExpectedNumbers = new Dictionary<string, int>
		{
			{ "TestAccount1", 2 },
			{ "TestAccount2", 1 },
		};

		public Run()
		{

		}

		[Fact(Timeout = 300000)]
		public async Task TestSimpleImport()
		{
			// Arrange
			Environment.SetEnvironmentVariable("GHOSTFOLIO_URL", "https://ghostfol.io/");
			Environment.SetEnvironmentVariable("GHOSTFOLIO_ACCESTOKEN", "2026bcfd9b1f6e5ad608750babc5a949b84786394832173fe266428ea0c14263471c220a277ecc983e9e91234e3e4657fcf29112e5fe1d7a1e5e105ec1c9fe7b");
			Environment.SetEnvironmentVariable("FILEIMPORTER_PATH", "./Files/");
			Environment.SetEnvironmentVariable("CONFIGURATIONFILE_PATH", "./Files/config.json");

			var testLogger = new TestLogger("Service FileImporterTask has executed.");
			var testHost = Program
			.CreateHostBuilder()
			.ConfigureServices((hostContext, services) =>
			{
				services.AddSingleton<ILogger<TimedHostedService>>(testLogger);
			})
			.Build();

			// Delete all existing items if exists
			var activitiesService = testHost.Services.GetService<IActivitiesService>();
			var accountService = testHost.Services.GetService<IAccountService>();
			await CleanInstance(activitiesService!, accountService!);

			var host = testHost.Services.GetService<IHostedService>();
			var c = new CancellationToken();

			// Act
			await host!.StartAsync(c);

			while (!testLogger.IsTriggered)
			{
				await Task.Delay(1000);
			}

			// Assert
			await VerifyInstance(activitiesService!, accountService!);
		}

		private async Task CleanInstance(IActivitiesService activitiesService, IAccountService accountService)
		{
			await activitiesService.DeleteAll();

			foreach (var item in AccountsWithExpectedNumbers)
			{
				await accountService.DeleteAccount(item.Key);
				var account = await accountService.GetAccountByName(item.Key);
				account.Should().BeNull();
			}
		}

		private async Task VerifyInstance(IActivitiesService activitiesService, IAccountService accountService)
		{
			foreach (var item in AccountsWithExpectedNumbers)
			{
				var account = await accountService.GetAccountByName(item.Key);
				account.Should().NotBeNull();

				var activities = (await activitiesService.GetAllActivities())
						.SelectMany(x => x.Activities)
						.Where(x => x.Account.Name == item.Key);
				activities.Should().HaveCount(item.Value);
			}
		}
	}
}