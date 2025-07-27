using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace GhostfolioSidekick.Database
{
	public class DatabaseContext : DbContext
	{
		public const string DbFileName = "GhostfolioSidekick.db";

		public virtual DbSet<Platform> Platforms { get; set; }

		public virtual DbSet<Account> Accounts { get; set; }

		public virtual DbSet<CurrencyExchangeProfile> CurrencyExchangeRates { get; set; }

		public virtual DbSet<SymbolProfile> SymbolProfiles { get; set; }

		public virtual DbSet<Activity> Activities { get; set; }

		public virtual DbSet<Holding> Holdings { get; set; }

		public virtual DbSet<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; }

		public virtual DbSet<MarketData> MarketDatas { get; set; }

		// Performance sets
		public virtual DbSet<HoldingAggregated> HoldingAggregateds { get; set; }

		public virtual DbSet<CalculatedSnapshot> CalculatedSnapshots { get; set; }

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
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.UseLazyLoadingProxies();
				optionsBuilder.UseSqlite($"Data Source={DbFileName}");
			}

			base.OnConfiguring(optionsBuilder);
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
