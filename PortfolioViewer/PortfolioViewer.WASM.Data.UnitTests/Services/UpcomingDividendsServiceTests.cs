using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Database.Repository;

namespace PortfolioViewer.WASM.Data.UnitTests.Services
{
    public class UpcomingDividendsServiceTests
    {
        [Fact]
        public async Task GetUpcomingDividendsAsync_ReturnsExpectedDividends()
        {
            // Arrange
            var symbolProfile = new SymbolProfile
            {
                Symbol = "AAPL",
                Name = "Apple Inc.",
                Currency = Currency.USD,
                DataSource = "TestSource"
            };

            var dividend = new Dividend
            {
                ExDividendDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                PaymentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                Amount = new Money(Currency.USD, 2.5m),
                SymbolProfileSymbol = "AAPL",
                SymbolProfileDataSource = "TestSource"
            };

            var calculatedSnapshot = new CalculatedSnapshot 
            { 
                Date = DateOnly.FromDateTime(DateTime.Today), 
                Quantity = 10m 
            };

            var holding = new Holding
            {
                SymbolProfiles = [symbolProfile],
                CalculatedSnapshots = [calculatedSnapshot]
            };

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.Dividends).ReturnsDbSet(new List<Dividend> { dividend });
            mockContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile> { symbolProfile });
            mockContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding> { holding });
            mockContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { calculatedSnapshot });

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockContext.Object);

            // Mock ICurrencyExchange - return the same money (no conversion for USD to USD)
            var mockCurrencyExchange = new Mock<ICurrencyExchange>();
            mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
                .ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

            // Mock IServerConfigurationService
            var mockConfigService = new Mock<IServerConfigurationService>();
            mockConfigService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.USD);

            var service = new UpcomingDividendsService(mockFactory.Object, mockCurrencyExchange.Object, mockConfigService.Object);

            // Act
            var result = await service.GetUpcomingDividendsAsync();

            // Assert
            Assert.Single(result);
            var div = result[0];
            Assert.Equal("AAPL", div.Symbol);
            Assert.Equal("Apple Inc.", div.CompanyName);
            
            // Native currency values
            Assert.Equal(25.0m, div.Amount); // 2.5 * 10
            Assert.Equal("USD", div.Currency);
            Assert.Equal(2.5m, div.DividendPerShare);
            
            // Primary currency values (same as native in this case)
            Assert.Equal(25.0m, div.AmountPrimaryCurrency); // 2.5 * 10
            Assert.Equal("USD", div.PrimaryCurrency);
            Assert.Equal(2.5m, div.DividendPerSharePrimaryCurrency);
            
            Assert.Equal(10m, div.Quantity);
        }

        [Fact]
        public async Task GetUpcomingDividendsAsync_WithCurrencyConversion_ReturnsConvertedAmounts()
        {
            // Arrange
            var symbolProfile = new SymbolProfile
            {
                Symbol = "ASML",
                Name = "ASML Holding NV",
                Currency = Currency.EUR,
                DataSource = "TestSource"
            };

            var dividend = new Dividend
            {
                ExDividendDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                PaymentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                Amount = new Money(Currency.EUR, 3.0m), // EUR dividend
                SymbolProfileSymbol = "ASML",
                SymbolProfileDataSource = "TestSource"
            };

            var calculatedSnapshot = new CalculatedSnapshot 
            { 
                Date = DateOnly.FromDateTime(DateTime.Today), 
                Quantity = 5m 
            };

            var holding = new Holding
            {
                SymbolProfiles = [symbolProfile],
                CalculatedSnapshots = [calculatedSnapshot]
            };

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.Dividends).ReturnsDbSet(new List<Dividend> { dividend });
            mockContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile> { symbolProfile });
            mockContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding> { holding });
            mockContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { calculatedSnapshot });

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockContext.Object);

            // Mock ICurrencyExchange - simulate EUR to USD conversion at 1.1 rate
            var mockCurrencyExchange = new Mock<ICurrencyExchange>();
            mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), Currency.USD, It.IsAny<DateOnly>()))
                .ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount * 1.1m));

            // Mock IServerConfigurationService to return USD as primary currency
            var mockConfigService = new Mock<IServerConfigurationService>();
            mockConfigService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.USD);

            var service = new UpcomingDividendsService(mockFactory.Object, mockCurrencyExchange.Object, mockConfigService.Object);

            // Act
            var result = await service.GetUpcomingDividendsAsync();

            // Assert
            Assert.Single(result);
            var div = result[0];
            Assert.Equal("ASML", div.Symbol);
            Assert.Equal("ASML Holding NV", div.CompanyName);
            
            // Native currency values (EUR)
            Assert.Equal(15.0m, div.Amount); // 3.0 * 5
            Assert.Equal("EUR", div.Currency);
            Assert.Equal(3.0m, div.DividendPerShare);
            
            // Primary currency values (USD)
            Assert.Equal(16.5m, div.AmountPrimaryCurrency); // 3.3 * 5 (3.0 * 1.1)
            Assert.Equal("USD", div.PrimaryCurrency);
            Assert.Equal(3.3m, div.DividendPerSharePrimaryCurrency); // 3.0 * 1.1
            
            Assert.Equal(5m, div.Quantity);
        }

        [Fact]
        public async Task GetUpcomingDividendsAsync_WhenNoCalculatedSnapshots_ReturnsEmptyList()
        {
            // Arrange
            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.Dividends).ReturnsDbSet(new List<Dividend>());
            mockContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile>());
            mockContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding>());
            mockContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>()); // Empty

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockContext.Object);

            var mockCurrencyExchange = new Mock<ICurrencyExchange>();
            var mockConfigService = new Mock<IServerConfigurationService>();
            mockConfigService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.USD);

            var service = new UpcomingDividendsService(mockFactory.Object, mockCurrencyExchange.Object, mockConfigService.Object);

            // Act
            var result = await service.GetUpcomingDividendsAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetUpcomingDividendsAsync_WhenHoldingHasZeroQuantity_ExcludesDividend()
        {
            // Arrange
            var symbolProfile = new SymbolProfile
            {
                Symbol = "ZERO",
                Name = "Zero Holdings",
                Currency = Currency.USD,
                DataSource = "TestSource"
            };

            var dividend = new Dividend
            {
                ExDividendDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                PaymentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                Amount = new Money(Currency.USD, 1.0m),
                SymbolProfileSymbol = "ZERO",
                SymbolProfileDataSource = "TestSource"
            };

            var calculatedSnapshot = new CalculatedSnapshot
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Quantity = 0m  // Zero quantity
            };

            var holding = new Holding
            {
                SymbolProfiles = [symbolProfile],
                CalculatedSnapshots = [calculatedSnapshot]
            };

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.Dividends).ReturnsDbSet(new List<Dividend> { dividend });
            mockContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile> { symbolProfile });
            mockContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding> { holding });
            mockContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { calculatedSnapshot });

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockContext.Object);

            var mockCurrencyExchange = new Mock<ICurrencyExchange>();
            var mockConfigService = new Mock<IServerConfigurationService>();
            mockConfigService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.USD);

            var service = new UpcomingDividendsService(mockFactory.Object, mockCurrencyExchange.Object, mockConfigService.Object);

            // Act
            var result = await service.GetUpcomingDividendsAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetUpcomingDividendsAsync_WhenCurrencyConversionFails_FallsBackToNativeAmount()
        {
            // Arrange
            var symbolProfile = new SymbolProfile
            {
                Symbol = "FAILCONV",
                Name = "Failed Conversion Inc.",
                Currency = Currency.EUR,
                DataSource = "TestSource"
            };

            var dividend = new Dividend
            {
                ExDividendDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                PaymentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                Amount = new Money(Currency.EUR, 5.0m),
                SymbolProfileSymbol = "FAILCONV",
                SymbolProfileDataSource = "TestSource"
            };

            var calculatedSnapshot = new CalculatedSnapshot
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Quantity = 10m
            };

            var holding = new Holding
            {
                SymbolProfiles = [symbolProfile],
                CalculatedSnapshots = [calculatedSnapshot]
            };

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.Dividends).ReturnsDbSet(new List<Dividend> { dividend });
            mockContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile> { symbolProfile });
            mockContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding> { holding });
            mockContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { calculatedSnapshot });

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockContext.Object);

            // Mock ICurrencyExchange to throw exception
            var mockCurrencyExchange = new Mock<ICurrencyExchange>();
            mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
                .ThrowsAsync(new Exception("Currency conversion service unavailable"));

            var mockConfigService = new Mock<IServerConfigurationService>();
            mockConfigService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.USD);

            var service = new UpcomingDividendsService(mockFactory.Object, mockCurrencyExchange.Object, mockConfigService.Object);

            // Act
            var result = await service.GetUpcomingDividendsAsync();

            // Assert - Should fallback to native amount
            Assert.Single(result);
            var div = result[0];
            Assert.Equal(5.0m, div.DividendPerSharePrimaryCurrency); // Fallback to native per-share
            Assert.Equal(50.0m, div.AmountPrimaryCurrency); // 5.0 * 10, fallback calculation
        }

        [Fact]
        public async Task GetUpcomingDividendsAsync_WithMultipleHoldingsOfSameSymbol_AggregatesQuantity()
        {
            // Arrange
            var symbolProfile = new SymbolProfile
            {
                Symbol = "MULTI",
                Name = "Multiple Holdings Inc.",
                Currency = Currency.USD,
                DataSource = "TestSource"
            };

            var dividend = new Dividend
            {
                ExDividendDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                PaymentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                Amount = new Money(Currency.USD, 2.0m),
                SymbolProfileSymbol = "MULTI",
                SymbolProfileDataSource = "TestSource"
            };

            var snapshot1 = new CalculatedSnapshot { Date = DateOnly.FromDateTime(DateTime.Today), Quantity = 10m };
            var snapshot2 = new CalculatedSnapshot { Date = DateOnly.FromDateTime(DateTime.Today), Quantity = 15m };

            var holding1 = new Holding
            {
                Id = 1,
                SymbolProfiles = [symbolProfile],
                CalculatedSnapshots = [snapshot1]
            };

            var holding2 = new Holding
            {
                Id = 2,
                SymbolProfiles = [symbolProfile],
                CalculatedSnapshots = [snapshot2]
            };

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.Dividends).ReturnsDbSet(new List<Dividend> { dividend });
            mockContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile> { symbolProfile });
            mockContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding> { holding1, holding2 });
            mockContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { snapshot1, snapshot2 });

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockContext.Object);

            var mockCurrencyExchange = new Mock<ICurrencyExchange>();
            mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
                .ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

            var mockConfigService = new Mock<IServerConfigurationService>();
            mockConfigService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.USD);

            var service = new UpcomingDividendsService(mockFactory.Object, mockCurrencyExchange.Object, mockConfigService.Object);

            // Act
            var result = await service.GetUpcomingDividendsAsync();

            // Assert
            Assert.Single(result);
            var div = result[0];
            Assert.Equal(25m, div.Quantity); // 10 + 15
            Assert.Equal(50.0m, div.Amount); // 2.0 * 25
        }

        [Fact]
        public async Task GetUpcomingDividendsAsync_WhenDividendAmountIsZero_ExcludesDividend()
        {
            // Arrange
            var symbolProfile = new SymbolProfile
            {
                Symbol = "ZERODIV",
                Name = "Zero Dividend Inc.",
                Currency = Currency.USD,
                DataSource = "TestSource"
            };

            var dividend = new Dividend
            {
                ExDividendDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                PaymentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                Amount = new Money(Currency.USD, 0m), // Zero amount
                SymbolProfileSymbol = "ZERODIV",
                SymbolProfileDataSource = "TestSource"
            };

            var calculatedSnapshot = new CalculatedSnapshot
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Quantity = 10m
            };

            var holding = new Holding
            {
                SymbolProfiles = [symbolProfile],
                CalculatedSnapshots = [calculatedSnapshot]
            };

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.Dividends).ReturnsDbSet(new List<Dividend> { dividend });
            mockContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile> { symbolProfile });
            mockContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding> { holding });
            mockContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { calculatedSnapshot });

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockContext.Object);

            var mockCurrencyExchange = new Mock<ICurrencyExchange>();
            var mockConfigService = new Mock<IServerConfigurationService>();
            mockConfigService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.USD);

            var service = new UpcomingDividendsService(mockFactory.Object, mockCurrencyExchange.Object, mockConfigService.Object);

            // Act
            var result = await service.GetUpcomingDividendsAsync();

            // Assert - Zero amount dividends are filtered out
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetUpcomingDividendsAsync_WithPredictedDividend_SetsPredictedFlag()
        {
            // Arrange
            var symbolProfile = new SymbolProfile
            {
                Symbol = "PRED",
                Name = "Predicted Inc.",
                Currency = Currency.USD,
                DataSource = "TestSource"
            };

            var dividend = new Dividend
            {
                ExDividendDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                PaymentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                Amount = new Money(Currency.USD, 1.5m),
                DividendState = DividendState.Predicted, // Predicted state
                SymbolProfileSymbol = "PRED",
                SymbolProfileDataSource = "TestSource"
            };

            var calculatedSnapshot = new CalculatedSnapshot
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Quantity = 20m
            };

            var holding = new Holding
            {
                SymbolProfiles = [symbolProfile],
                CalculatedSnapshots = [calculatedSnapshot]
            };

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.Dividends).ReturnsDbSet(new List<Dividend> { dividend });
            mockContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile> { symbolProfile });
            mockContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding> { holding });
            mockContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { calculatedSnapshot });

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockContext.Object);

            var mockCurrencyExchange = new Mock<ICurrencyExchange>();
            mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
                .ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

            var mockConfigService = new Mock<IServerConfigurationService>();
            mockConfigService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.USD);

            var service = new UpcomingDividendsService(mockFactory.Object, mockCurrencyExchange.Object, mockConfigService.Object);

            // Act
            var result = await service.GetUpcomingDividendsAsync();

            // Assert
            Assert.Single(result);
            Assert.True(result[0].IsPredicted);
        }

        [Fact]
        public async Task GetUpcomingDividendsAsync_FiltersOutPastDividends()
        {
            // Arrange
            var symbolProfile = new SymbolProfile
            {
                Symbol = "PAST",
                Name = "Past Dividend Inc.",
                Currency = Currency.USD,
                DataSource = "TestSource"
            };

            var pastDividend = new Dividend
            {
                ExDividendDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)),
                PaymentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)), // Past payment date
                Amount = new Money(Currency.USD, 1.0m),
                SymbolProfileSymbol = "PAST",
                SymbolProfileDataSource = "TestSource"
            };

            var futureDividend = new Dividend
            {
                ExDividendDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                PaymentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)), // Future payment date
                Amount = new Money(Currency.USD, 1.0m),
                SymbolProfileSymbol = "PAST",
                SymbolProfileDataSource = "TestSource"
            };

            var calculatedSnapshot = new CalculatedSnapshot
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Quantity = 10m
            };

            var holding = new Holding
            {
                SymbolProfiles = [symbolProfile],
                CalculatedSnapshots = [calculatedSnapshot]
            };

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.Dividends).ReturnsDbSet(new List<Dividend> { pastDividend, futureDividend });
            mockContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile> { symbolProfile });
            mockContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding> { holding });
            mockContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot> { calculatedSnapshot });

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockContext.Object);

            var mockCurrencyExchange = new Mock<ICurrencyExchange>();
            mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
                .ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

            var mockConfigService = new Mock<IServerConfigurationService>();
            mockConfigService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.USD);

            var service = new UpcomingDividendsService(mockFactory.Object, mockCurrencyExchange.Object, mockConfigService.Object);

            // Act
            var result = await service.GetUpcomingDividendsAsync();

            // Assert - Only future dividend should be returned
            Assert.Single(result);
            Assert.Equal(DateOnly.FromDateTime(DateTime.Today.AddDays(10)), DateOnly.FromDateTime(result[0].PaymentDate));
        }
    }
}