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
		public void MergeTwoSell_CorrectlyMerged()
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

		[Fact]
		public void MergeBuyAndSell_CorrectlyMerged()
		{
			// Arrange
			var asset = new Fixture().Create<Asset>();
			var a = new Activity { Asset = asset, Type = ActivityType.BUY, Quantity = 3, Fee = 1, UnitPrice = 50 };
			var b = new Activity { Asset = asset, Type = ActivityType.SELL, Quantity = 1, Fee = 1, UnitPrice = 100 };

			// Act
			var c = a.Merge(b);

			// Assert
			c.Type.Should().Be(ActivityType.BUY);
			c.Quantity.Should().Be(2);
			c.Fee.Should().Be(2);
			c.UnitPrice.Should().Be(62.5M);
		}

		[Fact]
		public void MergeBuyAndSell_EqualAmount_SetToIgnore()
		{
			// Arrange
			var asset = new Fixture().Create<Asset>();
			var a = new Activity { Asset = asset, Type = ActivityType.BUY, Quantity = 3, Fee = 1, UnitPrice = 50 };
			var b = new Activity { Asset = asset, Type = ActivityType.SELL, Quantity = 3, Fee = 1, UnitPrice = 100 };

			// Act
			var c = a.Merge(b);

			// Assert
			c.Type.Should().Be(ActivityType.IGNORE);
		}
	}
}
