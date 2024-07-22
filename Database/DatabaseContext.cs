using Database.Model;
using Microsoft.EntityFrameworkCore;

namespace Database
{
	public class DatabaseContext : DbContext
	{
		public DbSet<StockSplit> StockSplits { get; set; }

		public string DbPath { get; }

		public DatabaseContext()
		{
			var folder = Environment.SpecialFolder.LocalApplicationData;
			var path = Environment.GetFolderPath(folder);
			DbPath = System.IO.Path.Join(path, "blogging.db");
		}

		// The following configures EF to create a Sqlite database file in the
		// special "local" folder for your platform.
		protected override void OnConfiguring(DbContextOptionsBuilder options)
			=> options.UseSqlite($"Data Source={DbPath}");
	}
}
