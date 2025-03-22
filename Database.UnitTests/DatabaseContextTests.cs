using Microsoft.EntityFrameworkCore;
using GhostfolioSidekick.Database;
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

		[Fact]
		public async Task CanPerformCRUDOperations()
		{
			// Arrange
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite("Data Source=:memory:")
				.Options;

			using (var context = new DatabaseContext(options))
			{
				context.Database.OpenConnection();
				await context.Database.MigrateAsync().ConfigureAwait(false);

				// Act & Assert
				// Create
				var platform = new Platform { Name = "Test Platform" };
				context.Platforms.Add(platform);
				await context.SaveChangesAsync();

				// Read
				var retrievedPlatform = await context.Platforms.FirstOrDefaultAsync(p => p.Name == "Test Platform");
				retrievedPlatform.Should().NotBeNull();

				// Update
				retrievedPlatform.Name = "Updated Platform";
				await context.SaveChangesAsync();

				var updatedPlatform = await context.Platforms.FirstOrDefaultAsync(p => p.Name == "Updated Platform");
				updatedPlatform.Should().NotBeNull();

				// Delete
				context.Platforms.Remove(updatedPlatform);
				await context.SaveChangesAsync();

				var deletedPlatform = await context.Platforms.FirstOrDefaultAsync(p => p.Name == "Updated Platform");
				deletedPlatform.Should().BeNull();
			}
		}

		[Fact]
		public async Task CanHandleEdgeCases()
		{
			// Arrange
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite("Data Source=:memory:")
				.Options;

			using (var context = new DatabaseContext(options))
			{
				context.Database.OpenConnection();
				await context.Database.MigrateAsync().ConfigureAwait(false);

				// Act & Assert
				// Attempt to retrieve a non-existent entity
				var nonExistentPlatform = await context.Platforms.FirstOrDefaultAsync(p => p.Name == "Non Existent Platform");
				nonExistentPlatform.Should().BeNull();

				// Attempt to delete a non-existent entity
				var platform = new Platform { Name = "Test Platform" };
				context.Platforms.Remove(platform);
				await context.SaveChangesAsync();
			}
		}
	}
}
