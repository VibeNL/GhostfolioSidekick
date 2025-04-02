using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;
using FluentAssertions;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.ProcessingService.Activities.Strategies;

namespace GhostfolioSidekick.ProcessingService.UnitTests.Activities.Strategies
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
            priority.Should().Be((int)StrategiesPriority.DeterminePrice);
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
            holding.Activities.Should().BeEmpty();
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
            holding.Activities.Should().BeEmpty();
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
            activity.AdjustedUnitPrice.Should().Be(marketData.Close);
            activity.AdjustedUnitPriceSource.Should().ContainSingle(trace => trace.Reason == "Determine price" && trace.NewQuantity == activity.AdjustedQuantity && trace.NewPrice == activity.AdjustedUnitPrice);
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
            activity.AdjustedUnitPrice.Amount.Should().Be(0);
            activity.AdjustedUnitPriceSource.Should().BeEmpty();
        }
    }
}
