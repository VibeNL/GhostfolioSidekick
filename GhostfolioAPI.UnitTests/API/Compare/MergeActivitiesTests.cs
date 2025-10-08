using AutoFixture;
using GhostfolioSidekick.GhostfolioAPI.API.Compare;
using GhostfolioSidekick.GhostfolioAPI.Contract;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API.Compare
{
	public class MergeActivitiesTests
	{
		[Fact]
		public async Task Merge_ShouldReturnNewMergeOrders_WhenNewActivitiesAreProvided()
		{
			// Arrange
			var existingActivities = new List<Activity>();
			var newActivities = new List<Activity>
			{
				new() { Comment = "New Activity", SymbolProfile = GenerateSymbol(), Date = DateTime.Now }
			};

			// Act
			var result = await MergeActivities.Merge(existingActivities, newActivities);

			// Assert
			Assert.Single(result);
			Assert.Equal(Operation.New, result.First().Operation);
		}


		[Fact]
		public async Task Merge_ShouldReturnRemovedMergeOrders_WhenExistingActivitiesAreNotInNewActivities()
		{
			// Arrange
			var existingActivities = new List<Activity>
			{
				new() { Comment = "Existing Activity", SymbolProfile = GenerateSymbol(), Date = DateTime.Now }
			};
			var newActivities = new List<Activity>();

			// Act
			var result = await MergeActivities.Merge(existingActivities, newActivities);

			// Assert
			Assert.Single(result);
			Assert.Equal(Operation.Removed, result.First().Operation);
		}

		[Fact]
		public async Task Merge_ShouldReturnUpdatedMergeOrders_WhenActivitiesAreDifferent()
		{
			// Arrange
			var existingActivities = new List<Activity>
			{
				new() { Comment = "Activity", SymbolProfile = GenerateSymbol(), Date = DateTime.Now, Fee = 10 }
			};
			var newActivities = new List<Activity>
			{
				new() { Comment = "Activity", SymbolProfile = GenerateSymbol(), Date = DateTime.Now, Fee = 20 }
			};

			// Act
			var result = await MergeActivities.Merge(existingActivities, newActivities);

			// Assert
			Assert.Single(result);
			Assert.Equal(Operation.Updated, result.First().Operation);
		}

		private static SymbolProfile GenerateSymbol()
		{
			return new Fixture().Build<SymbolProfile>().With(x => x.Symbol, "AAPL").Create();
		}
	}
}