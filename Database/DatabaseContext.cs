﻿using GhostfolioSidekick.Database.Model;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace GhostfolioSidekick.Database
{
	public class DatabaseContext : DbContext
	{
		public DbSet<StockSplitList> StockSplitLists { get; set; }

		public DbSet<StockSplit> StockSplits { get; set; }

		public DbSet<SymbolProfile> SymbolProfiles { get; set; }

		public DbSet<Currency> Currencies { get; set; }

		public string DbPath { get; }

		public DatabaseContext()
		{
			var folder = Environment.SpecialFolder.LocalApplicationData;
			var path = Environment.GetFolderPath(folder);
			DbPath = Path.Join(path, "ghostfoliosidekick.db");
		}

		// The following configures EF to create a Sqlite database file in the
		// special "local" folder for your platform.
		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			options.UseSqlite($"Data Source={DbPath}");
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
		}
	}
}