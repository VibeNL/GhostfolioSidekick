using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.Services.Implementation;

namespace GhostfolioSidekick.PortfolioViewer.Services.UnitTests
{
    public class PerformanceAnalyticsServiceTests
    {
        private readonly Mock<ILogger<PerformanceAnalyticsService>> _loggerMock;
        private readonly DbContextOptions<DatabaseContext> _dbContextOptions;

        public PerformanceAnalyticsServiceTests()
        {
            _loggerMock = new Mock<ILogger<PerformanceAnalyticsService>>();
            _dbContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task GetMonthlyActivityDataAsync_ShouldReturnEmptyList_WhenNoActivities()
        {
            // Arrange
            using var context = new DatabaseContext(_dbContextOptions);
            var service = new PerformanceAnalyticsService(context, _loggerMock.Object);

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
            using var context = new DatabaseContext(_dbContextOptions);
            var service = new PerformanceAnalyticsService(context, _loggerMock.Object);

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
            using var context = new DatabaseContext(_dbContextOptions);
            var service = new PerformanceAnalyticsService(context, _loggerMock.Object);

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
            using var context = new DatabaseContext(_dbContextOptions);
            var service = new PerformanceAnalyticsService(context, _loggerMock.Object);

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
            using var context = new DatabaseContext(_dbContextOptions);
            var service = new PerformanceAnalyticsService(context, _loggerMock.Object);

            // Act
            var result = await service.GetActivityTimelineAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}