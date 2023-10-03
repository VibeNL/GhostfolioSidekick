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
				new Activity{ ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Activity{ ReferenceCode = "2", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc)},
				new Activity{ ReferenceCode = "3", Date = new DateTime(2023,1,1,1,1,3, DateTimeKind.Utc)}
			};

			// Act
			DateTimeCollisionFixer.Fix(orders);

			// Assert
			orders.Should().BeEquivalentTo(new[]
			{
				new Activity{ ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Activity{ ReferenceCode = "2", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc)},
				new Activity{ ReferenceCode = "3", Date = new DateTime(2023,1,1,1,1,3, DateTimeKind.Utc)}
			});
		}

		[Fact]
		public async Task SingleCollisionsWithCascadingEffect()
		{
			// Arrange
			var asset = new Asset { Symbol = "A" };
			var orders = new[] {
				new Activity{ Asset = asset, ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Activity{ Asset = asset, ReferenceCode = "2", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Activity{ Asset = asset, ReferenceCode = "3", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc)}
			};

			// Act
			DateTimeCollisionFixer.Fix(orders);

			// Assert
			orders.Should().BeEquivalentTo(new[]
			{
				new Activity{ Asset = asset, ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Activity{ Asset = asset, ReferenceCode = "2", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc)},
				new Activity{Asset = asset, ReferenceCode = "3", Date = new DateTime(2023,1,1,1,1,3, DateTimeKind.Utc)}
			});
		}

		[Fact]
		public async Task SingleCollisionsWithNoCascadingEffect()
		{
			// Arrange
			var asset = new Asset { Symbol = "A" };
			var assetB = new Asset { Symbol = "B" };
			var orders = new[] {
				new Activity{ Asset = asset, ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Activity{ Asset = asset, ReferenceCode = "2", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Activity{ Asset = assetB, ReferenceCode = "3", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc)}
			};

			// Act
			DateTimeCollisionFixer.Fix(orders);

			// Assert
			orders.Should().BeEquivalentTo(new[]
			{
				new Activity{ Asset = asset, ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Activity{ Asset = asset, ReferenceCode = "2", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc)},
				new Activity{Asset = assetB, ReferenceCode = "3", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc)}
			});
		}
	}
}
