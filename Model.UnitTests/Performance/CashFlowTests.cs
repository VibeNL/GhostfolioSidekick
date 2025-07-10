using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using AwesomeAssertions;

namespace GhostfolioSidekick.Model.UnitTests.Performance
{
    public class CashFlowTests
    {
        [Fact]
        public void Constructor_ShouldSetBasicProperties()
        {
            // Arrange
            var date = new DateOnly(2024, 1, 15);
            var amount = new Money(Currency.USD, 1000m);
            var type = CashFlowType.Deposit;

            // Act
            var cashFlow = new CashFlow(date, amount, type);

            // Assert
            cashFlow.Date.Should().Be(date);
            cashFlow.Amount.Should().Be(amount);
            cashFlow.Type.Should().Be(type);
        }

        [Fact]
        public void IsInflow_WithPositiveAmount_ShouldReturnTrue()
        {
            // Arrange
            var cashFlow = new CashFlow(
                new DateOnly(2024, 1, 15),
                new Money(Currency.USD, 1000m),
                CashFlowType.Deposit);

            // Act & Assert
            cashFlow.IsInflow.Should().BeTrue();
            cashFlow.IsOutflow.Should().BeFalse();
        }

        [Fact]
        public void IsOutflow_WithNegativeAmount_ShouldReturnTrue()
        {
            // Arrange
            var cashFlow = new CashFlow(
                new DateOnly(2024, 1, 15),
                new Money(Currency.USD, -500m),
                CashFlowType.Withdrawal);

            // Act & Assert
            cashFlow.IsOutflow.Should().BeTrue();
            cashFlow.IsInflow.Should().BeFalse();
        }

        [Fact]
        public void AbsoluteAmount_ShouldReturnAbsoluteValue()
        {
            // Arrange
            var cashFlow = new CashFlow(
                new DateOnly(2024, 1, 15),
                new Money(Currency.USD, -500m),
                CashFlowType.Withdrawal);

            // Act
            var absoluteAmount = cashFlow.AbsoluteAmount;

            // Assert
            absoluteAmount.Amount.Should().Be(500m);
            absoluteAmount.Currency.Should().Be(Currency.USD);
        }

        [Theory]
        [InlineData(CashFlowType.Deposit)]
        [InlineData(CashFlowType.Dividend)]
        [InlineData(CashFlowType.Interest)]
        [InlineData(CashFlowType.Purchase)]
        [InlineData(CashFlowType.Sale)]
        public void CashFlowType_AllTypes_ShouldBeSupported(CashFlowType type)
        {
            // Arrange & Act
            var cashFlow = new CashFlow(
                new DateOnly(2024, 1, 15),
                new Money(Currency.USD, 100m),
                type);

            // Assert
            cashFlow.Type.Should().Be(type);
        }

        [Fact]
        public void Constructor_WithAllProperties_ShouldSetAllValues()
        {
            // Arrange
            var date = new DateOnly(2024, 1, 15);
            var amount = new Money(Currency.EUR, 750m);
            var type = CashFlowType.Dividend;
            var description = "AAPL Dividend Payment";
            var activityId = 12345L;

            // Act
            var cashFlow = new CashFlow(date, amount, type)
            {
                Description = description,
                ActivityId = activityId
            };

            // Assert
            cashFlow.Date.Should().Be(date);
            cashFlow.Amount.Should().Be(amount);
            cashFlow.Type.Should().Be(type);
            cashFlow.Description.Should().Be(description);
            cashFlow.ActivityId.Should().Be(activityId);
        }
    }
}