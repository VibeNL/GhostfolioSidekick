using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting.Server;

namespace PortfolioViewer.WASM.UITests
{
	// https://danieldonbavand.com/2022/06/13/using-playwright-with-the-webapplicationfactory-to-test-a-blazor-application/
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly", Justification = "Test code")]
	public class CustomWebApplicationFactory : WebApplicationFactory<GhostfolioSidekick.PortfolioViewer.ApiService.Program>
	{
		private IHost? _host;

		public CustomWebApplicationFactory()
		{
			// Ensure WASM publish/copy target is executed
			EnsureWasmPublishedToApiStaticFiles();
		}

		public string ServerAddress
		{
			get
			{
				EnsureServer();
				return ClientOptions.BaseAddress.ToString();
			}
		}

		protected override IHost CreateHost(IHostBuilder builder)
		{
			// Create the host for TestServer now before we
			// modify the builder to use Kestrel instead.
			var testHost = builder.Build();

			// Modify the host builder to use Kestrel instead
			// of TestServer so we can listen on a real address.
			builder.ConfigureWebHost(webHostBuilder => webHostBuilder
														.UseKestrel()
														.UseSetting("ASPNETCORE_ENVIRONMENT", "Production"));


			// Create and start the Kestrel server before the test server,
			// otherwise due to the way the deferred host builder works
			// for minimal hosting, the server will not get "initialized
			// enough" for the address it is listening on to be available.
			// See https://github.com/dotnet/aspnetcore/issues/33846.
			_host = builder.Build();
			_host.Start();

			// Extract the selected dynamic port out of the Kestrel server
			// and assign it onto the client options for convenience so it
			// "just works" as otherwise it'll be the default http://localhost
			// URL, which won't route to the Kestrel-hosted HTTP server.
			var server = _host.Services.GetRequiredService<IServer>();
			var addresses = server.Features.Get<IServerAddressesFeature>();

			ClientOptions.BaseAddress = addresses!.Addresses
				.Select(x => new Uri(x))
				.Last();

			// Return the host that uses TestServer, rather than the real one.
			// Otherwise the internals will complain about the host's server
			// not being an instance of the concrete type TestServer.
			// See https://github.com/dotnet/aspnetcore/pull/34702.
			testHost.Start();
			return testHost;
		}

		protected override void Dispose(bool disposing)
		{
			_host?.Dispose();
		}

		private void EnsureServer()
		{
			if (_host is null)
			{
				// This forces WebApplicationFactory to bootstrap the server
				using var _ = CreateDefaultClient();
			}
		}


		private void EnsureWasmPublishedToApiStaticFiles()
		{
			var solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
			var wasmProj = Path.Combine(solutionDir, "PortfolioViewer", "PortfolioViewer.WASM", "PortfolioViewer.WASM.csproj");
			var apiroot = Path.Combine(solutionDir, "PortfolioViewer", "PortfolioViewer.ApiService");
			var apiDebugWwwroot = Path.Combine(apiroot, "wwwroot");
			var localWwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
			var expectedIndex = Path.Combine(apiDebugWwwroot, "index.html");

			var tempFolder = Path.Combine(Path.GetTempPath() + "WasmPublish");

			// Clean temp folder
			if (Directory.Exists(tempFolder))
			{
				Directory.Delete(tempFolder, true);
			}

			// Delete old debug wwwroot
			if (Directory.Exists(apiDebugWwwroot))
			{
				Directory.Delete(apiDebugWwwroot, true);
			}

			Directory.CreateDirectory(apiDebugWwwroot);

			// Publish WASM project directly into temp folder
			var psi = new System.Diagnostics.ProcessStartInfo("dotnet", $"publish \"{wasmProj}\" -c Release -o \"{tempFolder}\" /p:PublishTrimmed=false")
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
			{
				throw new Exception($"WASM publish failed: {error}\n{output}");
			}

			// Copy published files to API debug wwwroot
			CopyDirectory(Path.Combine(tempFolder, "wwwroot"), apiDebugWwwroot);
			CopyDirectory(Path.Combine(tempFolder, "wwwroot"), localWwwroot);

			// Ensure index.html exists in API debug wwwroot
			if (!File.Exists(expectedIndex))
			{
				throw new FileNotFoundException($"WASM index.html not found in API debug wwwroot: {expectedIndex}");
			}
		}

		private static void CopyDirectory(string tempFolder, string apiWwwroot)
		{
			foreach (var dirPath in Directory.GetDirectories(tempFolder, "*", SearchOption.AllDirectories))
			{
				Directory.CreateDirectory(dirPath.Replace(tempFolder, apiWwwroot));
			}

			foreach (var newPath in Directory.GetFiles(tempFolder, "*.*", SearchOption.AllDirectories))
			{
				File.Copy(newPath, newPath.Replace(tempFolder, apiWwwroot), true);
			}
		}
	}
}
