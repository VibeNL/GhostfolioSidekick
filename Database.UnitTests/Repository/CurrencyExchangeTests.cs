using System;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Logging;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace Database.UnitTests.Repository
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
			_currencyExchange = new CurrencyExchange(_dbContextFactoryMock.Object, _loggerMock.Object);
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
			var currency = Currency.GBp;
			var date = DateOnly.FromDateTime(DateTime.Now);
			var exchangeRate = 100m;

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
			dbContextMock.Setup(x => x.SymbolProfiles)
				.ReturnsDbSet(new[]
				{
					new SymbolProfile
					{
						Symbol = $"{money.Currency.Symbol}{currency.Symbol}",
						MarketData = new[]
						{
							new MarketData { Date = date, Close = new Money(currency, exchangeRate) }
						}
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
			dbContextMock.Setup(x => x.SymbolProfiles)
				.ReturnsDbSet(new[]
				{
					new SymbolProfile
					{
						Symbol = $"{money.Currency.Symbol}{currency.Symbol}",
						MarketData = new[]
						{
							new MarketData { Date = previousDate, Close =  new Money(currency, exchangeRate) }
						}
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
			dbContextMock.Setup(x => x.SymbolProfiles)
				.ReturnsDbSet(new SymbolProfile[0]);

			_dbContextFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dbContextMock.Object);

			// Act
			var result = await _currencyExchange.ConvertMoney(money, currency, date);

			// Assert
			Assert.Equal(new Money(currency, money.Amount), result);
		}
	}
}
