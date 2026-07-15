using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider.Yahoo;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.ExternalDataProvider.UnitTests.Yahoo;

public class PriceTargetRepositoryTests
{
    private DatabaseContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new DatabaseContext(options);
    }

    [Fact]
    public void Constructor_WithValidContext_ShouldCreateInstance()
    {
        // Arrange & Act
        using var db = CreateDbContext();
        var repository = new PriceTargetRepository(db);

        // Assert
        Assert.NotNull(repository);
        Assert.IsAssignableFrom<IPriceTargetRepository>(repository);
    }

    [Fact]
    public void ImplementsIPriceTargetRepository()
    {
        // Arrange & Act
        using var db = CreateDbContext();
        var repository = new PriceTargetRepository(db);

        // Assert
        Assert.IsAssignableFrom<IPriceTargetRepository>(repository);
    }
}
