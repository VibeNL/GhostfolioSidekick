using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Diagnostics;

namespace GhostfolioSidekick.Tools.ScraperUtilities
{
	public static class Program
	{
		private static Process? _chromeProcess;

		public static async Task Main(string[] args)
		{
			// Try to start Chrome directly (without using StartChrome.bat)
			try
			{
				_chromeProcess = StartChrome();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to start Chrome directly: {ex.Message}");
			}

			var host = CreateHostBuilder(args).Build();

			RegisterChromeCleanup(host);

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

		private static void RegisterChromeCleanup(IHost host)
		{
			// Register cleanup to ensure Chrome process is terminated when the host is stopping
			var lifetime = host.Services.GetService<IHostApplicationLifetime>();
			if (lifetime != null)
			{
				lifetime.ApplicationStopping.Register(() =>
				{
					try
					{
						if (_chromeProcess != null && !_chromeProcess.HasExited)
						{
							Console.WriteLine("Stopping Chrome process...");
							// Attempt to kill the process and its children
							_chromeProcess.Kill(entireProcessTree: true);
							_chromeProcess.WaitForExit(5000);
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Failed to stop Chrome process: {ex.Message}");
					}
				});

				// Fallback: ensure process killed on process exit as well
				AppDomain.CurrentDomain.ProcessExit += (s, e) =>
				{
					try
					{
						if (_chromeProcess != null && !_chromeProcess.HasExited)
						{
							_chromeProcess.Kill(entireProcessTree: true);
							_chromeProcess.WaitForExit(2000);
						}
					}
					catch
					{
						// swallow exceptions during process exit
					}
				};
			}
		}

		private static Process? StartChrome()
		{
			// Start Chrome minimized so it doesn't steal focus from the console
			const string args = "--remote-debugging-port=9222 --user-data-dir=\"C:\\temp\\chrome-debug\" --start-minimized";

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
					WindowStyle = ProcessWindowStyle.Minimized
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
					WindowStyle = ProcessWindowStyle.Minimized
				};
			}

			try
			{
				var process = Process.Start(psi);
				if (process == null)
				{
					Console.WriteLine("Failed to start Chrome process (Process.Start returned null).");
					return null;
				}
				else
				{
					Console.WriteLine("Chrome started successfully.");
					return process;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error launching Chrome: {ex.Message}");
				return null;
			}
		}

	}
}
