using AutoFixture;
using AwesomeAssertions;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.DeGiro;

namespace GhostfolioSidekick.Parsers.UnitTests.DeGiro
{
	public class DeGiroEnglishStrategyTests
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
			var record = DefaultFixture.Create().Build<DeGiroRecord>().With(x => x.Description, description).Create();
			var strategy = new DeGiroEnglishStrategy();

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
			var strategy = new DeGiroEnglishStrategy();

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
			var strategy = new DeGiroEnglishStrategy();

			// Act
			var activityType = strategy.GetActivityType(record);

			// Assert
			activityType.Should().BeNull();
		}
	}
}
