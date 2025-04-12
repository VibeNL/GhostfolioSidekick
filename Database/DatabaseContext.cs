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
			////optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.MultipleCollectionIncludeWarning));
			////optionsBuilder.ConfigureWarnings(w => w.Ignore(CoreEventId.DuplicateDependentEntityTypeInstanceWarning)); // We do not duplicate Currency instances
			
			optionsBuilder.UseLazyLoadingProxies();
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.UseSqlite($"Data Source=ghostfoliosidekick.db");
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
