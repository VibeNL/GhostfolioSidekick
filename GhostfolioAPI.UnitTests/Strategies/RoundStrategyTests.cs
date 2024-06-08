using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.GhostfolioAPI.Strategies;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.Strategies
{
	public class RoundStrategyTests
	{
		private readonly RoundStrategy _roundStrategy;

		public RoundStrategyTests()
		{
			_roundStrategy = new RoundStrategy(new Mock<ILogger<RoundStrategy>>().Object);
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
				Activities = [mockActivity.Object]
			};

			// Act
			await _roundStrategy.Execute(holding);

			// Assert
			mockActivity.Object.Quantity.Should().Be(1.12345m);
			mockActivity.Object.UnitPrice!.Amount.Should().Be(1.12345m);
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
				Activities = [mockActivity.Object]
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
				Activities = [null!]
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
			Holding holding = null!;

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
				Activities = []
			};

			// Act
			await _roundStrategy.Execute(holding);

			// Assert
			// No exception should be thrown
		}

		[Fact]
		public async Task Execute_ShouldNotRoundMissingQuantityToZero()
		{
			// Arrange
			var mockActivity1 = new Mock<IActivityWithQuantityAndUnitPrice>();
			mockActivity1.SetupProperty(a => a.Quantity, 1.12345678901234567890m);
			mockActivity1.SetupProperty(a => a.UnitPrice, new Money(Currency.USD, 1.12345678901234567890m));

			var mockActivity2 = new Mock<IActivityWithQuantityAndUnitPrice>();
			mockActivity2.SetupProperty(a => a.Quantity, 1.12345678901234567890m);
			mockActivity2.SetupProperty(a => a.UnitPrice, new Money(Currency.USD, 1.12345678901234567890m));

			var holding = new Holding(new Fixture().Create<SymbolProfile>())
			{
				Activities = [mockActivity1.Object, mockActivity2.Object]
			};

			// Act
			await _roundStrategy.Execute(holding);

			// Assert
			var lastActivity = holding.Activities.OfType<IActivityWithQuantityAndUnitPrice>().LastOrDefault();
			lastActivity.Should().NotBeNull();
			lastActivity!.Quantity.Should().NotBe(1.12345m); // The quantity should have been adjusted by the missing quantity
		}

	}
}
