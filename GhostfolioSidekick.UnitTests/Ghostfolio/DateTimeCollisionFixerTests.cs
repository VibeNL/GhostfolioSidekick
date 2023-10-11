using FluentAssertions;
using GhostfolioSidekick.Ghostfolio;
using GhostfolioSidekick.Ghostfolio.API.Contract;

namespace GhostfolioSidekick.UnitTests.Ghostfolio
{
	public class DateTimeCollisionFixerTests
	{
		[Fact]
		public async Task NoCollisions()
		{
			// Arrange
			var activities = new[] {
				new Activity{ ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Activity{ ReferenceCode = "2", Date = new DateTime(2023,1,2,1,1,1, DateTimeKind.Utc)},
				new Activity{ ReferenceCode = "3", Date = new DateTime(2023,1,3,1,1,1, DateTimeKind.Utc)}
			};

			// Act
			activities = DateTimeCollisionFixer.Merge(activities).ToArray();

			// Assert
			activities.Should().BeEquivalentTo(new[]
			{
				new Activity{ ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc)},
				new Activity{ ReferenceCode = "2", Date = new DateTime(2023,1,2,1,1,1, DateTimeKind.Utc)},
				new Activity{ ReferenceCode = "3", Date = new DateTime(2023,1,3,1,1,1, DateTimeKind.Utc)}
			});
		}

		[Fact]
		public async Task SingleCollision_Merged()
		{
			// Arrange
			var asset = new Asset { Symbol = "A" };
			var activities = new[] {
				new Activity{ Asset = asset, ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc), Quantity = 1},
				new Activity{ Asset = asset, ReferenceCode = "2", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc), Quantity = 1},
				new Activity{ Asset = asset, ReferenceCode = "3", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc), Quantity = 1}
			};

			// Act
			activities = DateTimeCollisionFixer.Merge(activities).ToArray();

			// Assert
			activities.Should().BeEquivalentTo(new[]
			{
				new Activity{ Asset = asset, ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc), Quantity = 3, Comment = " (01:01 BUY 1@0|01:01 BUY 1@0|01:01 BUY 1@0)"}
			});
		}

		[Fact]
		public async Task SingleCollisions_MultipleAssets_CorrectlyMerged()
		{
			// Arrange
			var asset = new Asset { Symbol = "A" };
			var assetB = new Asset { Symbol = "B" };
			var activities = new[] {
				new Activity{ Asset = asset, ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc), Quantity = 1},
				new Activity{ Asset = asset, ReferenceCode = "2", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc), Quantity = 1},
				new Activity{ Asset = assetB, ReferenceCode = "3", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc), Quantity = 1}
			};

			// Act
			activities = DateTimeCollisionFixer.Merge(activities).ToArray();

			// Assert
			activities.Should().BeEquivalentTo(new[]
			{
				new Activity{ Asset = asset, ReferenceCode = "1", Date = new DateTime(2023,1,1,1,1,1, DateTimeKind.Utc), Quantity = 2, Comment=" (01:01 BUY 1@0|01:01 BUY 1@0)"},
				new Activity{Asset = assetB, ReferenceCode = "3", Date = new DateTime(2023,1,1,1,1,2, DateTimeKind.Utc), Quantity = 1}
			});
		}
	}
}
