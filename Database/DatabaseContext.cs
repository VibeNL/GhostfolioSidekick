using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace GhostfolioSidekick.Database
{
	public class DatabaseContext : DbContext
	{
		public DbSet<Platform> Platforms { get; set; }

		public DbSet<Account> Accounts { get; set; }

		public DbSet<SymbolProfile> SymbolProfiles { get; set; }

		public DbSet<Activity> Activities { get; set; }

		public DbSet<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; }

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
				optionsBuilder.UseSqlite($"Data Source=ghostfoliosidekick.db");
			}
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
		}
	}
}
