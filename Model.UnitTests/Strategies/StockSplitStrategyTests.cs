using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Strategies;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.UnitTests.Model.Strategies
{
	public class StockSplitStrategyTests
	{
		private readonly StockSplitStrategy _stockSplitStrategy;

		public StockSplitStrategyTests()
		{
			_stockSplitStrategy = new StockSplitStrategy();
		}

		[Fact]
		public async Task Execute_NoStockSplitActivity_ReturnsCompletedTask()
		{
			// Arrange
			var holding = new Holding(new Fixture().Create<SymbolProfile>())
			{
				Activities = [new Fixture().Create<BuySellActivity>()]
			};

			// Act
			await _stockSplitStrategy.Execute(holding);

			// Assert
		}

		[Fact]
		public async Task Execute_WithStockSplitActivity_UpdatesUnitPrice()
		{
			// Arrange
			var holding = new Holding(new Fixture().Create<SymbolProfile>())
			{
				Activities =
				[
					new StockSplitActivity(null, DateTime.Now.AddDays(1), 1, 2, string.Empty),
					new BuySellActivity(null, DateTime.Now, 100, new Money(Currency.EUR, 50), string.Empty)
				]
			};

			// Act
			await _stockSplitStrategy.Execute(holding);

			// Assert
			var buySellActivity = holding.Activities.OfType<BuySellActivity>().First();
			buySellActivity.UnitPrice?.Amount.Should().Be(25);
			buySellActivity.Quantity.Should().Be(200);
		}

		[Fact]
		public async Task Execute_WithStockAfterSplitActivity_NoUpdatesUnitPrice()
		{
			// Arrange
			var holding = new Holding(new Fixture().Create<SymbolProfile>())
			{
				Activities =
				[
					new StockSplitActivity(null, DateTime.Now.AddDays(-1), 1, 2, string.Empty),
					new BuySellActivity(null, DateTime.Now, 100, new Money(Currency.EUR, 50), string.Empty)
				]
			};

			// Act
			await _stockSplitStrategy.Execute(holding);

			// Assert
			var buySellActivity = holding.Activities.OfType<BuySellActivity>().First();
			buySellActivity.UnitPrice?.Amount.Should().Be(50);
			buySellActivity.Quantity.Should().Be(100);
		}

		[Fact]
		public async Task Execute_WithoutPrice_NoUpdatesUnitPrice()
		{
			// Arrange
			var holding = new Holding(new Fixture().Create<SymbolProfile>())
			{
				Activities =
				[
					new StockSplitActivity(null, DateTime.Now.AddDays(1), 1, 2, string.Empty),
					new BuySellActivity(null, DateTime.Now, 100, null, string.Empty)
				]
			};

			// Act
			await _stockSplitStrategy.Execute(holding);

			// Assert
			var buySellActivity = holding.Activities.OfType<BuySellActivity>().First();
			buySellActivity.UnitPrice.Should().BeNull();
			buySellActivity.Quantity.Should().Be(200);
		}
	}
}
