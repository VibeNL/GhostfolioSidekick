using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;

namespace GhostfolioSidekick.Tools.Database.UnitTests.Repository
{
	public class CurrencyExchangeTests
	{
		private readonly Mock<IDbContextFactory<DatabaseContext>> _dbContextFactoryMock;
		private readonly Mock<ILogger<CurrencyExchange>> _loggerMock;
		private readonly CurrencyExchange _currencyExchange;

		public CurrencyExchangeTests()
		{
			_dbContextFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			_loggerMock = new Mock<ILogger<CurrencyExchange>>();
			_currencyExchange = new CurrencyExchange(_dbContextFactoryMock.Object, new MemoryCache(new MemoryCacheOptions()), _loggerMock.Object);
		}

		[Fact]
		public async Task ConvertMoney_SameCurrency_ReturnsSameMoney()
		{
			// Arrange
			var money = new Money(Currency.USD, 100);
			var currency = Currency.USD;
			var date = DateOnly.FromDateTime(DateTime.Now);

			// Act
			var result = await _currencyExchange.ConvertMoney(money, currency, date);

			// Assert
			Assert.Equal(money, result);
		}

		[Fact]
		public async Task ConvertMoney_KnownPair_ReturnsConvertedMoney()
		{
			// Arrange
			var money = new Money(Currency.GBP, 100);
			var currency = Currency.GBp; // Note: GBp is the penny, GBP is the pound
			var date = DateOnly.FromDateTime(DateTime.Now);
			var exchangeRate = Currency.GBP.GetKnownExchangeRate(Currency.GBp); // Should be 0.01m

			// Act
			var result = await _currencyExchange.ConvertMoney(money, currency, date);

			// Assert
			Assert.Equal(new Money(currency, money.Amount * exchangeRate), result);
		}

		[Fact]
		public async Task ConvertMoney_ExchangeRateOnDate_ReturnsConvertedMoney()
		{
			// Arrange
			var money = new Money(Currency.USD, 100);
			var currency = Currency.EUR;
			var date = DateOnly.FromDateTime(DateTime.Now);
			var exchangeRate = 0.85m;

			var dbContextMock = new Mock<DatabaseContext>();
			dbContextMock.Setup(x => x.CurrencyExchangeRates)
				.ReturnsDbSet(new[]
				{
					new CurrencyExchangeProfile
					{
						SourceCurrency = Currency.USD,
						TargetCurrency = Currency.EUR,
						Rates =
						[
							new CurrencyExchangeRate
							{
								Date = date,
								Close = new Money(currency, exchangeRate),
								Open = new Money(currency, exchangeRate),
								High = new Money(currency, exchangeRate),
								Low = new Money(currency, exchangeRate),
								TradingVolume = 0
							}
						]
					}
				});

			_dbContextFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dbContextMock.Object);

			// Act
			var result = await _currencyExchange.ConvertMoney(money, currency, date);

			// Assert
			Assert.Equal(new Money(currency, money.Amount * exchangeRate), result);
		}

		[Fact]
		public async Task ConvertMoney_ExchangeRateNotOnDate_UsesPreviousRate()
		{
			// Arrange
			var money = new Money(Currency.USD, 100);
			var currency = Currency.EUR;
			var date = DateOnly.FromDateTime(DateTime.Now);
			var previousDate = date.AddDays(-1);
			var exchangeRate = 0.85m;

			var dbContextMock = new Mock<DatabaseContext>();
			dbContextMock.Setup(x => x.CurrencyExchangeRates)
				.ReturnsDbSet(new[]
				{
					new CurrencyExchangeProfile
					{
						SourceCurrency = Currency.USD,
						TargetCurrency = Currency.EUR,
						Rates =
						[
							new CurrencyExchangeRate
							{
								Date = previousDate,
								Close = new Money(currency, exchangeRate),
								Open = new Money(currency, exchangeRate),
								High = new Money(currency, exchangeRate),
								Low = new Money(currency, exchangeRate),
								TradingVolume = 0
							}
						]
					}
				});

			_dbContextFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dbContextMock.Object);

			// Act
			var result = await _currencyExchange.ConvertMoney(money, currency, date);

			// Assert
			Assert.Equal(new Money(currency, money.Amount * exchangeRate), result);
		}

		[Fact]
		public async Task ConvertMoney_NoExchangeRate_UsesOneToOneRate()
		{
			// Arrange
			var money = new Money(Currency.USD, 100);
			var currency = Currency.EUR;
			var date = DateOnly.FromDateTime(DateTime.Now);

			var dbContextMock = new Mock<DatabaseContext>();
			dbContextMock.Setup(x => x.CurrencyExchangeRates)
				.ReturnsDbSet(Array.Empty<CurrencyExchangeProfile>());

			_dbContextFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dbContextMock.Object);

			// Act
			var result = await _currencyExchange.ConvertMoney(money, currency, date);

			// Assert
			Assert.Equal(new Money(currency, money.Amount), result);
		}
	}
}
