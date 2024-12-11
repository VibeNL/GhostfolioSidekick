using GhostfolioSidekick.Activities.Strategies;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using Moq;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    }
}
