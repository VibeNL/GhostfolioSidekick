using FluentAssertions;

namespace GhostfolioSidekick.Model.UnitTests
{
	public class MoneyTests
	{
		[Fact]
		public void Equals_ShouldReturnTrue_WhenSameCurrencyAndAmount()
		{
			// Arrange
			var money1 = new Money(Currency.USD, 100);
			var money2 = new Money(Currency.USD, 100);

			// Act
			var result = money1.Equals(money2);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void Equals_ShouldReturnFalse_WhenDifferentCurrency()
		{
			// Arrange
			var money1 = new Money(Currency.USD, 100);
			var money2 = new Money(Currency.EUR, 100);

			// Act
			var result = money1.Equals(money2);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void Equals_ShouldReturnFalse_WhenDifferentAmount()
		{
			// Arrange
			var money1 = new Money(Currency.USD, 100);
			var money2 = new Money(Currency.USD, 200);

			// Act
			var result = money1.Equals(money2);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void GetHashCode_ShouldReturnSameHashCode_WhenSameCurrencyAndAmount()
		{
			// Arrange
			var money1 = new Money(Currency.USD, 100);
			var money2 = new Money(Currency.USD, 100);

			// Act
			var hash1 = money1.GetHashCode();
			var hash2 = money2.GetHashCode();

			// Assert
			hash1.Should().Be(hash2);
		}

		[Fact]
		public void ToString_ShouldReturnCorrectFormat()
		{
			// Arrange
			var money = new Money(Currency.USD, 100);

			// Act
			var result = money.ToString();

			// Assert
			result.Should().Be("100 USD");
		}
	}
}
