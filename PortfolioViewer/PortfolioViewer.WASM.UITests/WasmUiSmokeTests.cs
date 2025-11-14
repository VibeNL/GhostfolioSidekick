using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Playwright;
using Xunit;
using GhostfolioSidekick.PortfolioViewer.ApiService;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace PortfolioViewer.WASM.UITests
{
	public class CustomWebApplicationFactory : WebApplicationFactory<GhostfolioSidekick.PortfolioViewer.ApiService.Program>
	{
		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			builder.UseKestrel()
				   .UseUrls("http://localhost:0"); // 0 = random port
		}

		public HttpClient GetClientWithRealBaseAddress()
		{
			var client = this.CreateDefaultClient();
			client.BaseAddress = this.Server.BaseAddress;
			return client;
		}

		public string GetServerAddress()
		{
			var addresses = this.Server.Features.Get<IServerAddressesFeature>()?.Addresses;
			return addresses?.FirstOrDefault() ?? throw new InvalidOperationException("Server address not found");
		}
	}

	public class WasmUiSmokeTests : IClassFixture<CustomWebApplicationFactory>
	{
		private readonly CustomWebApplicationFactory _apiFactory;

		public WasmUiSmokeTests(CustomWebApplicationFactory apiFactory)
		{
			_apiFactory = apiFactory;
		}

		private async Task<bool> WaitForEndpointAsync(HttpClient apiClient, string url, int timeoutSeconds = 30)
		{
			var end = DateTime.Now.AddSeconds(timeoutSeconds);
			while (DateTime.Now < end)
			{
				try
				{
					var response = await apiClient.GetAsync(url);
					if (response.IsSuccessStatusCode)
						return true;
				}
				catch { /* Optionally log or handle */ }
				await Task.Delay(1000);
			}
			return false;
		}

		private void EnsureWasmPublishedToApiStaticFiles()
		{
			var solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
			var wasmProj = Path.Combine(solutionDir, "PortfolioViewer", "PortfolioViewer.WASM", "PortfolioViewer.WASM.csproj");
			var apiWwwroot = Path.Combine(solutionDir, "PortfolioViewer", "PortfolioViewer.ApiService", "wwwroot");
			var expectedIndex = Path.Combine(apiWwwroot, "index.html");

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
				throw new FileNotFoundException($"WASM index.html not found in API wwwroot: {expectedIndex}");
		}

		[Fact]
		public async Task MainPage_ShouldLoadSuccessfully()
		{
			// Ensure WASM publish/copy target is executed
			EnsureWasmPublishedToApiStaticFiles();

			// Start API in-process and get the client
			var apiClient = _apiFactory.CreateClient(); // Use default CreateClient
			var apiUrl = "api/auth/health";
			var uiUrl = apiClient.BaseAddress!.ToString(); // This should have the correct port

			// Wait for both endpoints to be available
			var apiReady = await WaitForEndpointAsync(apiClient, apiUrl);
			Assert.True(apiReady, $"API endpoint {apiUrl} did not start");

			var uiReady = await WaitForEndpointAsync(apiClient, uiUrl);
			Assert.True(uiReady, $"UI endpoint {uiUrl} did not start");

			using var playwright = await Playwright.CreateAsync();
			await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
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
			var apiClient = _apiFactory.GetClientWithRealBaseAddress();
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
