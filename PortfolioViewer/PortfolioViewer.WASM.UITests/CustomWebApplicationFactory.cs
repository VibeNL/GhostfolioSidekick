using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Cache;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Model.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Data.Common;
using System.Diagnostics;

namespace PortfolioViewer.WASM.UITests
{
	// https://danieldonbavand.com/2022/06/13/using-playwright-with-the-webapplicationfactory-to-test-a-blazor-application/
	public class CustomWebApplicationFactory : WebApplicationFactory<GhostfolioSidekick.PortfolioViewer.ApiService.Program>
	{
		public const string TestAccessToken = "test-token-12345";
		private IHost? _host;
		private readonly Microsoft.Data.Sqlite.SqliteConnection? _connection;

		public CustomWebApplicationFactory()
		{
			// Ensure WASM publish/copy target is executed
			EnsureWasmPublishedToApiStaticFiles();

			// Create and open an in-memory SQLite connection using a named in-memory database ("TestDb")
			// with a shared cache. We keep a single connection open for the lifetime of the factory
			// and share that connection across all DbContext instances.
			_connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=TestDb;Mode=Memory;Cache=Shared");
			_connection.Open();
		}

		public string ServerAddress
		{
			get
			{
				// Force the host to start by accessing CreateDefaultClient
				// This triggers CreateHost which sets up Kestrel and ClientOptions.BaseAddress
				using var _ = CreateDefaultClient();
				return ClientOptions.BaseAddress.ToString();
			}
		}

		protected override IHost CreateHost(IHostBuilder builder)
		{
			// Create the host for TestServer now before we
			// modify the builder to use Kestrel instead.
			IHost testHost = builder.Build();

			// Modify the host builder to use Kestrel instead
			// of TestServer so we can listen on a real address.
			_ = builder.ConfigureWebHost(webHostBuilder => webHostBuilder
														.UseKestrel()
														.UseUrls("http://127.0.0.1:0") // Use dynamic port (0 = auto-select available port)
														.UseSetting("ASPNETCORE_ENVIRONMENT", "Production")
															.ConfigureServices(services =>
															{
																// Replace IApplicationSettings with a mock that provides the test token
																Mock<IApplicationSettings> mockSettings = new();
																_ = mockSettings.Setup(x => x.GhostfolioAccessToken).Returns(TestAccessToken);

																// Remove existing registration and add our mock
																ServiceDescriptor? descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IApplicationSettings));
																if (descriptor != null)
																{
																	_ = services.Remove(descriptor);
																}
																_ = services.AddSingleton(mockSettings.Object);

																// Remove existing DbContext registrations and replace with in-memory SQLite
																List<ServiceDescriptor> dbContextDescriptors = services.Where(d =>
																	d.ServiceType == typeof(DbContextOptions<DatabaseContext>) ||
																	d.ServiceType == typeof(DatabaseContext) ||
																	d.ServiceType == typeof(IDbContextFactory<DatabaseContext>))
																	.ToList();


																foreach (ServiceDescriptor? desc in dbContextDescriptors)
																{
																	_ = services.Remove(desc);
																}

																// Register in-memory SQLite database for testing
																// Important: Share the same connection across all DbContext instances
																// to ensure in-memory database persists
																_ = services.AddDbContext<DatabaseContext>(options =>
																{
																	// Use the shared in-memory connection
																	// Don't let EF Core manage the connection (we keep it open)
																	_ = options.UseSqlite("DataSource=TestDb;Mode=Memory;Cache=Shared");
																}, ServiceLifetime.Scoped, ServiceLifetime.Scoped);

																// Also register DbContextFactory with the same connection
																_ = services.AddDbContextFactory<DatabaseContext>(options =>
																{
																	_ = options.UseSqlite("DataSource=TestDb;Mode=Memory;Cache=Shared");
																}, ServiceLifetime.Scoped);
															}));


			// Create and start the Kestrel server before the test server,
			// otherwise due to the way the deferred host builder works
			// for minimal hosting, the server will not get "initialized
			// enough" for the address it is listening on to be available.
			// See https://github.com/dotnet/aspnetcore/issues/33846.
			_host = builder.Build();
			_host.Start();

			// Seed test data
			SeedTestData(_host.Services, _connection);

			// Extract the selected dynamic port out of the Kestrel server
			// and assign it onto the client options for convenience so it
			// "just works" as otherwise it'll be the default http://localhost
			// URL, which won't route to the Kestrel-hosted HTTP server.
			IServer server = _host.Services.GetRequiredService<IServer>();
			IServerAddressesFeature? addresses = server.Features.Get<IServerAddressesFeature>();

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
			_connection?.Dispose();
			base.Dispose(disposing);
		}

		private void EnsureServer()
		{
			if (_host is null)
			{
				// This forces WebApplicationFactory to bootstrap the server
				using HttpClient _ = CreateDefaultClient();
			}
		}


		private static void EnsureWasmPublishedToApiStaticFiles()
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

			_ = Directory.CreateDirectory(apiDebugWwwroot);

			// Publish WASM project directly into temp folder
			ProcessStartInfo psi = new("dotnet", $"publish \"{wasmProj}\" -c Release -o \"{tempFolder}\" /p:PublishTrimmed=false")
			{
				WorkingDirectory = solutionDir,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			using Process proc = System.Diagnostics.Process.Start(psi)!;
			// Read output and error asynchronously to avoid deadlock
			Task<string> outputTask = proc.StandardOutput.ReadToEndAsync();
			Task<string> errorTask = proc.StandardError.ReadToEndAsync();
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
				_ = Directory.CreateDirectory(dirPath.Replace(tempFolder, apiWwwroot));
			}

			foreach (var newPath in Directory.GetFiles(tempFolder, "*.*", SearchOption.AllDirectories))
			{
				File.Copy(newPath, newPath.Replace(tempFolder, apiWwwroot), true);
			}
		}

		private static void SeedTestData(IServiceProvider services, Microsoft.Data.Sqlite.SqliteConnection? connection)
		{
			try
			{
				using IServiceScope scope = services.CreateScope();
				DatabaseContext dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

				// Create the database schema for the in-memory database
				_ = dbContext.Database.EnsureCreated();

				// Seed all tables via TestDataSeeder
				TestDataSeeder.Seed(dbContext);

				// List all tables in the database
				connection!.Open();
				using DbCommand command = connection.CreateCommand();
				command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
				using DbDataReader reader = command.ExecuteReader();
				var tables = new List<string>();
				while (reader.Read())
				{
					var name = reader.GetString(0);
					tables.Add(name);
				}

				Console.WriteLine($"Test data seeded. Tables ({tables.Count}):");
				foreach (var t in tables)
				{
					using DbCommand cmd = connection.CreateCommand();
					cmd.CommandText = $"SELECT COUNT(*) FROM [{t}]";
					var count = cmd.ExecuteScalar();
					Console.WriteLine($"  - {t}: {count} rows");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error seeding test data: {ex.Message}");
				Console.WriteLine($"Stack trace: {ex.StackTrace}");
				throw;
			}
		}
	}
}


