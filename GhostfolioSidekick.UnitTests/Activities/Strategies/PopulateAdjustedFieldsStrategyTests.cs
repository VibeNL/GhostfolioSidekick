using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using Moq;
using FluentAssertions;
using GhostfolioSidekick.Activities.Strategies;

namespace GhostfolioSidekick.UnitTests.Activities.Strategies
{
	public class PopulateAdjustedFieldsStrategyTests
    {
        private readonly PopulateAdjustedFieldsStrategy _populateAdjustedFieldsStrategy;

        public PopulateAdjustedFieldsStrategyTests()
        {
            _populateAdjustedFieldsStrategy = new PopulateAdjustedFieldsStrategy();
        }

        [Fact]
        public void Priority_ShouldReturnSetInitialValuePriority()
        {
            // Act
            var priority = _populateAdjustedFieldsStrategy.Priority;

            // Assert
            priority.Should().Be((int)StrategiesPriority.SetInitialValue);
        }

        [Fact]
        public async Task Execute_ShouldPopulateAdjustedFields_ForActivitiesWithQuantityAndUnitPrice()
        {
            // Arrange
            var activity = new Mock<ActivityWithQuantityAndUnitPrice>();
            activity.SetupAllProperties();
            activity.Object.Quantity = 10;
            activity.Object.UnitPrice = new Money(Currency.USD, 100);
            activity.Object.AdjustedUnitPriceSource = [];

            var holding = new Holding
            {
                Activities = [activity.Object]
            };

            // Act
            await _populateAdjustedFieldsStrategy.Execute(holding);

            // Assert
            activity.Object.AdjustedQuantity.Should().Be(activity.Object.Quantity);
            activity.Object.AdjustedUnitPrice.Should().Be(activity.Object.UnitPrice);
            activity.Object.AdjustedUnitPriceSource.Should().ContainSingle(trace => trace.Reason == "Initial value" && trace.NewQuantity == activity.Object.AdjustedQuantity && trace.NewPrice == activity.Object.AdjustedUnitPrice);
        }

        [Fact]
        public async Task Execute_ShouldPopulateAdjustedFields_ForDifferentActivities()
        {
            // Arrange
            var activity1 = new Mock<ActivityWithQuantityAndUnitPrice>();
            activity1.SetupAllProperties();
            activity1.Object.Quantity = 10;
            activity1.Object.UnitPrice = new Money(Currency.USD, 100);
            activity1.Object.AdjustedUnitPriceSource = [];

            var activity2 = new Mock<ActivityWithQuantityAndUnitPrice>();
            activity2.SetupAllProperties();
            activity2.Object.Quantity = 5;
            activity2.Object.UnitPrice = new Money(Currency.EUR, 50);
            activity2.Object.AdjustedUnitPriceSource = [];

            var holding = new Holding
            {
                Activities = [activity1.Object, activity2.Object]
            };

            // Act
            await _populateAdjustedFieldsStrategy.Execute(holding);

            // Assert
            activity1.Object.AdjustedQuantity.Should().Be(activity1.Object.Quantity);
            activity1.Object.AdjustedUnitPrice.Should().Be(activity1.Object.UnitPrice);
            activity1.Object.AdjustedUnitPriceSource.Should().ContainSingle(trace => trace.Reason == "Initial value" && trace.NewQuantity == activity1.Object.AdjustedQuantity && trace.NewPrice == activity1.Object.AdjustedUnitPrice);

            activity2.Object.AdjustedQuantity.Should().Be(activity2.Object.Quantity);
            activity2.Object.AdjustedUnitPrice.Should().Be(activity2.Object.UnitPrice);
            activity2.Object.AdjustedUnitPriceSource.Should().ContainSingle(trace => trace.Reason == "Initial value" && trace.NewQuantity == activity2.Object.AdjustedQuantity && trace.NewPrice == activity2.Object.AdjustedUnitPrice);
        }

        [Fact]
        public async Task Execute_ShouldPopulateAdjustedFields_ForDifferentHoldingConfigurations()
        {
            // Arrange
            var activity1 = new Mock<ActivityWithQuantityAndUnitPrice>();
            activity1.SetupAllProperties();
            activity1.Object.Quantity = 10;
            activity1.Object.UnitPrice = new Money(Currency.USD, 100);
            activity1.Object.AdjustedUnitPriceSource = [];

            var activity2 = new Mock<ActivityWithQuantityAndUnitPrice>();
            activity2.SetupAllProperties();
            activity2.Object.Quantity = 5;
            activity2.Object.UnitPrice = new Money(Currency.EUR, 50);
            activity2.Object.AdjustedUnitPriceSource = [];

            var holding1 = new Holding
            {
                Activities = [activity1.Object]
            };

            var holding2 = new Holding
            {
                Activities = [activity2.Object]
            };

            // Act
            await _populateAdjustedFieldsStrategy.Execute(holding1);
            await _populateAdjustedFieldsStrategy.Execute(holding2);

            // Assert
            activity1.Object.AdjustedQuantity.Should().Be(activity1.Object.Quantity);
            activity1.Object.AdjustedUnitPrice.Should().Be(activity1.Object.UnitPrice);
            activity1.Object.AdjustedUnitPriceSource.Should().ContainSingle(trace => trace.Reason == "Initial value" && trace.NewQuantity == activity1.Object.AdjustedQuantity && trace.NewPrice == activity1.Object.AdjustedUnitPrice);

            activity2.Object.AdjustedQuantity.Should().Be(activity2.Object.Quantity);
            activity2.Object.AdjustedUnitPrice.Should().Be(activity2.Object.UnitPrice);
            activity2.Object.AdjustedUnitPriceSource.Should().ContainSingle(trace => trace.Reason == "Initial value" && trace.NewQuantity == activity2.Object.AdjustedQuantity && trace.NewPrice == activity2.Object.AdjustedUnitPrice);
        }
    }
}
