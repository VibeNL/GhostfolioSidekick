using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Ghostfolio.API.Contract;

namespace GhostfolioSidekick.UnitTests.Ghostfolio.Contract
{
	public class ActivityTests
	{
		[Fact]
		public void MergeTwoBuys_CorrectlyMerged()
		{
			// Arrange
			var asset = new Fixture().Create<Asset>();
			var a = new Activity { Asset = asset, Type = ActivityType.BUY, Quantity = 2, Fee = 1, UnitPrice = 100 };
			var b = new Activity { Asset = asset, Type = ActivityType.BUY, Quantity = 2, Fee = 3, UnitPrice = 10 };

			// Act
			var c = a.Merge(b);

			// Assert
			c.Type.Should().Be(ActivityType.BUY);
			c.Quantity.Should().Be(4);
			c.Fee.Should().Be(4);
			c.UnitPrice.Should().Be(55);
		}

		[Fact]
		public void MergeTwoSells_CorrectlyMerged()
		{
			// Arrange
			var asset = new Fixture().Create<Asset>();
			var a = new Activity { Asset = asset, Type = ActivityType.SELL, Quantity = 2, Fee = 1, UnitPrice = 100 };
			var b = new Activity { Asset = asset, Type = ActivityType.SELL, Quantity = 2, Fee = 3, UnitPrice = 10 };

			// Act
			var c = a.Merge(b);

			// Assert
			c.Type.Should().Be(ActivityType.SELL);
			c.Quantity.Should().Be(4);
			c.Fee.Should().Be(4);
			c.UnitPrice.Should().Be(55);
		}
	}
}
