using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;
using Xunit;
using GhostfolioSidekick.PortfolioViewer.ApiService;

namespace PortfolioViewer.WASM.UITests
{
	public class WasmUiSmokeTests : IClassFixture<WebApplicationFactory<GhostfolioSidekick.PortfolioViewer.ApiService.Program>>
	{
		private readonly WebApplicationFactory<GhostfolioSidekick.PortfolioViewer.ApiService.Program> _apiFactory;

		public WasmUiSmokeTests(WebApplicationFactory<GhostfolioSidekick.PortfolioViewer.ApiService.Program> apiFactory)
		{
			_apiFactory = apiFactory;
		}

		private async Task<bool> WaitForEndpointAsync(string url, int timeoutSeconds = 30)
		{
			using var httpClient = new HttpClient();
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

		[Fact]
		public async Task MainPage_ShouldLoadSuccessfully()
		{
			var solutionDir = AppDomain.CurrentDomain.BaseDirectory;
			var wasmProjectPath = System.IO.Path.GetFullPath("../../../../PortfolioViewer.WASM", solutionDir);
			var uiUrl = "http://localhost:5252";

			// Start API in-process
			var apiClient = _apiFactory.CreateClient();
			var apiUrl = "api/auth/health";

			using var wasmHost = new WasmTestHost(wasmProjectPath, 5252);
			await wasmHost.StartAsync();

			try
			{
				// Wait for both endpoints to be available
				var apiReady = await WaitForEndpointAsync(apiUrl);
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
			finally
			{
				await wasmHost.StopAsync();
			}
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
