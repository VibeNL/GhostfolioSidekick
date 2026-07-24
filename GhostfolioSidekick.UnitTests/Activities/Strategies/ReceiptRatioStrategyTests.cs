using AwesomeAssertions;
using GhostfolioSidekick.Activities.Strategies;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.UnitTests.Activities.Strategies
{
	public class ReceiptRatioStrategyTests
	{
		private readonly ReceiptRatioStrategy _receiptRatioStrategy;

		public ReceiptRatioStrategyTests()
		{
			_receiptRatioStrategy = new ReceiptRatioStrategy();
		}

		[Fact]
		public void Priority_ShouldReturnReceiptRatioPriority()
		{
			// Act
			var priority = _receiptRatioStrategy.Priority;

			// Assert
			priority.Should().Be((int)StrategiesPriority.ReceiptRatio);
		}

		[Fact]
		public async Task Execute_ShouldDoNothing_WhenNoSymbolsWithRatio()
		{
			// Arrange
			var holding = new Holding
			{
				SymbolProfiles = [new SymbolProfile { SharesPerReceipt = 1 }],
				Activities = []
			};

			// Act
			await _receiptRatioStrategy.Execute(holding);

			// Assert
			holding.Activities.Should().BeEmpty();
		}

		[Fact]
		public async Task Execute_ShouldAdjustFields_ForSymbolWithReceiptRatio()
		{
			// Arrange
			var symbolProfile = new SymbolProfile
			{
				Symbol = "SMSN",
				SharesPerReceipt = 25
			};

			var activity = new Mock<ActivityWithQuantityAndUnitPrice>();
			activity.SetupAllProperties();
			activity.Object.Date = DateTime.Now;
			activity.Object.Quantity = 10;
			activity.Object.UnitPrice = new Money(Currency.USD, 100);
			activity.Object.AdjustedQuantity = 10;
			activity.Object.AdjustedUnitPrice = new Money(Currency.USD, 100);
			activity.Object.AdjustedUnitPriceSource = [];

			var holding = new Holding
			{
				SymbolProfiles = [symbolProfile],
				Activities = [activity.Object]
			};

			// Act
			await _receiptRatioStrategy.Execute(holding);

			// Assert
			var expectedAdjustedUnitPrice = activity.Object.UnitPrice.Times(1 / 25m);
			var expectedAdjustedQuantity = activity.Object.Quantity * 25;

			activity.Object.AdjustedUnitPrice.Should().Be(expectedAdjustedUnitPrice);
			activity.Object.AdjustedQuantity.Should().Be(expectedAdjustedQuantity);
			activity.Object.AdjustedUnitPriceSource.Should().ContainSingle(trace =>
				trace.NewQuantity == expectedAdjustedQuantity && trace.NewPrice == expectedAdjustedUnitPrice);
		}
	}
}
