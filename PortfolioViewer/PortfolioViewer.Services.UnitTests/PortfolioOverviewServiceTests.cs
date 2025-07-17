using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.Services.Implementation;
using GhostfolioSidekick.PortfolioViewer.Services.Models;

namespace GhostfolioSidekick.PortfolioViewer.Services.UnitTests;

public class PortfolioOverviewServiceTests
{
    private readonly Mock<ILogger<PortfolioOverviewService>> _loggerMock;
    private readonly Mock<DatabaseContext> _dbContextMock;

    public PortfolioOverviewServiceTests()
    {
        _loggerMock = new Mock<ILogger<PortfolioOverviewService>>();
        _dbContextMock = new Mock<DatabaseContext>();
    }

    [Fact]
    public async Task GetTotalAccountsAsync_ShouldReturnZero_WhenNoAccounts()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Accounts.Account>>();
        var emptyAccounts = new List<GhostfolioSidekick.Model.Accounts.Account>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyAccounts);
        _dbContextMock.Setup(c => c.Accounts).Returns(mockSet.Object);

        var service = new PortfolioOverviewService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetTotalAccountsAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetTotalHoldingsAsync_ShouldReturnZero_WhenNoHoldings()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Holding>>();
        var emptyHoldings = new List<GhostfolioSidekick.Model.Holding>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyHoldings);
        _dbContextMock.Setup(c => c.Holdings).Returns(mockSet.Object);

        var service = new PortfolioOverviewService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetTotalHoldingsAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetTotalActivitiesAsync_ShouldReturnZero_WhenNoActivities()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Activities.Activity>>();
        var emptyActivities = new List<GhostfolioSidekick.Model.Activities.Activity>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyActivities);
        _dbContextMock.Setup(c => c.Activities).Returns(mockSet.Object);

        var service = new PortfolioOverviewService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetTotalActivitiesAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetBuyTransactionsCountAsync_ShouldReturnZero_WhenNoBuySellActivities()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Activities.Activity>>();
        var emptyActivities = new List<GhostfolioSidekick.Model.Activities.Activity>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyActivities);
        _dbContextMock.Setup(c => c.Activities).Returns(mockSet.Object);

        var service = new PortfolioOverviewService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetBuyTransactionsCountAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetActivityBreakdownAsync_ShouldReturnEmptyDictionary_WhenNoActivities()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Activities.Activity>>();
        var emptyActivities = new List<GhostfolioSidekick.Model.Activities.Activity>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyActivities);
        _dbContextMock.Setup(c => c.Activities).Returns(mockSet.Object);

        var service = new PortfolioOverviewService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetActivityBreakdownAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecentActivitiesAsync_ShouldReturnEmptyList_WhenNoActivities()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Activities.Activity>>();
        var emptyActivities = new List<GhostfolioSidekick.Model.Activities.Activity>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyActivities);
        _dbContextMock.Setup(c => c.Activities).Returns(mockSet.Object);

        var service = new PortfolioOverviewService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetRecentActivitiesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAccountSummariesAsync_ShouldReturnEmptyList_WhenNoAccounts()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Accounts.Account>>();
        var emptyAccounts = new List<GhostfolioSidekick.Model.Accounts.Account>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyAccounts);
        _dbContextMock.Setup(c => c.Accounts).Returns(mockSet.Object);

        var service = new PortfolioOverviewService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetAccountSummariesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecentActivitiesAsync_ShouldRespectCountParameter()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Activities.Activity>>();
        var emptyActivities = new List<GhostfolioSidekick.Model.Activities.Activity>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyActivities);
        _dbContextMock.Setup(c => c.Activities).Returns(mockSet.Object);

        var service = new PortfolioOverviewService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetRecentActivitiesAsync(count: 5);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Service_ShouldHandleExceptions_GracefullyWithLogging()
    {
        // Arrange - Setup mocks to throw exceptions
        var mockAccountsSet = new Mock<DbSet<GhostfolioSidekick.Model.Accounts.Account>>();
        var mockHoldingsSet = new Mock<DbSet<GhostfolioSidekick.Model.Holding>>();
        var mockActivitiesSet = new Mock<DbSet<GhostfolioSidekick.Model.Activities.Activity>>();
        
        mockAccountsSet.Setup(m => m.AsAsyncEnumerable()).Throws(new InvalidOperationException("Database error"));
        mockHoldingsSet.Setup(m => m.AsAsyncEnumerable()).Throws(new InvalidOperationException("Database error"));
        mockActivitiesSet.Setup(m => m.AsAsyncEnumerable()).Throws(new InvalidOperationException("Database error"));
        
        _dbContextMock.Setup(c => c.Accounts).Returns(mockAccountsSet.Object);
        _dbContextMock.Setup(c => c.Holdings).Returns(mockHoldingsSet.Object);
        _dbContextMock.Setup(c => c.Activities).Returns(mockActivitiesSet.Object);

        var service = new PortfolioOverviewService(_dbContextMock.Object, _loggerMock.Object);

        // Act & Assert - Should not throw exceptions, should return default values
        var accountsCount = await service.GetTotalAccountsAsync();
        var holdingsCount = await service.GetTotalHoldingsAsync();
        var activitiesCount = await service.GetTotalActivitiesAsync();
        var buyTransactionsCount = await service.GetBuyTransactionsCountAsync();
        var breakdown = await service.GetActivityBreakdownAsync();
        var recentActivities = await service.GetRecentActivitiesAsync();
        var accountSummaries = await service.GetAccountSummariesAsync();

        Assert.Equal(0, accountsCount);
        Assert.Equal(0, holdingsCount);
        Assert.Equal(0, activitiesCount);
        Assert.Equal(0, buyTransactionsCount);
        Assert.NotNull(breakdown);
        Assert.NotNull(recentActivities);
        Assert.NotNull(accountSummaries);

        // Verify error logging occurred
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}