using FluentAssertions;
using GhostfolioSidekick.Ghostfolio;
using GhostfolioSidekick.Ghostfolio.API;

namespace GhostfolioSidekick.UnitTests.Ghostfolio
{
	public class DateTimeCollisionFixerTests
	{
		[Fact]
		public async Task NoCollisions()
		{
			// Arrange
			var orders = new[] {
				new Order{ ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Order{ ReferenceCode = "2", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc)},
				new Order{ ReferenceCode = "3", Date = new DateTime(2023,1,1,1,1,3, DateTimeKind.Utc)}
			};

			// Act
			DateTimeCollisionFixer.Fix(orders);

			// Assert
			orders.Should().BeEquivalentTo(new[]
			{
				new Order{ ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Order{ ReferenceCode = "2", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc)},
				new Order{ ReferenceCode = "3", Date = new DateTime(2023,1,1,1,1,3, DateTimeKind.Utc)}
			});
		}

		[Fact]
		public async Task SingleCollisionsWithCascadingEffect()
		{
			// Arrange
			var asset = new Asset { Symbol = "A" };
			var orders = new[] {
				new Order{ Asset = asset, ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Order{ Asset = asset, ReferenceCode = "2", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Order{ Asset = asset, ReferenceCode = "3", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc)}
			};

			// Act
			DateTimeCollisionFixer.Fix(orders);

			// Assert
			orders.Should().BeEquivalentTo(new[]
			{
				new Order{ Asset = asset, ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Order{ Asset = asset, ReferenceCode = "2", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc)},
				new Order{Asset = asset, ReferenceCode = "3", Date = new DateTime(2023,1,1,1,1,3, DateTimeKind.Utc)}
			});
		}

		[Fact]
		public async Task SingleCollisionsWithNoCascadingEffect()
		{
			// Arrange
			var asset = new Asset { Symbol = "A" };
			var assetB = new Asset { Symbol = "B" };
			var orders = new[] {
				new Order{ Asset = asset, ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Order{ Asset = asset, ReferenceCode = "2", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Order{ Asset = assetB, ReferenceCode = "3", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc)}
			};

			// Act
			DateTimeCollisionFixer.Fix(orders);

			// Assert
			orders.Should().BeEquivalentTo(new[]
			{
				new Order{ Asset = asset, ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Order{ Asset = asset, ReferenceCode = "2", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc)},
				new Order{Asset = assetB, ReferenceCode = "3", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc)}
			});
		}
	}
}
