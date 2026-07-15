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
using System.Reflection;

namespace PortfolioViewer.WASM.UITests
{
	// https://danieldonbavand.com/2022/06/13/using-playwright-with-the-webapplicationfactory-to-test-a-blazor-application/
	public class CustomWebApplicationFactory : WebApplicationFactory<GhostfolioSidekick.PortfolioViewer.ApiService.Program>
	{
		public const string TestAccessToken = "test-token-12345";
		private IHost? _host;
		private Microsoft.Data.Sqlite.SqliteConnection _connection;
		private readonly string _dbPath;

		public CustomWebApplicationFactory()
		{
			// Ensure WASM publish/copy target is executed
			EnsureWasmPublishedToApiStaticFiles();

			// Use a file-based SQLite database so EF Core migrations can be applied properly.
			// In-memory SQLite with EnsureCreated() does not create __EFMigrationsHistory,
			// causing GetPendingMigrationsAsync() to fail with "no such table".
			_dbPath = Path.Combine(Path.GetTempPath(), $"GhostfolioSidekickUITest_{Guid.NewGuid():n}.db");

			// Create and open the file-based SQLite connection
			_connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_dbPath}");
			_connection.Open();
		}

		// Cache the first Kestrel server address across instances to avoid per-test host restart
		private static string? _cachedServerAddress;
		private static readonly object _addressLock = new();

		public string ServerAddress
		{
			get
			{
				// Use cached server address if available
				if (!string.IsNullOrEmpty(_cachedServerAddress))
				{
					return _cachedServerAddress;
				}

				// Force the host to start by accessing CreateDefaultClient
				// This triggers CreateHost which sets up Kestrel and ClientOptions.BaseAddress
				using var _ = CreateDefaultClient();
				lock (_addressLock)
				{
					_cachedServerAddress ??= ClientOptions.BaseAddress.ToString();
				}
				return _cachedServerAddress;
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

																	// Remove existing DbContext registrations and replace with file-based SQLite
																	var dbContextDescriptors = services.Where(d =>
																			d.ServiceType == typeof(DbContextOptions<DatabaseContext>) ||
																			d.ServiceType == typeof(DatabaseContext) ||
																			d.ServiceType == typeof(IDbContextFactory<DatabaseContext>))
																			.ToList();

																	// Remove by index to avoid modifying collection during enumeration
																	for (int i = dbContextDescriptors.Count - 1; i >= 0; i--)
																	{
																		services.Remove(dbContextDescriptors[i]);
																	}

																	// Register file-based SQLite database for testing
																	// Important: Share the same connection across all DbContext instances
																	// to ensure the database persists
																	var connectionString = $"Data Source={_dbPath}";
																	_ = services.AddDbContext<DatabaseContext>(options =>
																		_ = options.UseSqlite(connectionString),
																		ServiceLifetime.Scoped);

																	// Also register DbContextFactory with the same connection
																	_ = services.AddDbContextFactory<DatabaseContext>(options =>
																		_ = options.UseSqlite(connectionString),
																		ServiceLifetime.Scoped);
															}));


			// Create and start the Kestrel server before the test server,
			// otherwise due to the way the deferred host builder works
			// for minimal hosting, the server will not get "initialized
			// enough" for the address it is listening on to be available.
			// See https://github.com/dotnet/aspnetcore/issues/33846.
			_host = builder.Build();
			_host.Start();

			// Seed test data
			SeedTestData(_host.Services, ref _connection);

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
			// Clean up the temp database file
			if (File.Exists(_dbPath))
			{
				try
				{
					File.Delete(_dbPath);
				}
				catch
				{
					// Ignore cleanup failures in disposal
				}
			}
			base.Dispose(disposing);
		}

		private void EnsureServer()
		{
			if (_host is null)
			{
				// Forces WebApplicationFactory to bootstrap the server
				using HttpClient _ = CreateDefaultClient();
			}
		}

		public void ResetAndReseedTestData()
		{
			EnsureServer();
			SeedTestData(_host!.Services, ref _connection, resetDatabase: true, dbPath: _dbPath);
		}

		/// <summary>
		/// Returns the test host's services for direct DB access during tests.
		/// </summary>
		public IServiceProvider GetTestHostServices()
		{
			EnsureServer();
			return _host!.Services;
		}

		/// <summary>
		/// Seeds a DividendActivity with an invalid decimal Amount value directly into SQLite.
		/// This reproduces the FormatException that occurs when production data contains corrupted decimal columns.
		/// </summary>
		public void SeedInvalidDividendData()
		{
			EnsureServer();
			using var scope = _host!.Services.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

			// Ensure basic data exists (account, holding)
			var account = db.Accounts.FirstOrDefault();
			if (account == null)
			{
				ResetAndReseedTestData();
				account = db.Accounts.First();
			}

			var holding = db.Holdings.FirstOrDefault();
			if (holding == null)
			{
				ResetAndReseedTestData();
				holding = db.Holdings.First();
			}

			// First create a valid dividend via EF, then corrupt its Amount column
			var baseDate = DateTimeOffset.UtcNow.Date;
			var dividend = new GhostfolioSidekick.Model.Activities.Types.DividendActivity(
				account, holding, [], baseDate.AddDays(-1),
				new GhostfolioSidekick.Model.Money(GhostfolioSidekick.Model.Currency.USD, 50m),
				"DIV-INVALID-001", 0, "Test invalid dividend");
			db.Activities.Add(dividend);
			db.SaveChanges();

			// Corrupt the Amount column to an empty string (simulates corrupted SQLite data)
			// The column name for Money.Amount in DividendActivity is "Amount"
#pragma warning disable EF1003 // Suppress SQL injection warning — this is test code with controlled values
			db.Database.ExecuteSqlRaw(
				"UPDATE Activities SET Amount = '' WHERE Id = " + dividend.Id + " AND Discriminator = 'Dividend'");
#pragma warning restore EF1003
		}


		// Tracks whether WASM has been published this run to avoid redundant publishes
		private static bool _wasmPublished = false;
		private static readonly object _wasmPublishLock = new();

		private static void EnsureWasmPublishedToApiStaticFiles()
		{
			if (_wasmPublished) return;

			lock (_wasmPublishLock)
			{
				if (_wasmPublished) return;

				// Resolve solution dir from assembly location (robust vs AppContext.BaseDirectory)
				var assemblyPath = Assembly.GetExecutingAssembly().Location;
				var testDir = Path.GetDirectoryName(assemblyPath) ?? Directory.GetCurrentDirectory();
				var solutionDir = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", ".."));

				var wasmProj = Path.Combine(solutionDir, "PortfolioViewer", "PortfolioViewer.WASM", "PortfolioViewer.WASM.csproj");
				var apiroot = Path.Combine(solutionDir, "PortfolioViewer", "PortfolioViewer.ApiService");
				var apiDebugWwwroot = Path.Combine(apiroot, "wwwroot");
				var localWwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
				var expectedIndex = Path.Combine(apiDebugWwwroot, "index.html");

				// Skip if already published (e.g., by build)
				if (File.Exists(expectedIndex))
				{
					_wasmPublished = true;
					return;
				}

				var tempFolder = Path.Combine(Path.GetTempPath(), "WasmPublish_" + Guid.NewGuid().ToString("n")[..8]);

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

				// Publish WASM project directly into temp folder (async to avoid deadlock)
				ProcessStartInfo psi = new("dotnet", $"publish \"{wasmProj}\" -c Release -o \"{tempFolder}\" /p:PublishTrimmed=false")
				{
					WorkingDirectory = solutionDir,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false
				};
				using Process proc = System.Diagnostics.Process.Start(psi)!;
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

				_wasmPublished = true;
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

		private static void SeedTestData(IServiceProvider services, ref Microsoft.Data.Sqlite.SqliteConnection connection, bool resetDatabase = false, string? dbPath = null)
		{
			try
			{
				if (resetDatabase)
				{
					// Drop all application tables and reseed (avoids file-lock issues with EnsureDeleted)
					using IServiceScope scope = services.CreateScope();
					DatabaseContext dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

					// Disable foreign keys, drop all tables, then re-enable
					var dbConn = dbContext.Database.GetDbConnection();
					if (dbConn.State == System.Data.ConnectionState.Closed)
					{
						dbConn.Open();
					}
					using var fkCmd = dbConn.CreateCommand();
					fkCmd.CommandText = "PRAGMA foreign_keys = OFF";
					_ = fkCmd.ExecuteNonQuery();

					// Drop all user tables (keep sqlite_sequence for auto-increment)
					var dropTableNames = new List<string>();
					using var dropCmd = dbConn.CreateCommand();
					dropCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
					using var dropReader = dropCmd.ExecuteReader();
					while (dropReader.Read())
					{
						dropTableNames.Add(dropReader.GetString(0));
					}
					dropReader.Dispose();

					foreach (var table in dropTableNames)
					{
						dropCmd.CommandText = $"DROP TABLE [{table}]";
						_ = dropCmd.ExecuteNonQuery();
					}

					// Re-enable foreign keys
					fkCmd.CommandText = "PRAGMA foreign_keys = ON";
					_ = fkCmd.ExecuteNonQuery();

					// Now apply migrations fresh
					dbContext.Database.EnsureCreated();
					dbContext.Database.Migrate();
				}
				else
				{
					// Apply all migrations to set up the schema and __EFMigrationsHistory
					using IServiceScope scope = services.CreateScope();
					DatabaseContext dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
					dbContext.Database.Migrate();
				}

				// Seed all tables via TestDataSeeder (uses shared connection for table listing)
				using IServiceScope scope2 = services.CreateScope();
				DatabaseContext dbContext2 = scope2.ServiceProvider.GetRequiredService<DatabaseContext>();
				TestDataSeeder.Seed(dbContext2);

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
