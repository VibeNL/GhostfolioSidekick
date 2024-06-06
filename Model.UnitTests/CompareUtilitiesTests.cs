using FluentAssertions;
using GhostfolioSidekick.Model.Compare;
using Moq;

namespace GhostfolioSidekick.Model.UnitTests
{
	public class CompareUtilitiesTests
	{
		private readonly Mock<IExchangeRateService> exchangeRateServiceMock;

		public CompareUtilitiesTests()
		{
			exchangeRateServiceMock = new Mock<IExchangeRateService>();
		}

		[Fact]
		public void AreNumbersEquals_Decimal_ShouldReturnTrue_WhenNumbersAreEqual()
		{
			// Arrange
			var a = 1.23m;
			var b = 1.23m;

			// Act
			bool result = CompareUtilities.AreNumbersEquals(a, b);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void AreNumbersEquals_Decimal_ShouldReturnTrue_WhenNumbersAreNull()
		{
			// Arrange

			// Act
			bool result = CompareUtilities.AreNumbersEquals((decimal?)null, null);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void AreNumbersEquals_Money_ShouldReturnTrue_WhenAmountsAreEqual()
		{
			// Arrange
			var a = new Money(Currency.USD, 1.23m);
			var b = new Money(Currency.USD, 1.23m);

			// Act
			bool result = CompareUtilities.AreNumbersEquals(a, b);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void AreNumbersEquals_Money_ShouldReturnTrue_WhenAmountsAreNull()
		{
			// Arrange

			// Act
			bool result = CompareUtilities.AreNumbersEquals((Money?)null, null);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void AreMoneyEquals_ShouldReturnTrue_WhenMoneyListsAreEqual()
		{
			// Arrange
			var target = Currency.USD;
			var dateTime = DateTime.Now;
			var money1 = new List<Money> { new Money(Currency.USD, 1.23m) };
			var money2 = new List<Money> { new Money(Currency.USD, 1.23m) };

			exchangeRateServiceMock
				.Setup(x => x.GetConversionRate(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTime>()))
				.ReturnsAsync(1m);

			// Act
			bool result = CompareUtilities.AreMoneyEquals(exchangeRateServiceMock.Object, target, dateTime, money1, money2);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public async Task RoundAndConvert_ShouldReturnRoundedAndConvertedMoney()
		{
			// Arrange
			var value = new Money(Currency.USD, 1.23456789m);
			var target = Currency.EUR;
			var dateTime = DateTime.Now;

			exchangeRateServiceMock
				.Setup(x => x.GetConversionRate(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTime>()))
				.ReturnsAsync(0.85m);

			// Act
			var result = await CompareUtilities.Convert(exchangeRateServiceMock.Object, value, target, dateTime);

			// Assert
			result.Should().NotBeNull();
			result!.Currency.Should().Be(target);
			result.Amount.Should().BeApproximately(1.05m, 0.01m);
		}
	}
}
