using AwesomeAssertions;
using GhostfolioSidekick.Activities.Strategies;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.UnitTests.Activities.Strategies
{
	public class CleanTraceTests
	{
		[Fact]
		public async Task Execute_NoActivities_DoesNothing()
		{
			// Arrange
			var holding = new Holding
			{
				Activities = []
			};
			var cleanTrace = new CleanTrace();

			// Act
			await cleanTrace.Execute(holding);

			// Assert
			holding.Activities.Should().BeEmpty();
		}

		[Fact]
		public async Task Execute_WithActivities_CleansAdjustedUnitPriceSource()
		{
			// Arrange
			var activity = new Mock<ActivityWithQuantityAndUnitPrice>();
			var adjustedUnitPriceSource = new List<CalculatedPriceTrace> { new() };
			activity.Setup(a => a.AdjustedUnitPriceSource).Returns(adjustedUnitPriceSource);

			var holding = new Holding
			{
				Activities = [activity.Object]
			};
			var cleanTrace = new CleanTrace();

			// Act
			await cleanTrace.Execute(holding);

			// Assert
			adjustedUnitPriceSource.Should().BeEmpty();
		}
	}
}
