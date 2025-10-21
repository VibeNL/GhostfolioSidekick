using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Diagnostics;

namespace GhostfolioSidekick.Tools.ScraperUtilities
{
	public partial class Program
	{
		public static async Task Main(string[] args)
		{
			// Try to start Chrome directly (without using StartChrome.bat)
			try
			{
				StartChrome();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to start Chrome directly: {ex.Message}");
			}

			var host = CreateHostBuilder(args).Build();
			await host.RunAsync();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureServices((context, services) =>
				{
					services.AddLogging(configure => configure.AddConsole());
					services.AddSingleton(Playwright.CreateAsync().Result);
					services.AddHostedService<ScraperService>();
				});

		private static void StartChrome()
		{
			const string args = "--remote-debugging-port=9222 --user-data-dir=\"C:\\temp\\chrome-debug\"";

			// Common Windows install locations
			var candidates = new[]
			{
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe"),
			};

			var chromePath = candidates.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));

			ProcessStartInfo psi;
			if (!string.IsNullOrWhiteSpace(chromePath))
			{
				Console.WriteLine($"Starting Chrome from: {chromePath}");
				psi = new ProcessStartInfo
				{
					FileName = chromePath,
					Arguments = args,
					UseShellExecute = true,
					CreateNoWindow = false,
				};
			}
			else
			{
				// Fallback: try to start using chrome on PATH
				Console.WriteLine("Chrome executable not found in common locations; trying 'chrome' from PATH.");
				psi = new ProcessStartInfo
				{
					FileName = "chrome",
					Arguments = args,
					UseShellExecute = true,
					CreateNoWindow = false,
				};
			}

			try
			{
				var process = Process.Start(psi);
				if (process == null)
				{
					Console.WriteLine("Failed to start Chrome process (Process.Start returned null).");
				}
				else
				{
					Console.WriteLine("Chrome started successfully.");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error launching Chrome: {ex.Message}");
			}
		}

	}
}
