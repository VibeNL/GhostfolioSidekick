using Xunit;
using FluentAssertions;
using GhostfolioSidekick.Parsers.DeGiro;
using GhostfolioSidekick.Model.Activities;
using AutoFixture;
using GhostfolioSidekick.Parsers.UnitTests;

namespace GhostfolioSidekick.UnitTests.Parsers.DeGiro
{
	public class DeGiroRecordPTTests
	{
		[Theory]
		[InlineData("Comissões de transação DEGIRO e/ou taxas de terceiros", ActivityType.Fee)]
		[InlineData("Venda", ActivityType.Sell)]
		[InlineData("Compra", ActivityType.Buy)]
		[InlineData("Dividendo", ActivityType.Dividend)]
		[InlineData("Processed Flatex Withdrawal", ActivityType.CashWithdrawal)]
		[InlineData("Depósitos", ActivityType.CashDeposit)]
		[InlineData("Flatex Interest Income", ActivityType.Interest)]
		[InlineData("Custo de Conectividade DEGIRO", ActivityType.Fee)]
		public void GetActivityType_WhenDescriptionIsNotNull_ShouldReturnExpectedActivityType(string description, ActivityType expectedActivityType)
		{
			// Arrange
			var record = DefaultFixture.Create().Build<DeGiroRecordPT>().With(x => x.Description, description).Create();

			// Act
			var result = record.GetActivityType();

			// Assert
			result.Should().Be(expectedActivityType);
		}

		[Fact]
		public void GetActivityType_WhenDescriptionIsNull_ShouldReturnNull()
		{
			// Arrange
			var record = DefaultFixture.Create().Build<DeGiroRecordPT>().Without(x => x.Description).Create();

			// Act
			var result = record.GetActivityType();

			// Assert
			result.Should().BeNull();
		}
		
		[Fact]
		public void GetActivityType_WhenDescriptionDoesNotMatchAnyCondition_ShouldReturnNull()
		{
			// Arrange
			var record = DefaultFixture.Create().Build<DeGiroRecordPT>().With(x => x.Description, "Some random description").Create();

			// Act
			var result = record.GetActivityType();

			// Assert
			result.Should().BeNull();
		}
	}
}
