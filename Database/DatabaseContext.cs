using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
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
			optionsBuilder.UseLazyLoadingProxies();
			if (!optionsBuilder.IsConfigured)
			{
				var databaseProvider = Environment.GetEnvironmentVariable("DATABASE_PROVIDER");
				var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

				switch (databaseProvider)
				{
					case "SQLServer":
						optionsBuilder.UseSqlServer(connectionString);
						break;
					case "SQLite":
					default:
						optionsBuilder.UseSqlite(connectionString ?? "Data Source=ghostfoliosidekick.db");
						break;
				}
			}
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
	}
}
