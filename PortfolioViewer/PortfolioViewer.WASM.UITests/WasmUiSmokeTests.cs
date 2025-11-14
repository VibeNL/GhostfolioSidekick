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
	}
}
