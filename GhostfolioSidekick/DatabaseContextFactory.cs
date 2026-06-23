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
		connection.Open();

		// Set busy_timeout on every new connection
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "PRAGMA busy_timeout=5000;";
		cmd.ExecuteNonQuery();

		var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
		optionsBuilder.UseSqlite(connection);
		optionsBuilder.UseLazyLoadingProxies();

		return new DatabaseContext(optionsBuilder.Options);
	}
}
