using FluentAssertions;

namespace GhostfolioSidekick.Model.UnitTests
{
	public class CurrencyTests
	{
		[Fact]
		public void Should_Create_Currency_With_Symbol()
		{
			// Arrange
			var symbol = "USD";

			// Act
			var currency = Currency.GetCurrency(symbol);

			// Assert
			currency.Symbol.Should().Be(symbol);
		}

		[Fact]
		public void Should_Check_If_Currency_Is_Fiat()
		{
			// Arrange
			var symbol = "USD";
			var currency = Currency.GetCurrency(symbol);

			// Act
			var isFiat = currency.IsFiat();

			// Assert
			isFiat.Should().BeTrue();
		}

		[Fact]
		public void Should_Check_If_Currency_Is_Not_Fiat()
		{
			// Arrange
			var symbol = "BTC";
			var currency = Currency.GetCurrency(symbol);

			// Act
			var isFiat = currency.IsFiat();

			// Assert
			isFiat.Should().BeFalse();
		}

		[Fact]
		public void Should_Check_Currency_Equality()
		{
			// Arrange
			var currency1 = Currency.GetCurrency("USD");
			var currency2 = Currency.GetCurrency("USD");

			// Act & Assert
			currency1.Equals(currency2).Should().BeTrue();
		}

		[Fact]
		public void Should_Check_Currency_Inequality()
		{
			// Arrange
			var currency1 = Currency.GetCurrency("USD");
			var currency2 = Currency.GetCurrency("EUR");

			// Act & Assert
			currency1.Equals(currency2).Should().BeFalse();
		}
	}
}
