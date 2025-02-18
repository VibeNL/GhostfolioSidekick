using GhostfolioSidekick.Activities.Strategies;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;
using Shouldly;
using GhostfolioSidekick.Model.Market;

namespace GhostfolioSidekick.UnitTests.Activities.Strategies
{
	public class DeterminePriceTests
    {
        private readonly DeterminePrice _determinePrice;

        public DeterminePriceTests()
        {
            _determinePrice = new DeterminePrice();
        }

        [Fact]
        public void Priority_ShouldReturnDeterminePricePriority()
        {
            // Act
            var priority = _determinePrice.Priority;

            // Assert
            priority.ShouldBe((int)StrategiesPriority.DeterminePrice);
        }

        [Fact]
        public async Task Execute_ShouldDoNothing_WhenSymbolProfilesAreNotEmpty()
        {
            // Arrange
            var holding = new Holding
            {
                SymbolProfiles = [new SymbolProfile()]
            };

            // Act
            await _determinePrice.Execute(holding);

            // Assert
            holding.Activities.ShouldBeEmpty();
        }

        [Fact]
        public async Task Execute_ShouldDoNothing_WhenActivitiesAreEmpty()
        {
            // Arrange
            var holding = new Holding
            {
                SymbolProfiles = [],
                Activities = []
            };

            // Act
            await _determinePrice.Execute(holding);

            // Assert
            holding.Activities.ShouldBeEmpty();
        }

        [Fact]
        public async Task Execute_ShouldSetUnitPrice_ForRelevantActivities()
        {
            // Arrange
            var activityDate = DateTime.Now;
            var marketDataDate = DateOnly.FromDateTime(activityDate);
            var marketData = new MarketData { Date = marketDataDate, Close = new Money(Currency.USD, 100) };

            var symbolProfile = new SymbolProfile
            {
                MarketData = new List<MarketData> { marketData }
            };

            var activity = new SendAndReceiveActivity
            {
                Date = activityDate,
                AdjustedQuantity = 10
            };

            var holding = new Holding
            {
                SymbolProfiles = [symbolProfile],
                Activities = [activity]
            };

            // Act
            await _determinePrice.Execute(holding);

            // Assert
            activity.AdjustedUnitPrice.ShouldBe(marketData.Close);
            activity.AdjustedUnitPriceSource.ShouldContainSingle(trace => trace.Reason == "Determine price" && trace.NewQuantity == activity.AdjustedQuantity && trace.NewPrice == activity.AdjustedUnitPrice);
        }

        [Fact]
        public async Task Execute_ShouldNotSetUnitPrice_ForIrrelevantActivities()
        {
            // Arrange
            var activityDate = DateTime.Now;
            var marketDataDate = DateOnly.FromDateTime(activityDate);
            var marketData = new MarketData { Date = marketDataDate, Close = new Money(Currency.USD, 100) };

            var symbolProfile = new SymbolProfile
            {
                MarketData = new List<MarketData> { marketData }
            };

            var activity = new BuySellActivity
            {
                Date = activityDate,
                AdjustedQuantity = 10
            };

            var holding = new Holding
            {
                SymbolProfiles = [symbolProfile],
                Activities = [activity]
            };

            // Act
            await _determinePrice.Execute(holding);

            // Assert
            activity.AdjustedUnitPrice.Amount.ShouldBe(0);
            activity.AdjustedUnitPriceSource.ShouldBeEmpty();
        }
    }
}
