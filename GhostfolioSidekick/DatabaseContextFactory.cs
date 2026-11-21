using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GhostfolioSidekick;

/// <summary>
/// Custom IDbContextFactory that sets PRAGMA busy_timeout on every connection to prevent "database is locked" errors on network drives.
/// </summary>
public class DatabaseContextFactory : IDbContextFactory<DatabaseContext>
{
	private readonly IServiceProvider _serviceProvider;

	public DatabaseContextFactory(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	public DatabaseContext CreateDbContext()
	{
		var settings = _serviceProvider.GetRequiredService<IApplicationSettings>();
		var dbPath = settings.DatabaseFilePath;

		var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
		try
		{
			connection.Open();
		}
		catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 14) // SQLITE_CANTOPEN: file cannot be opened.
		{
			throw new InvalidOperationException(CreateDatabaseOpenErrorMessage(dbPath), ex);
		}

		// Set busy_timeout on every new connection
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "PRAGMA busy_timeout=5000;";
		cmd.ExecuteNonQuery();

		var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
		optionsBuilder.UseSqlite(connection);
		optionsBuilder.UseLazyLoadingProxies();

		return new DatabaseContext(optionsBuilder.Options);
	}

	internal static string CreateDatabaseOpenErrorMessage(string dbPath)
	{
		string? directory = Path.GetDirectoryName(Path.GetFullPath(dbPath));
		if (directory is not null && !Directory.Exists(directory))
		{
			return $"Cannot open database file '{dbPath}': the directory '{directory}' does not exist. Set DATABASE_PATH or FILEIMPORTER_PATH to an existing, writable location.";
		}

		return $"Cannot open database file '{dbPath}'. The current user cannot write to '{directory}'. In Docker the container runs as unprivileged user 'app' (uid 1654): for data volumes created by older image versions run once: docker run --rm -v <your-data-volume>:/data alpine chown 1654:1654 /data. Also check that the volume is not mounted read-only.";
	}
}
