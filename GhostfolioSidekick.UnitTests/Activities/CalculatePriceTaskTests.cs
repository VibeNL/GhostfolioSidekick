using GhostfolioSidekick.Activities;
using GhostfolioSidekick.Activities.Strategies;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using Moq.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.UnitTests.Activities
{
	public class CalculatePriceTaskTests
    {
        private readonly Mock<IDbContextFactory<DatabaseContext>> _mockDbContextFactory;
        private readonly List<IHoldingStrategy> _holdingStrategies;
        private readonly CalculatePriceTask _calculatePriceTask;
        private readonly Mock<ILogger<CalculatePriceTask>> _mockLogger;

        public CalculatePriceTaskTests()
        {
            _mockDbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            _holdingStrategies = new List<IHoldingStrategy>
            {
                new Mock<IHoldingStrategy>().Object,
                new Mock<IHoldingStrategy>().Object
            };
            _mockLogger = new Mock<ILogger<CalculatePriceTask>>();
            _calculatePriceTask = new CalculatePriceTask(_holdingStrategies, _mockDbContextFactory.Object, _mockLogger.Object);
        }

        [Fact]
        public void Priority_ShouldReturnCalculatePrice()
        {
            // Act
            var priority = _calculatePriceTask.Priority;

            // Assert
            priority.Should().Be(TaskPriority.CalculatePrice);
        }

        [Fact]
        public async Task DoWork_ShouldExecuteHoldingStrategies()
        {
            // Arrange
            var mockDbContext = new Mock<DatabaseContext>();
            var holdings = new List<Holding>
            {
                new Holding { Id = 1 },
                new Holding { Id = 2 }
            };

            mockDbContext.Setup(db => db.Holdings).ReturnsDbSet(holdings);
            _mockDbContextFactory.Setup(factory => factory.CreateDbContext()).Returns(mockDbContext.Object);

            var mockStrategy1 = new Mock<IHoldingStrategy>();
            var mockStrategy2 = new Mock<IHoldingStrategy>();
            _holdingStrategies.Clear();
            _holdingStrategies.Add(mockStrategy1.Object);
            _holdingStrategies.Add(mockStrategy2.Object);

            // Act
            await _calculatePriceTask.DoWork();

            // Assert
            mockStrategy1.Verify(strategy => strategy.Execute(It.IsAny<Holding>()), Times.Exactly(holdings.Count));
            mockStrategy2.Verify(strategy => strategy.Execute(It.IsAny<Holding>()), Times.Exactly(holdings.Count));
            mockDbContext.Verify(db => db.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DoWork_ShouldNotExecuteHoldingStrategies_WhenNoHoldings()
        {
            // Arrange
            var mockDbContext = new Mock<DatabaseContext>();
            mockDbContext.Setup(db => db.Holdings).ReturnsDbSet(new List<Holding>());
            _mockDbContextFactory.Setup(factory => factory.CreateDbContext()).Returns(mockDbContext.Object);

            var mockStrategy = new Mock<IHoldingStrategy>();
            _holdingStrategies.Clear();
            _holdingStrategies.Add(mockStrategy.Object);

            // Act
            await _calculatePriceTask.DoWork();

            // Assert
            mockStrategy.Verify(strategy => strategy.Execute(It.IsAny<Holding>()), Times.Never);
            mockDbContext.Verify(db => db.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DoWork_ShouldLogError_WhenDbContextFactoryThrowsException()
        {
            // Arrange
            _mockDbContextFactory.Setup(factory => factory.CreateDbContext()).Throws(new Exception("Test exception"));

            // Act
            await _calculatePriceTask.DoWork();

            // Assert
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Test exception")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)));
        }

        [Fact]
        public async Task DoWork_ShouldNotExecuteHoldingStrategies_WhenDbContextIsNull()
        {
            // Arrange
            _mockDbContextFactory.Setup(factory => factory.CreateDbContext()).Returns((DatabaseContext)null);

            var mockStrategy = new Mock<IHoldingStrategy>();
            _holdingStrategies.Clear();
            _holdingStrategies.Add(mockStrategy.Object);

            // Act
            await _calculatePriceTask.DoWork();

            // Assert
            mockStrategy.Verify(strategy => strategy.Execute(It.IsAny<Holding>()), Times.Never);
        }
    }
}
