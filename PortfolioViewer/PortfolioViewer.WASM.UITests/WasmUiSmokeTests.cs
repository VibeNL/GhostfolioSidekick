using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;
using Xunit;
using GhostfolioSidekick.PortfolioViewer.ApiService;
using System.IO;
using System.Linq;

namespace PortfolioViewer.WASM.UITests
{
	public class WasmUiSmokeTests : IClassFixture<WebApplicationFactory<GhostfolioSidekick.PortfolioViewer.ApiService.Program>>
	{
		private readonly WebApplicationFactory<GhostfolioSidekick.PortfolioViewer.ApiService.Program> _apiFactory;

		public WasmUiSmokeTests(WebApplicationFactory<GhostfolioSidekick.PortfolioViewer.ApiService.Program> apiFactory)
		{
			_apiFactory = apiFactory;
		}

		private async Task<bool> WaitForEndpointAsync(string url, HttpClient? apiClient = null, int timeoutSeconds = 30)
		{
			using var httpClient = apiClient ?? new HttpClient();
			var end = DateTime.Now.AddSeconds(timeoutSeconds);
			while (DateTime.Now < end)
			{
				try
				{
					var response = await httpClient.GetAsync(url);
					if (response.IsSuccessStatusCode)
						return true;
				}
				catch { /* Optionally log or handle */ }
				await Task.Delay(1000);
			}
			return false;
		}

		private void CopyWasmToApiStaticFiles()
		{
			// Paths
			var solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
			var wasmProj = Path.Combine(solutionDir, "PortfolioViewer", "PortfolioViewer.WASM", "PortfolioViewer.WASM.csproj");
			var wasmPublishDir = Path.Combine(solutionDir, "PortfolioViewer", "PortfolioViewer.WASM", "bin", "Release", "net9.0", "publish", "wwwroot");
			var apiWwwroot = Path.Combine(solutionDir, "PortfolioViewer", "PortfolioViewer.ApiService", "wwwroot");

			// Publish WASM project
			var psi = new System.Diagnostics.ProcessStartInfo("dotnet", $"publish \"{wasmProj}\" -c Release")
			{
				WorkingDirectory = solutionDir,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			using var proc = System.Diagnostics.Process.Start(psi)!;
			proc.WaitForExit();
			if (proc.ExitCode != 0)
				throw new Exception($"WASM publish failed: {proc.StandardError.ReadToEnd()}");

			// Copy files
			if (!Directory.Exists(wasmPublishDir))
				throw new DirectoryNotFoundException($"WASM publish dir not found: {wasmPublishDir}");
			if (!Directory.Exists(apiWwwroot))
				Directory.CreateDirectory(apiWwwroot);

			foreach (var file in Directory.EnumerateFiles(wasmPublishDir, "*", SearchOption.AllDirectories))
			{
				var relPath = Path.GetRelativePath(wasmPublishDir, file);
				var destPath = Path.Combine(apiWwwroot, relPath);
				Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
				File.Copy(file, destPath, true);
			}
		}

		[Fact]
		public async Task MainPage_ShouldLoadSuccessfully()
		{
			// Copy WASM files to API wwwroot
			CopyWasmToApiStaticFiles();

			// Start API in-process
			var apiClient = _apiFactory.CreateClient();
			var apiUrl = "api/auth/health";
			var uiUrl = apiClient.BaseAddress!.ToString()!;

			// Wait for both endpoints to be available
			var apiReady = await WaitForEndpointAsync(apiUrl, apiClient);
			Assert.True(apiReady, $"API endpoint {apiUrl} did not start");

			var uiReady = await WaitForEndpointAsync(uiUrl);
			Assert.True(uiReady, $"UI endpoint {uiUrl} did not start");

			using var playwright = await Playwright.CreateAsync();
			await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
			var page = await browser.NewPageAsync();

			await page.GotoAsync(uiUrl);

			// Use Playwright's CountAsync to check for header existence
			var mainHeaderCount = await page.Locator("h1:has-text('Portfolio Viewer')").CountAsync();
			Assert.True(mainHeaderCount > 0, "Main header not found");
		}

		[Fact]
		public async Task DebugApiHealthEndpoint()
		{
			// Log the API base address
			var apiClient = _apiFactory.CreateClient();
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
	}
}
