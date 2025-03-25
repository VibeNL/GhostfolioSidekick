using FluentAssertions;
using GhostfolioSidekick.Parsers.DeGiro;
using GhostfolioSidekick.Model.Activities;
using AutoFixture;

namespace GhostfolioSidekick.Parsers.UnitTests.DeGiro
{
	public class DeGiroRecordENTests
	{
		[Theory]
		[InlineData("DEGIRO Transaction and/or third party fees", PartialActivityType.Fee)]
		[InlineData("Dividend Tax", PartialActivityType.Tax)]
		[InlineData("Sell something", PartialActivityType.Sell)]
		[InlineData("Buy  something", PartialActivityType.Buy)]
		[InlineData("Dividend", PartialActivityType.Dividend)]
		public void GetActivityType_ShouldReturnCorrectActivityType(string description, PartialActivityType expectedActivityType)
		{
			// Arrange
			var deGiroRecordNL = DefaultFixture.Create().Build<DeGiroEnglishStrategy>().With(x => x.Description, description).Create();

			// Act
			var activityType = deGiroRecordNL.GetActivityType();

			// Assert
			activityType.Should().Be(expectedActivityType);
		}

		[Fact]
		public void GetActivityType_ShouldReturnNull_WhenDescriptionIsEmpty()
		{
			// Arrange
			var deGiroRecordNL = DefaultFixture.Create().Build<DeGiroEnglishStrategy>().With(x => x.Description, string.Empty).Create();

			// Act
			var activityType = deGiroRecordNL.GetActivityType();

			// Assert
			activityType.Should().BeNull();
		}

		[Fact]
		public void GetActivityType_ShouldReturnNull_WhenDescriptionDoesNotMatchAnyActivityType()
		{
			// Arrange
			var deGiroRecordNL = DefaultFixture.Create().Build<DeGiroEnglishStrategy>().With(x => x.Description, "Some random description").Create();

			// Act
			var activityType = deGiroRecordNL.GetActivityType();

			// Assert
			activityType.Should().BeNull();
		}
	}
}
