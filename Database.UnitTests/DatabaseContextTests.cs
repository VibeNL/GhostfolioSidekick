using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using GhostfolioSidekick.Database;
using KellermanSoftware.CompareNetObjects;
using FluentAssertions;

namespace GhostfolioSidekick.Tests
{
	public class DatabaseContextTests
	{
		public DatabaseContextTests()
		{

		}

		[Fact]
		public async Task CanApplyMigrations()
		{
			// Arrange
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite("Data Source=:memory:")
				.Options;

			using (var context = new DatabaseContext(options))
			{
				context.Database.OpenConnection();

				// Act
				await context.Database.MigrateAsync().ConfigureAwait(false);

				// Assert
				var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
				pendingMigrations.Should().BeEmpty();

				// Check if table Holding exists
				var tableExists = await context.Database.ExecuteSqlRawAsync("SELECT name FROM sqlite_master WHERE type='table' AND name='Holdings';");
			}
		}
	}
}
