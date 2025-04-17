using FluentAssertions;
using GhostfolioSidekick.Activities.Strategies;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using Moq;

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

        [Fact]
        public async Task Execute_WithDifferentActivities_CleansAdjustedUnitPriceSource()
        {
            // Arrange
            var activity1 = new Mock<ActivityWithQuantityAndUnitPrice>();
            var adjustedUnitPriceSource1 = new List<CalculatedPriceTrace> { new CalculatedPriceTrace() };
            activity1.Setup(a => a.AdjustedUnitPriceSource).Returns(adjustedUnitPriceSource1);

            var activity2 = new Mock<ActivityWithQuantityAndUnitPrice>();
            var adjustedUnitPriceSource2 = new List<CalculatedPriceTrace> { new CalculatedPriceTrace() };
            activity2.Setup(a => a.AdjustedUnitPriceSource).Returns(adjustedUnitPriceSource2);

            var holding = new Holding
            {
                Activities = new List<Activity> { activity1.Object, activity2.Object }
            };
            var cleanTrace = new CleanTrace();

            // Act
            await cleanTrace.Execute(holding);

            // Assert
            adjustedUnitPriceSource1.Should().BeEmpty();
            adjustedUnitPriceSource2.Should().BeEmpty();
        }

        [Fact]
        public async Task Execute_WithDifferentHoldingConfigurations_CleansAdjustedUnitPriceSource()
        {
            // Arrange
            var activity1 = new Mock<ActivityWithQuantityAndUnitPrice>();
            var adjustedUnitPriceSource1 = new List<CalculatedPriceTrace> { new CalculatedPriceTrace() };
            activity1.Setup(a => a.AdjustedUnitPriceSource).Returns(adjustedUnitPriceSource1);

            var activity2 = new Mock<ActivityWithQuantityAndUnitPrice>();
            var adjustedUnitPriceSource2 = new List<CalculatedPriceTrace> { new CalculatedPriceTrace() };
            activity2.Setup(a => a.AdjustedUnitPriceSource).Returns(adjustedUnitPriceSource2);

            var holding1 = new Holding
            {
                Activities = new List<Activity> { activity1.Object }
            };

            var holding2 = new Holding
            {
                Activities = new List<Activity> { activity2.Object }
            };

            var cleanTrace = new CleanTrace();

            // Act
            await cleanTrace.Execute(holding1);
            await cleanTrace.Execute(holding2);

            // Assert
            adjustedUnitPriceSource1.Should().BeEmpty();
            adjustedUnitPriceSource2.Should().BeEmpty();
        }
    }
}
