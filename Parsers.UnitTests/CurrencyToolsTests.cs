using FluentAssertions;

namespace GhostfolioSidekick.Parsers.UnitTests
{
	public class CurrencyToolsTests
	{
		[Fact]
		public void Map_ShouldReturnDictionary()
		{
			// Act
			var result = CurrencyTools.Map;

			// Assert
			result.Should().NotBeNull();
			result.Should().BeOfType<Dictionary<string, string>>();
		}

		[Theory]
		[InlineData("USD", "\u0024")]
		[InlineData("EUR", "\u20AC")]
		public void TryGetCurrencySymbol_ValidISOCurrencySymbol_ShouldReturnTrueAndSymbol(string isoCurrencySymbol, string expectedSymbol)
		{
			// Act
			var result = CurrencyTools.TryGetCurrencySymbol(isoCurrencySymbol, out string? symbol);

			// Assert
			result.Should().BeTrue();
			symbol.Should().Be(expectedSymbol);
		}

		[Theory]
		[InlineData("XYZ")]
		public void TryGetCurrencySymbol_InvalidISOCurrencySymbol_ShouldReturnFalse(string isoCurrencySymbol)
		{
			// Act
			var result = CurrencyTools.TryGetCurrencySymbol(isoCurrencySymbol, out string? symbol);

			// Assert
			result.Should().BeFalse();
			symbol.Should().BeNull();
		}

		[Theory]
		[InlineData("\u0024", "USD")]
		[InlineData("\u20AC", "EUR")]
		public void GetCurrencyFromSymbol_ValidCurrencySymbol_ShouldReturnISOCurrencySymbol(string currencySymbol, string expectedISOCurrencySymbol)
		{
			// Act
			var result = CurrencyTools.GetCurrencyFromSymbol(currencySymbol);

			// Assert
			result.Should().Be(expectedISOCurrencySymbol);
		}

		[Theory]
		[InlineData("XYZ")]
		public void GetCurrencyFromSymbol_InvalidCurrencySymbol_ShouldThrowArgumentException(string currencySymbol)
		{
			// Act
			var act = new Action(() => CurrencyTools.GetCurrencyFromSymbol(currencySymbol));

			// Assert
			act.Should().Throw<ArgumentException>().WithMessage("Currency symbol not found. Searched for XYZ");
		}
	}
}
