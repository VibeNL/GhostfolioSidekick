using FluentAssertions;
using GhostfolioSidekick.Parsers.DeGiro;
using GhostfolioSidekick.Model.Activities;
using AutoFixture;
using GhostfolioSidekick.Parsers.UnitTests;

namespace GhostfolioSidekick.UnitTests.Parsers.DeGiro
{
	public class DeGiroRecordNLTests
	{
		[Theory]
		[InlineData("DEGIRO Transactiekosten en/of kosten van derden", PartialActivityType.Fee)]
		[InlineData("Dividendbelasting", PartialActivityType.Tax)]
		[InlineData("Verkoop something", PartialActivityType.Sell)]
		[InlineData("Koop something",	PartialActivityType.Buy)]
		[InlineData("Dividend", PartialActivityType.Dividend)]
		[InlineData("flatex terugstorting", PartialActivityType.CashWithdrawal)]
		[InlineData("Deposit something", PartialActivityType.CashDeposit)]
		[InlineData("DEGIRO Verrekening Promotie", PartialActivityType.CashDeposit)]
		public void GetActivityType_ShouldReturnCorrectActivityType(string description, PartialActivityType expectedActivityType)
		{
			// Arrange
			var deGiroRecordNL = DefaultFixture.Create().Build<DeGiroRecordNL>().With(x => x.Description, description).Create();

			// Act
			var activityType = deGiroRecordNL.GetActivityType();

			// Assert
			activityType.Should().Be(expectedActivityType);
		}

		[Fact]
		public void GetActivityType_ShouldReturnNull_WhenDescriptionIsEmpty()
		{
			// Arrange
			var deGiroRecordNL = DefaultFixture.Create().Build<DeGiroRecordNL>().With(x => x.Description, string.Empty).Create();

			// Act
			var activityType = deGiroRecordNL.GetActivityType();

			// Assert
			activityType.Should().BeNull();
		}

		[Fact]
		public void GetActivityType_ShouldReturnNull_WhenDescriptionDoesNotMatchAnyActivityType()
		{
			// Arrange
			var deGiroRecordNL = DefaultFixture.Create().Build<DeGiroRecordNL>().With(x => x.Description, "Some random description").Create();

			// Act
			var activityType = deGiroRecordNL.GetActivityType();

			// Assert
			activityType.Should().BeNull();
		}
	}
}
