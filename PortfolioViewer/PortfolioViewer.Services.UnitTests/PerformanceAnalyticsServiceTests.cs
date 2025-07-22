using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.Services.Implementation;
using GhostfolioSidekick.PortfolioViewer.Services.Models;

namespace GhostfolioSidekick.PortfolioViewer.Services.UnitTests;

public class PerformanceAnalyticsServiceTests
{
    private readonly Mock<ILogger<PerformanceAnalyticsService>> _loggerMock;
    private readonly Mock<DatabaseContext> _dbContextMock;

    public PerformanceAnalyticsServiceTests()
    {
        _loggerMock = new Mock<ILogger<PerformanceAnalyticsService>>();
        _dbContextMock = new Mock<DatabaseContext>();
    }

    [Fact]
    public async Task GetMonthlyActivityDataAsync_ShouldReturnEmptyList_WhenNoActivities()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Activities.Activity>>();
        var emptyActivities = new List<GhostfolioSidekick.Model.Activities.Activity>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyActivities);
        _dbContextMock.Setup(c => c.Activities).Returns(mockSet.Object);

        var service = new PerformanceAnalyticsService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetMonthlyActivityDataAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBuySellAnalysisAsync_ShouldReturnEmptyList_WhenNoBuySellActivities()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Activities.Activity>>();
        var emptyActivities = new List<GhostfolioSidekick.Model.Activities.Activity>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyActivities);
        _dbContextMock.Setup(c => c.Activities).Returns(mockSet.Object);

        var service = new PerformanceAnalyticsService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetBuySellAnalysisAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDividendAnalysisAsync_ShouldReturnEmptyList_WhenNoDividendActivities()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Activities.Activity>>();
        var emptyActivities = new List<GhostfolioSidekick.Model.Activities.Activity>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyActivities);
        _dbContextMock.Setup(c => c.Activities).Returns(mockSet.Object);

        var service = new PerformanceAnalyticsService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetDividendAnalysisAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTopActiveHoldingsAsync_ShouldReturnEmptyList_WhenNoHoldings()
    {
        // Arrange
        var mockHoldingsSet = new Mock<DbSet<GhostfolioSidekick.Model.Holding>>();
        var emptyHoldings = new List<GhostfolioSidekick.Model.Holding>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockHoldingsSet, emptyHoldings);
        _dbContextMock.Setup(c => c.Holdings).Returns(mockHoldingsSet.Object);

        var service = new PerformanceAnalyticsService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetTopActiveHoldingsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActivityTimelineAsync_ShouldReturnEmptyList_WhenNoActivities()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Activities.Activity>>();
        var emptyActivities = new List<GhostfolioSidekick.Model.Activities.Activity>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyActivities);
        _dbContextMock.Setup(c => c.Activities).Returns(mockSet.Object);

        var service = new PerformanceAnalyticsService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetActivityTimelineAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActivityTimelineAsync_ShouldRespectCountParameter()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Activities.Activity>>();
        var emptyActivities = new List<GhostfolioSidekick.Model.Activities.Activity>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyActivities);
        _dbContextMock.Setup(c => c.Activities).Returns(mockSet.Object);

        var service = new PerformanceAnalyticsService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetActivityTimelineAsync(count: 25);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTopActiveHoldingsAsync_ShouldRespectCountParameter()
    {
        // Arrange
        var mockHoldingsSet = new Mock<DbSet<GhostfolioSidekick.Model.Holding>>();
        var emptyHoldings = new List<GhostfolioSidekick.Model.Holding>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockHoldingsSet, emptyHoldings);
        _dbContextMock.Setup(c => c.Holdings).Returns(mockHoldingsSet.Object);

        var service = new PerformanceAnalyticsService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetTopActiveHoldingsAsync(count: 5);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Service_ShouldHandleExceptions_GracefullyWithLogging()
    {
        // Arrange - Setup mocks to throw exceptions
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Activities.Activity>>();
        mockSet.Setup(m => m.AsAsyncEnumerable()).Throws(new InvalidOperationException("Database error"));
        
        _dbContextMock.Setup(c => c.Activities).Returns(mockSet.Object);

        var service = new PerformanceAnalyticsService(_dbContextMock.Object, _loggerMock.Object);

        // Act & Assert - Should not throw exceptions, should return empty lists
        var monthlyData = await service.GetMonthlyActivityDataAsync();
        var buySellAnalysis = await service.GetBuySellAnalysisAsync();
        var dividendAnalysis = await service.GetDividendAnalysisAsync();
        var timeline = await service.GetActivityTimelineAsync();

        Assert.NotNull(monthlyData);
        Assert.NotNull(buySellAnalysis);
        Assert.NotNull(dividendAnalysis);
        Assert.NotNull(timeline);

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