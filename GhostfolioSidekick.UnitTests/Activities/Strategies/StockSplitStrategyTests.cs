using GhostfolioSidekick.Activities.Strategies;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using Moq;
using Shouldly;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Model.Market;

namespace GhostfolioSidekick.UnitTests.Activities.Strategies
{
	public class StockSplitStrategyTests
    {
        private readonly StockSplitStrategy _stockSplitStrategy;

        public StockSplitStrategyTests()
        {
            _stockSplitStrategy = new StockSplitStrategy();
        }

        [Fact]
        public void Priority_ShouldReturnStockSplitPriority()
        {
            // Act
            var priority = _stockSplitStrategy.Priority;

            // Assert
            priority.ShouldBe((int)StrategiesPriority.StockSplit);
        }

        [Fact]
        public async Task Execute_ShouldDoNothing_WhenNoStockSplits()
        {
            // Arrange
            var holding = new Holding
            {
                SymbolProfiles = new List<SymbolProfile>(),
                Activities = []
            };

            // Act
            await _stockSplitStrategy.Execute(holding);

            // Assert
            holding.Activities.ShouldBeEmpty();
        }

        [Fact]
        public async Task Execute_ShouldAdjustFields_ForActivitiesBeforeStockSplit()
        {
            // Arrange
            var splitDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
            var stockSplit = new StockSplit(splitDate, 2, 1);

            var symbolProfile = new SymbolProfile
            {
                StockSplits = new List<StockSplit> { stockSplit }
            };

            var activityDate = DateTime.Now;
            var activity = new Mock<ActivityWithQuantityAndUnitPrice>();
            activity.SetupAllProperties();
            activity.Object.Date = activityDate;
            activity.Object.Quantity = 10;
            activity.Object.UnitPrice = new Money(Currency.USD, 100);
            activity.Object.AdjustedQuantity = 10;
            activity.Object.AdjustedUnitPrice = new Money(Currency.USD, 100);
            activity.Object.AdjustedUnitPriceSource = [];

            var holding = new Holding
            {
                SymbolProfiles = new List<SymbolProfile> { symbolProfile },
                Activities = [activity.Object]
            };

            // Act
            await _stockSplitStrategy.Execute(holding);

            // Assert
            var expectedAdjustedUnitPrice = activity.Object.UnitPrice.Times(2);
            var expectedAdjustedQuantity = activity.Object.Quantity / 2;

            activity.Object.AdjustedUnitPrice.ShouldBe(expectedAdjustedUnitPrice);
            activity.Object.AdjustedQuantity.ShouldBe(expectedAdjustedQuantity);
            activity.Object.AdjustedUnitPriceSource.ShouldContainSingle(trace => trace.Reason == stockSplit.ToString() && trace.NewQuantity == expectedAdjustedQuantity && trace.NewPrice == expectedAdjustedUnitPrice);
        }

        [Fact]
        public async Task Execute_ShouldNotAdjustFields_ForActivitiesAfterStockSplit()
        {
            // Arrange
            var splitDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
            var stockSplit = new StockSplit(splitDate, 2,1);

            var symbolProfile = new SymbolProfile
            {
                StockSplits = new List<StockSplit> { stockSplit }
            };

            var activityDate = DateTime.Now;
            var activity = new Mock<ActivityWithQuantityAndUnitPrice>();
            activity.SetupAllProperties();
            activity.Object.Date = activityDate;
            activity.Object.Quantity = 10;
            activity.Object.UnitPrice = new Money(Currency.USD, 100);
            activity.Object.AdjustedQuantity = 10;
            activity.Object.AdjustedUnitPrice = new Money(Currency.USD, 100);
            activity.Object.AdjustedUnitPriceSource = [];

            var holding = new Holding
            {
                SymbolProfiles = new List<SymbolProfile> { symbolProfile },
                Activities = [activity.Object]
            };

            // Act
            await _stockSplitStrategy.Execute(holding);

            // Assert
            activity.Object.AdjustedUnitPrice.ShouldBe(activity.Object.UnitPrice);
            activity.Object.AdjustedQuantity.ShouldBe(activity.Object.Quantity);
            activity.Object.AdjustedUnitPriceSource.ShouldBeEmpty();
        }
    }
}
