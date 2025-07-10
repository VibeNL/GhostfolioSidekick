using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace GhostfolioSidekick.Database
{
	public class DatabaseContext : DbContext
	{
		public virtual DbSet<Platform> Platforms { get; set; }

		public virtual DbSet<Account> Accounts { get; set; }

		public virtual DbSet<SymbolProfile> SymbolProfiles { get; set; }

		public virtual DbSet<Activity> Activities { get; set; }

		public virtual DbSet<Holding> Holdings { get; set; }

		public virtual DbSet<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; }

		public virtual DbSet<MarketData> MarketDatas { get; set; }

		public DatabaseContext()
		{
		}

		public DatabaseContext(DbContextOptions<DatabaseContext> options)
			: base(options)
		{
		}

		// The following configures EF to create a Sqlite database file in the
		// special "local" folder for your platform.
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			////optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.MultipleCollectionIncludeWarning));
			////optionsBuilder.ConfigureWarnings(w => w.Ignore(CoreEventId.DuplicateDependentEntityTypeInstanceWarning)); // We do not duplicate Currency instances

			optionsBuilder.UseLazyLoadingProxies();
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.UseSqlite($"Data Source=ghostfoliosidekick.db");
			}

			base.OnConfiguring(optionsBuilder);

			//optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information)
			//.EnableSensitiveDataLogging()
			//.EnableDetailedErrors();
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
		}

		protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
		{
			base.ConfigureConventions(configurationBuilder);
			configurationBuilder.DefaultTypeMapping<decimal>(builder => builder.HasPrecision(18, 8));
		}

		public Task ExecutePragma(string pragmaCommand)
		{
			var connection = Database.GetDbConnection();
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandText = pragmaCommand;
			return command.ExecuteNonQueryAsync();
		}

		public virtual IQueryable<T> SqlQueryRaw<T>(string sql)
		{
			return Database.SqlQueryRaw<T>(sql);
		}

		public virtual Task<int> ExecuteSqlRawAsync(string sql, CancellationToken cancellationToken)
		{
			return Database.ExecuteSqlRawAsync(sql, cancellationToken);
		}

		public async Task<List<Dictionary<string, object?>>> ExecuteDynamicQuery(string sql)
		{
			await using var command = Database.GetDbConnection().CreateCommand();
			command.CommandText = sql;

			await Database.OpenConnectionAsync();

			var result = new List<Dictionary<string, object?>>();

			await using var reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				var row = new Dictionary<string, object?>();

				for (int i = 0; i < reader.FieldCount; i++)
				{
					var columnName = reader.GetName(i);
					row[columnName] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
				}

				result.Add(row);
			}

			await Database.CloseConnectionAsync();
			return result;
		}
	}
}
