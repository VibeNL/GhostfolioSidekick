using AwesomeAssertions;
using GhostfolioSidekick.Activities.Strategies;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

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
		var marketData = new MarketData { Date = marketDataDate, Close = 100m, Currency = Currency.USD };

		var symbolProfile = new SymbolProfile
		{
			MarketData = [marketData],
			Currency = Currency.USD
		};

		var activity = new ReceiveActivity
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
		activity.AdjustedUnitPrice.Should().Be(new Money(Currency.USD, 100m));
			activity.AdjustedUnitPriceSource.Should().ContainSingle(trace => trace.Reason == "Determine price" && trace.NewQuantity == activity.AdjustedQuantity && trace.NewPrice == activity.AdjustedUnitPrice);
		}

		[Fact]
		public async Task Execute_ShouldNotSetUnitPrice_ForIrrelevantActivities()
		{
			// Arrange
			var activityDate = DateTime.Now;
			var marketDataDate = DateOnly.FromDateTime(activityDate);
		var marketData = new MarketData { Date = marketDataDate, Close = 100m, Currency = Currency.USD };

		var symbolProfile = new SymbolProfile
		{
			MarketData = [marketData],
			Currency = Currency.USD
		};

		var activity = new BuyActivity
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
		activity.AdjustedUnitPrice.Should().Be(new Money(Currency.USD, 0m));
			activity.AdjustedUnitPriceSource.Should().BeEmpty();
		}
	}
}
