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

	private static DatabaseContext CreateDbContext()
	{
		var options = new DbContextOptionsBuilder<DatabaseContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		return new DatabaseContext(options);
	}


}
