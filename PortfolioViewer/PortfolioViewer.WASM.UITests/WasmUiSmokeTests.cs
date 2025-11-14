using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using GhostfolioSidekick.PortfolioViewer.ApiService;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace PortfolioViewer.WASM.UITests
{
	public class WasmUiSmokeTests(CustomWebApplicationFactory fixture) : IClassFixture<CustomWebApplicationFactory>
	{
		private readonly string serverAddress = fixture.ServerAddress;

		[Fact]
		public async Task MainPage_ShouldLoadSuccessfully()
		{
			// Ensure WASM publish/copy target is executed
			EnsureWasmPublishedToApiStaticFiles();
						
			using var playwright = await Playwright.CreateAsync();
			await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
			var page = await browser.NewPageAsync();

			Console.WriteLine($"Navigating to: {serverAddress}");
			await page.GotoAsync(serverAddress);

			// Use Playwright's CountAsync to check for header existence
			var mainHeaderCount = await page.Locator("h1:has-text('Portfolio Viewer')").CountAsync();
			Assert.True(mainHeaderCount > 0, "Main header not found");
		}

		[Fact]
		public async Task DebugApiHealthEndpoint()
		{
			// Log the API base address
			var apiClient = fixture.CreateDefaultClient();
			var baseAddress = apiClient.BaseAddress?.ToString() ?? "null";
			Console.WriteLine($"API BaseAddress: {baseAddress}");

			// Try to call the health endpoint directly
			var healthUrl = "api/auth/health";
			try
			{
				var response = await apiClient.GetAsync(healthUrl);
				var content = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Status: {response.StatusCode}, Content: {content}");
				Assert.True(response.IsSuccessStatusCode, $"API health endpoint failed: {response.StatusCode} {content}");
			}
			catch (Exception ex)
			{
				Assert.Fail($"Exception calling API health endpoint: {ex}");
			}
		}

		private void EnsureWasmPublishedToApiStaticFiles()
		{
			var solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
			var wasmProj = Path.Combine(solutionDir, "PortfolioViewer", "PortfolioViewer.WASM", "PortfolioViewer.WASM.csproj");
			var apiWwwroot = Path.Combine(solutionDir, "PortfolioViewer", "PortfolioViewer.ApiService", "wwwroot");
			var expectedIndex = Path.Combine(apiWwwroot, "index.html");

			File.Delete(expectedIndex); // Force re-publish

			// Publish WASM project (triggers MSBuild target to copy files)
			var psi = new System.Diagnostics.ProcessStartInfo("dotnet", $"publish \"{wasmProj}\" -c Release")
			{
				WorkingDirectory = solutionDir,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			using var proc = System.Diagnostics.Process.Start(psi)!;
			// Read output and error asynchronously to avoid deadlock
			var outputTask = proc.StandardOutput.ReadToEndAsync();
			var errorTask = proc.StandardError.ReadToEndAsync();
			proc.WaitForExit();
			var output = outputTask.Result;
			var error = errorTask.Result;
			if (proc.ExitCode != 0)
				throw new Exception($"WASM publish failed: {error}\n{output}");

			// Ensure index.html exists in API wwwroot
			if (!File.Exists(expectedIndex))
			{
				throw new FileNotFoundException($"WASM index.html not found in API wwwroot: {expectedIndex}");
			}
		}
	}
}
