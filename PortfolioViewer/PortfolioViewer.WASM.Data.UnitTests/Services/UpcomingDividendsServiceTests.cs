using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
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
using System.Linq;
using GhostfolioSidekick.Database.Repository;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.UnitTests.Services
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

            var calculatedSnapshot = new CalculatedSnapshotPrimaryCurrency 
            { 
                Date = DateOnly.FromDateTime(DateTime.Today), 
                Quantity = 10m 
            };

            var holdingAggregated = new HoldingAggregated
            {
                Symbol = "AAPL",
                CalculatedSnapshotsPrimaryCurrency = new List<CalculatedSnapshotPrimaryCurrency>
                {
                    calculatedSnapshot
                }
            };

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.Dividends).ReturnsDbSet(new List<Dividend> { dividend });
            mockContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile> { symbolProfile });
            mockContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(new List<HoldingAggregated> { holdingAggregated });
            mockContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency> { calculatedSnapshot });

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<System.Threading.CancellationToken>()))
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

            var calculatedSnapshot = new CalculatedSnapshotPrimaryCurrency 
            { 
                Date = DateOnly.FromDateTime(DateTime.Today), 
                Quantity = 5m 
            };

            var holdingAggregated = new HoldingAggregated
            {
                Symbol = "ASML",
                CalculatedSnapshotsPrimaryCurrency = new List<CalculatedSnapshotPrimaryCurrency>
                {
                    calculatedSnapshot
                }
            };

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.Dividends).ReturnsDbSet(new List<Dividend> { dividend });
            mockContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile> { symbolProfile });
            mockContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(new List<HoldingAggregated> { holdingAggregated });
            mockContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency> { calculatedSnapshot });

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<System.Threading.CancellationToken>()))
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
    }
}
