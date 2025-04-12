using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using Moq.EntityFrameworkCore;
using GhostfolioSidekick.Activities;

namespace GhostfolioSidekick.UnitTests.Activities
{
    public class TrackAverageUnitPriceAndProfitLossTaskTests
    {
        private readonly Mock<IDbContextFactory<DatabaseContext>> _mockDbContextFactory;
        private readonly TrackAverageUnitPriceAndProfitLossTask _trackAverageUnitPriceAndProfitLossTask;

        public TrackAverageUnitPriceAndProfitLossTaskTests()
        {
            _mockDbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            _trackAverageUnitPriceAndProfitLossTask = new TrackAverageUnitPriceAndProfitLossTask(_mockDbContextFactory.Object);
        }

        [Fact]
        public void Priority_ShouldReturnTrackAverageUnitPriceAndProfitLoss()
        {
            // Act
            var priority = _trackAverageUnitPriceAndProfitLossTask.Priority;

            // Assert
            priority.Should().Be(TaskPriority.TrackAverageUnitPriceAndProfitLoss);
        }

        [Fact]
        public async Task DoWork_ShouldCalculateAndStoreAverageUnitPriceAndProfitLoss()
        {
            // Arrange
            var mockDbContext = new Mock<DatabaseContext>();
            var holdings = new List<Holding>
            {
                new Holding
                {
                    Id = 1,
                    Activities = new List<Activity>
                    {
                        new BuySellActivity { Date = DateTime.UtcNow.AddDays(-10), Quantity = 10, UnitPrice = new Money(Currency.USD, 100) },
                        new BuySellActivity { Date = DateTime.UtcNow.AddDays(-5), Quantity = 5, UnitPrice = new Money(Currency.USD, 110) }
                    }
                }
            };

            mockDbContext.Setup(db => db.Holdings).ReturnsDbSet(holdings);
            _mockDbContextFactory.Setup(factory => factory.CreateDbContext()).Returns(mockDbContext.Object);

            // Act
            await _trackAverageUnitPriceAndProfitLossTask.DoWork();

            // Assert
            mockDbContext.Verify(db => db.AverageUnitPriceAndProfitLoss.Add(It.IsAny<AverageUnitPriceAndProfitLoss>()), Times.AtLeastOnce);
            mockDbContext.Verify(db => db.SaveChangesAsync(default), Times.Once);
        }
    }
}
