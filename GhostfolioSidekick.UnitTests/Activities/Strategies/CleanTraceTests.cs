using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.ProcessingService.Activities.Strategies;
using Moq;

namespace GhostfolioSidekick.ProcessingService.UnitTests.Activities.Strategies
{
    public class CleanTraceTests
    {
        [Fact]
        public async Task Execute_NoActivities_DoesNothing()
        {
            // Arrange
            var holding = new Holding
            {
                Activities = new List<Activity>()
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
            var adjustedUnitPriceSource = new List<CalculatedPriceTrace> { new CalculatedPriceTrace() };
            activity.Setup(a => a.AdjustedUnitPriceSource).Returns(adjustedUnitPriceSource);

            var holding = new Holding
            {
                Activities = new List<Activity> { activity.Object }
            };
            var cleanTrace = new CleanTrace();

            // Act
            await cleanTrace.Execute(holding);

            // Assert
            adjustedUnitPriceSource.Should().BeEmpty();
        }
    }
}
