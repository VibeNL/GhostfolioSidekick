using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Strategies;
using GhostfolioSidekick.Model.Symbols;
using Moq;

namespace GhostfolioSidekick.UnitTests.Model.Strategies
{
	public class RoundStrategyTests
	{
		private readonly RoundStrategy _roundStrategy;

		public RoundStrategyTests()
		{
			_roundStrategy = new RoundStrategy();
		}

		[Fact]
		public async Task Execute_ShouldRoundQuantityAndUnitPrice()
		{
			// Arrange
			var mockActivity = new Mock<IActivityWithQuantityAndUnitPrice>();
			mockActivity.SetupProperty(a => a.Quantity, 1.12345678901234567890m);
			mockActivity.SetupProperty(a => a.UnitPrice, new Money(Currency.USD, 1.12345678901234567890m));

			var holding = new Holding(new Fixture().Create<SymbolProfile>())
			{
				Activities = new List<IActivity> { mockActivity.Object }
			};

			// Act
			await _roundStrategy.Execute(holding);

			// Assert
			mockActivity.Object.Quantity.Should().Be(1.123457m);
			mockActivity.Object.UnitPrice!.Amount.Should().Be(1.123457m);
		}

		[Fact]
		public async Task Execute_ShouldNotRoundWhenUnitPriceIsNull()
		{
			// Arrange
			var mockActivity = new Mock<IActivityWithQuantityAndUnitPrice>();
			mockActivity.SetupProperty(a => a.Quantity, 1.12345678901234567890m);
			mockActivity.SetupProperty(a => a.UnitPrice, null);

			var holding = new Holding(new Fixture().Create<SymbolProfile>())
			{
				Activities = new List<IActivity> { mockActivity.Object }
			};

			// Act
			await _roundStrategy.Execute(holding);

			// Assert
			// No exception should be thrown
		}


		[Fact]
		public async Task Execute_ShouldNotRoundWhenActivityIsNull()
		{
			// Arrange
			var holding = new Holding(new Fixture().Create<SymbolProfile>())
			{
				Activities = new List<IActivity> { null }
			};

			// Act
			await _roundStrategy.Execute(holding);

			// Assert
			// No exception should be thrown
		}

		[Fact]
		public async Task Execute_ShouldNotRoundWhenHoldingIsNull()
		{
			// Arrange
			Holding holding = null;

			// Act
			Func<Task> act = async () => await _roundStrategy.Execute(holding);

			// Assert
			await act.Should().ThrowAsync<ArgumentNullException>();
		}

		[Fact]
		public async Task Execute_ShouldNotRoundWhenActivitiesAreEmpty()
		{
			// Arrange
			var holding = new Holding(new Fixture().Create<SymbolProfile>())
			{
				Activities = new List<IActivity>()
			};

			// Act
			await _roundStrategy.Execute(holding);

			// Assert
			// No exception should be thrown
		}

	}
}
