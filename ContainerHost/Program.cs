using GhostfolioSidekick;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.ContainerHost
{
	/// <summary>
	/// Unified single-process host for the container image: runs the PortfolioViewer API service and the sidekick importer in one process.
	/// </summary>
	[ExcludeFromCodeCoverage]
	public class Program
	{
		protected Program()
		{
			// Protected constructor to prevent instantiation
		}

		private static async Task Main(string[] args)
		{
			var app = PortfolioViewer.ApiService.Program.BuildApp(args);
			IHost sidekick = SidekickHost.Build();

			using CancellationTokenSource shutdown = new();
			Task webTask = app.RunAsync(shutdown.Token);
			Task sidekickTask = sidekick.RunAsync(shutdown.Token);

			try
			{
				// Stop both hosts when either one stops (shutdown signal or crash).
				await Task.WhenAny(webTask, sidekickTask).ConfigureAwait(false);
				await shutdown.CancelAsync().ConfigureAwait(false);
				await Task.WhenAll(webTask, sidekickTask).ConfigureAwait(false);
			}
			finally
			{
				sidekick.Dispose();
				await app.DisposeAsync().ConfigureAwait(false);
			}
		}
	}
}
