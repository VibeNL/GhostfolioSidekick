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
		[InlineData("Comissões de transação DEGIRO e/ou taxas de terceiros", PartialActivityType.Fee)]
		[InlineData("Venda", PartialActivityType.Sell)]
		[InlineData("Compra", PartialActivityType.Buy)]
		[InlineData("Dividendo", PartialActivityType.Dividend)]
		[InlineData("Processed Flatex Withdrawal", PartialActivityType.CashWithdrawal)]
		[InlineData("Depósitos", PartialActivityType.CashDeposit)]
		[InlineData("Flatex Interest Income", PartialActivityType.Interest)]
		[InlineData("Custo de Conectividade DEGIRO", PartialActivityType.Fee)]
		public void GetActivityType_WhenDescriptionIsNotNull_ShouldReturnExpectedActivityType(string description, PartialActivityType expectedActivityType)
		{
			// Arrange
			var record = DefaultFixture.Create().Build<DeGiroRecordPT>().With(x => x.Description, description).Create();

			// Act
			var result = record.GetActivityType();

			// Assert
			result.Should().Be(expectedActivityType);
		}

		[Fact]
		public void GetActivityType_WhenDescriptionIsNull_ShouldReturnEmpty()
		{
			// Arrange
			var record = DefaultFixture.Create().Build<DeGiroRecordPT>().With(x => x.Description, string.Empty).Create();

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
