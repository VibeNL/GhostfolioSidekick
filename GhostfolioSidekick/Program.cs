﻿using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.FileImporter.DeGiro;
using GhostfolioSidekick.FileImporter.ScalableCaptial;
using GhostfolioSidekick.Ghostfolio.API;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick
	{
	internal class Program
		{
		static async Task Main(string[] args)
			{
			var hostBuilder = new HostBuilder()
			.ConfigureAppConfiguration((hostContext, configBuilder) =>
			{
				configBuilder.SetBasePath(Directory.GetCurrentDirectory());
				configBuilder.AddJsonFile("appsettings.json", optional: true);
				configBuilder.AddJsonFile(
					$"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
					optional: true);
				configBuilder.AddEnvironmentVariables();
			})
			.ConfigureLogging((hostContext, configLogging) =>
			{
				configLogging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
				configLogging.AddConsole();
			})
			.ConfigureServices((hostContext, services) =>
			{
				services.AddSingleton<IMemoryCache, MemoryCache>();

				services.AddScoped<IHostedService, TimedHostedService>();
				services.AddSingleton<IGhostfolioAPI, GhostfolioAPI>();
				services.AddScoped<IScheduledWork, FileImporterTask>();

				services.AddScoped<IFileImporter, ScalableCapitalParser>();
				services.AddScoped<IFileImporter, DeGiroParser>();
			});

			await hostBuilder.RunConsoleAsync();
			}
		}
	}