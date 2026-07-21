using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider.Yahoo;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.ExternalDataProvider.UnitTests.Yahoo;

public class PriceTargetRepositoryTests
{
    [Fact]
    public void Constructor_WithValidContext_ShouldCreateInstance()
    {
        // Arrange & Act
        using var db = CreateDbContext();
        var repository = new PriceTargetRepository(db);

        // Assert
        Assert.NotNull(repository);
        Assert.IsType<IPriceTargetRepository>(repository, exactMatch: false);
    }

    [Fact]
    public void ImplementsIPriceTargetRepository()
    {
        // Arrange & Act
        using var db = CreateDbContext();
        var repository = new PriceTargetRepository(db);

        // Assert
        Assert.IsType<IPriceTargetRepository>(repository, exactMatch: false);
    }

    [Fact]
    public async Task ClearPriceTargetsAsync_CanBeCancelled()
    {
        // Arrange
        using var db = CreateDbContext();
        var repository = new PriceTargetRepository(db);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => repository.ClearPriceTargetsAsync("TEST", cts.Token));
    }

	private static DatabaseContext CreateDbContext()
	{
		var options = new DbContextOptionsBuilder<DatabaseContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		return new DatabaseContext(options);
	}


}
