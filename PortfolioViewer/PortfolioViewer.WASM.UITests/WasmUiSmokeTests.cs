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
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace PortfolioViewer.WASM.UITests
{
	public class CustomWebApplicationFactory : WebApplicationFactory<GhostfolioSidekick.PortfolioViewer.ApiService.Program>
	{
		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			builder.UseKestrel(options =>
			{
				// Listen on localhost with a random available port
				options.ListenLocalhost(0);
			});
		}

		public HttpClient GetClientWithRealBaseAddress()
		{
			var client = this.CreateDefaultClient();
			client.BaseAddress = this.Server.BaseAddress;
			return client;
		}

		public string GetServerAddress()
		{
			// Force the server to start by creating a client if not already started
			if (this.Server.BaseAddress == null)
			{
				_ = this.CreateClient(); // This will trigger server startup
			}

			// First try to get the base address from the server
			if (this.Server.BaseAddress != null && this.Server.BaseAddress.Port > 0)
			{
				return this.Server.BaseAddress.ToString();
			}

			// Fallback to server features - get addresses and find one with an actual port
			var addresses = this.Server.Features.Get<IServerAddressesFeature>()?.Addresses;
			if (addresses != null)
			{
				var validAddress = addresses.FirstOrDefault(a => !a.Contains(":0") && Uri.TryCreate(a, UriKind.Absolute, out _));
				if (validAddress != null)
				{
					return validAddress;
				}
			}

			throw new InvalidOperationException($"Server address not found. BaseAddress: {this.Server.BaseAddress}, Features: {string.Join(", ", addresses ?? new[] { "none" })}");
		}
	}

	public class WasmUiSmokeTests : IClassFixture<CustomWebApplicationFactory>
	{
		private readonly CustomWebApplicationFactory _apiFactory;

		public WasmUiSmokeTests(CustomWebApplicationFactory apiFactory)
		{
			_apiFactory = apiFactory;
		}

		private async Task<bool> WaitForEndpointAsync(string url, int timeoutSeconds = 30)
		{
			var end = DateTime.Now.AddSeconds(timeoutSeconds);
			using var httpClient = new HttpClient();
			
			while (DateTime.Now < end)
			{
				try
				{
					Console.WriteLine($"Checking endpoint: {url}");
					var response = await httpClient.GetAsync(url);
					Console.WriteLine($"Response: {response.StatusCode}");
					
					if (response.IsSuccessStatusCode)
						return true;
						
					// Log response content for debugging
					var content = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"Response content length: {content.Length}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Exception checking endpoint {url}: {ex.Message}");
				}
				await Task.Delay(1000);
			}
			return false;
		}

		private async Task<bool> WaitForRelativeEndpointAsync(HttpClient apiClient, string relativeUrl, int timeoutSeconds = 30)
		{
			var end = DateTime.Now.AddSeconds(timeoutSeconds);
			while (DateTime.Now < end)
			{
				try
				{
					var response = await apiClient.GetAsync(relativeUrl);
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
			var apiClient = _apiFactory.CreateClient();
			
			// Force the server to start and get actual address
			try 
			{ 
				await apiClient.GetAsync("api/auth/health"); 
			} 
			catch (Exception ex) 
			{ 
				Console.WriteLine($"Initial server start call failed: {ex.Message}"); 
			}
			
			var serverUrl = _apiFactory.GetServerAddress();
			
			Console.WriteLine($"Server.BaseAddress: {_apiFactory.Server.BaseAddress}");
			Console.WriteLine($"Client.BaseAddress: {apiClient.BaseAddress}");
			Console.WriteLine($"Using server URL: {serverUrl}");

			// Wait for both endpoints to be available using appropriate methods
			var apiReady = await WaitForRelativeEndpointAsync(apiClient, "api/auth/health");
			Assert.True(apiReady, "API endpoint api/auth/health did not start");

			var uiReady = await WaitForEndpointAsync(serverUrl);
			Assert.True(uiReady, $"UI endpoint {serverUrl} did not start");

			using var playwright = await Playwright.CreateAsync();
			await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
			var page = await browser.NewPageAsync();

			Console.WriteLine($"Navigating to: {serverUrl}");
			await page.GotoAsync(serverUrl);

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

		[Fact]
		public async Task ServerShouldBeAccessibleExternally()
		{
			// Ensure WASM publish/copy target is executed
			EnsureWasmPublishedToApiStaticFiles();

			// Start the server by creating a client first
			var apiClient = _apiFactory.CreateClient();
			
			// Force the server to start and get actual address
			try 
			{ 
				await apiClient.GetAsync("api/auth/health"); 
			} 
			catch (Exception ex) 
			{ 
				Console.WriteLine($"Initial server start call failed: {ex.Message}"); 
			}
			
			var serverUrl = _apiFactory.GetServerAddress();
			
			Console.WriteLine($"Testing external access to: {serverUrl}");
			Console.WriteLine($"Server.BaseAddress: {_apiFactory.Server.BaseAddress}");
			
			// Test API endpoint first via in-process client
			var apiReady = await WaitForRelativeEndpointAsync(apiClient, "api/auth/health", 10);
			Assert.True(apiReady, "API endpoint should be accessible via in-process client");
			
			// Test external access to the server URL
			var externallyAccessible = await WaitForEndpointAsync(serverUrl, 10);
			
			Console.WriteLine($"External accessibility result: {externallyAccessible}");
			
			if (!externallyAccessible)
			{
				// Additional diagnostic
				using var externalClient = new HttpClient();
				try
				{
					var response = await externalClient.GetAsync(serverUrl);
					Console.WriteLine($"External client response: {response.StatusCode}");
					var content = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"External client content: {content.Substring(0, Math.Min(200, content.Length))}...");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"External client exception: {ex}");
				}

				// Debug server features
				var addresses = _apiFactory.Server.Features.Get<IServerAddressesFeature>()?.Addresses;
				Console.WriteLine($"Server addresses from features: {string.Join(", ", addresses ?? new[] { "null" })}");
			}
			
			Assert.True(externallyAccessible, $"Server at {serverUrl} should be accessible externally");
		}
	}
}
