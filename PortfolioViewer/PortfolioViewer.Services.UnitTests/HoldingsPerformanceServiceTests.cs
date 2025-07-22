using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.Services.Implementation;
using GhostfolioSidekick.PortfolioViewer.Services.Models;

namespace GhostfolioSidekick.PortfolioViewer.Services.UnitTests;

public class HoldingsPerformanceServiceTests
{
    private readonly Mock<ILogger<HoldingsPerformanceService>> _loggerMock;
    private readonly Mock<DatabaseContext> _dbContextMock;

    public HoldingsPerformanceServiceTests()
    {
        _loggerMock = new Mock<ILogger<HoldingsPerformanceService>>();
        _dbContextMock = new Mock<DatabaseContext>();
    }

    [Fact]
    public async Task GetHoldingsDataAsync_ShouldReturnEmptyResult_WhenNoHoldings()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Holding>>();
        var emptyHoldings = new List<GhostfolioSidekick.Model.Holding>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyHoldings);
        _dbContextMock.Setup(c => c.Holdings).Returns(mockSet.Object);

        var service = new HoldingsPerformanceService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetHoldingsDataAsync();

        // Assert
        Assert.NotNull(result.holdings);
        Assert.Empty(result.holdings);
        Assert.Equal(0, result.totalCount);
    }

    [Fact]
    public async Task GetAssetClassesAsync_ShouldReturnEmptyList_WhenNoSymbolProfiles()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Symbols.SymbolProfile>>();
        var emptySymbolProfiles = new List<GhostfolioSidekick.Model.Symbols.SymbolProfile>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptySymbolProfiles);
        _dbContextMock.Setup(c => c.SymbolProfiles).Returns(mockSet.Object);

        var service = new HoldingsPerformanceService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetAssetClassesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAssetClassDistributionAsync_ShouldReturnEmptyList_WhenNoHoldings()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Holding>>();
        var emptyHoldings = new List<GhostfolioSidekick.Model.Holding>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyHoldings);
        _dbContextMock.Setup(c => c.Holdings).Returns(mockSet.Object);

        var service = new HoldingsPerformanceService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetAssetClassDistributionAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMostActiveHoldingsAsync_ShouldReturnEmptyList_WhenNoActivities()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Holding>>();
        var emptyHoldings = new List<GhostfolioSidekick.Model.Holding>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyHoldings);
        _dbContextMock.Setup(c => c.Holdings).Returns(mockSet.Object);

        var service = new HoldingsPerformanceService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetMostActiveHoldingsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task FilterHoldingsByAssetClassAsync_ShouldReturnEmptyList_WhenNoHoldings()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Holding>>();
        var emptyHoldings = new List<GhostfolioSidekick.Model.Holding>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyHoldings);
        _dbContextMock.Setup(c => c.Holdings).Returns(mockSet.Object);

        var service = new HoldingsPerformanceService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.FilterHoldingsByAssetClassAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHoldingsDataAsync_ShouldHandlePaginationParameters()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Holding>>();
        var emptyHoldings = new List<GhostfolioSidekick.Model.Holding>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyHoldings);
        _dbContextMock.Setup(c => c.Holdings).Returns(mockSet.Object);

        var service = new HoldingsPerformanceService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetHoldingsDataAsync(pageSize: 50, page: 2);

        // Assert
        Assert.NotNull(result.holdings);
        Assert.Empty(result.holdings);
        Assert.Equal(0, result.totalCount);
    }

    [Fact]
    public async Task GetMostActiveHoldingsAsync_ShouldRespectCountParameter()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Holding>>();
        var emptyHoldings = new List<GhostfolioSidekick.Model.Holding>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyHoldings);
        _dbContextMock.Setup(c => c.Holdings).Returns(mockSet.Object);

        var service = new HoldingsPerformanceService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetMostActiveHoldingsAsync(count: 5);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task FilterHoldingsByAssetClassAsync_ShouldHandleAssetClassFilter()
    {
        // Arrange
        var mockSet = new Mock<DbSet<GhostfolioSidekick.Model.Holding>>();
        var emptyHoldings = new List<GhostfolioSidekick.Model.Holding>().AsQueryable();
        
        TestHelpers.SetupMockDbSet(mockSet, emptyHoldings);
        _dbContextMock.Setup(c => c.Holdings).Returns(mockSet.Object);

        var service = new HoldingsPerformanceService(_dbContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.FilterHoldingsByAssetClassAsync("Equity");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Service_ShouldHandleExceptions_GracefullyWithLogging()
    {
        // Arrange - Setup mocks to throw exceptions
        var mockHoldingsSet = new Mock<DbSet<GhostfolioSidekick.Model.Holding>>();
        var mockSymbolProfilesSet = new Mock<DbSet<GhostfolioSidekick.Model.Symbols.SymbolProfile>>();
        
        mockHoldingsSet.Setup(m => m.AsAsyncEnumerable()).Throws(new InvalidOperationException("Database error"));
        mockSymbolProfilesSet.Setup(m => m.AsAsyncEnumerable()).Throws(new InvalidOperationException("Database error"));
        
        _dbContextMock.Setup(c => c.Holdings).Returns(mockHoldingsSet.Object);
        _dbContextMock.Setup(c => c.SymbolProfiles).Returns(mockSymbolProfilesSet.Object);

        var service = new HoldingsPerformanceService(_dbContextMock.Object, _loggerMock.Object);

        // Act & Assert - Should not throw exceptions
        var holdings = await service.GetHoldingsDataAsync();
        var assetClasses = await service.GetAssetClassesAsync();
        var distribution = await service.GetAssetClassDistributionAsync();
        var activeHoldings = await service.GetMostActiveHoldingsAsync();
        var filtered = await service.FilterHoldingsByAssetClassAsync("Equity");

        Assert.NotNull(holdings.holdings);
        Assert.NotNull(assetClasses);
        Assert.NotNull(distribution);
        Assert.NotNull(activeHoldings);
        Assert.NotNull(filtered);

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