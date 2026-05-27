using GhostfolioSidekick.ProcessHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

IHost host = Host.CreateDefaultBuilder(args)
	.ConfigureServices((_, services) =>
	{
		// Each ChildProcessService instance is registered as a distinct hosted service.
		// The DLL paths are relative to the working directory (/app inside the container).
		services.AddSingleton<IHostedService>(sp =>
			new ChildProcessService(
				"GhostfolioSidekick.PortfolioViewer.ApiService.dll",
				sp.GetRequiredService<ILogger<ChildProcessService>>()));

		services.AddSingleton<IHostedService>(sp =>
			new ChildProcessService(
				"GhostfolioSidekick.dll",
				sp.GetRequiredService<ILogger<ChildProcessService>>()));
	})
	.Build();

await host.RunAsync();
