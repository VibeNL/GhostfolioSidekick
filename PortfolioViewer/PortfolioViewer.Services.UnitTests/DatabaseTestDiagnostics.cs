using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.PortfolioViewer.Services.UnitTests;

public class DatabaseTestDiagnostics
{
    [Fact]
    public async Task CanSaveAndRetrieveAccounts()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new DatabaseContext(dbContextOptions);
        
        // Act
        var account = new Account("Test Account");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        // Assert
        var savedAccounts = await context.Accounts.ToListAsync();
        Assert.Single(savedAccounts);
        Assert.Equal("Test Account", savedAccounts.First().Name);
    }
}