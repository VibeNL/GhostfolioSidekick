using Flurl.Http;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick
{
	/// <summary>
	/// Entry point for the sidekick host, usable both standalone and embedded in another process (e.g. the PortfolioViewer API service).
	/// </summary>
	public static class SidekickHost
	{
		/// <summary>Builds the sidekick host without starting it.</summary>
		public static IHost Build()
		{
			IHostBuilder hostBuilder = Program.CreateHostBuilder();
			IHost host = hostBuilder.Build();

			FlurlHttp.Configure(settings =>
			{
				settings.HttpClientFactory = new InternalCacheHttpFactory(host.Services);
			});

			return host;
		}

		/// <summary>Builds and runs the sidekick host until it is stopped.</summary>
		[ExcludeFromCodeCoverage]
		public static async Task RunAsync()
		{
			using IHost host = Build();
			await host.RunAsync().ConfigureAwait(false);
		}
	}
}
