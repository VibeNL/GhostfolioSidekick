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
		[InlineData("DEGIRO Transactiekosten en/of kosten van derden", ActivityType.Fee)]
		[InlineData("Dividendbelasting", ActivityType.Tax)]
		[InlineData("Verkoop something", ActivityType.Sell)]
		[InlineData("Koop something", ActivityType.Buy)]
		[InlineData("Dividend", ActivityType.Dividend)]
		[InlineData("flatex terugstorting", ActivityType.CashWithdrawal)]
		[InlineData("Deposit something", ActivityType.CashDeposit)]
		[InlineData("DEGIRO Verrekening Promotie", ActivityType.CashDeposit)]
		public void GetActivityType_ShouldReturnCorrectActivityType(string description, ActivityType expectedActivityType)
		{
			// Arrange
			var deGiroRecordNL = DefaultFixture.Create().Build<DeGiroRecordNL>().With(x => x.Description, description).Create();

			// Act
			var activityType = deGiroRecordNL.GetActivityType();

			// Assert
			activityType.Should().Be(expectedActivityType);
		}

		[Fact]
		public void GetActivityType_ShouldReturnNull_WhenDescriptionIsNull()
		{
			// Arrange
			var deGiroRecordNL = DefaultFixture.Create().Build<DeGiroRecordNL>().Without(x => x.Description).Create();

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
