using FluentAssertions;
using GhostfolioSidekick.Parsers.DeGiro;
using GhostfolioSidekick.Model.Activities;
using AutoFixture;

namespace GhostfolioSidekick.Parsers.UnitTests.DeGiro
{
	public class DeGiroDutchStrategyTests
	{
		[Theory]
		[InlineData("DEGIRO Transactiekosten en/of kosten van derden", PartialActivityType.Fee)]
		[InlineData("Dividendbelasting", PartialActivityType.Tax)]
		[InlineData("Verkoop something", PartialActivityType.Sell)]
		[InlineData("Koop something", PartialActivityType.Buy)]
		[InlineData("Dividend", PartialActivityType.Dividend)]
		[InlineData("flatex terugstorting", PartialActivityType.CashWithdrawal)]
		[InlineData("Deposit something", PartialActivityType.CashDeposit)]
		[InlineData("DEGIRO Verrekening Promotie", PartialActivityType.CashDeposit)]
		public void GetActivityType_ShouldReturnCorrectActivityType(string description, PartialActivityType expectedActivityType)
		{
			// Arrange
			var record = DefaultFixture.Create().Build<DeGiroRecord>().With(x => x.Description, description).Create();
			var strategy = new DeGiroDutchStrategy();

			// Act
			var activityType = strategy.GetActivityType(record);

			// Assert
			activityType.Should().Be(expectedActivityType);
		}

		[Fact]
		public void GetActivityType_ShouldReturnNull_WhenDescriptionIsEmpty()
		{
			// Arrange
			var record = DefaultFixture.Create().Build<DeGiroRecord>().With(x => x.Description, string.Empty).Create();
			var strategy = new DeGiroDutchStrategy();

			// Act
			var activityType = strategy.GetActivityType(record);

			// Assert
			activityType.Should().BeNull();
		}

		[Fact]
		public void GetActivityType_ShouldReturnNull_WhenDescriptionDoesNotMatchAnyActivityType()
		{
			// Arrange
			var record = DefaultFixture.Create().Build<DeGiroRecord>().With(x => x.Description, "Some random description").Create();
			var strategy = new DeGiroDutchStrategy();

			// Act
			var activityType = strategy.GetActivityType(record);

			// Assert
			activityType.Should().BeNull();
		}
	}
}
