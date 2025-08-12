using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Moq;
using Moq.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests
{
    public class DividendsDataServiceTests
    {
        [Fact]
        public async Task GetDividendsAsync_ReturnsExpectedDividends()
        {
            // Arrange
            var targetCurrency = Currency.USD;
            
            var platform = new Platform("Test Platform");
            var account = new Account("Test Account") { Platform = platform };
            var symbolProfile = new SymbolProfile
            {
                Symbol = "AAPL",
                Name = "Apple Inc.",
                AssetClass = AssetClass.Equity,
                SectorWeights = [new SectorWeight { Name = "Technology" }]
            };
            var holding = new Holding();
            holding.SymbolProfiles.Add(symbolProfile);

            var dividendActivities = new List<DividendActivity>
            {
                new DividendActivity(
                    account,
                    holding,
                    new List<PartialSymbolIdentifier>(),
                    new DateTime(2024, 1, 15),
                    new Money(Currency.USD, 50),
                    "DIV001",
                    1,
                    "Apple dividend"
                )
                {
                    Taxes = new List<DividendActivityTax>
                    {
                        new DividendActivityTax(new Money(Currency.USD, 7.5m))
                    }
                }
            };

            var dbContextMock = new Mock<DatabaseContext>();
            dbContextMock.Setup(x => x.Activities).ReturnsDbSet(dividendActivities.Cast<Model.Activities.Activity>().ToList());

            var currencyExchangeMock = new Mock<ICurrencyExchange>();
            currencyExchangeMock.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
                .ReturnsAsync((Money m, Currency c, DateOnly d) => m);

            var service = new DividendsDataService(dbContextMock.Object, currencyExchangeMock.Object);

            // Act
            var result = await service.GetDividendsAsync(
                targetCurrency,
                new DateTime(2024, 1, 1),
                new DateTime(2024, 12, 31));

            // Assert
            Assert.Single(result);
            var dividend = result[0];
            Assert.Equal("AAPL", dividend.Symbol);
            Assert.Equal("Apple Inc.", dividend.Name);
            Assert.Equal(new DateTime(2024, 1, 15), dividend.Date);
            Assert.Equal(50, dividend.Amount.Amount);
            Assert.Equal(7.5m, dividend.TaxAmount.Amount);
            Assert.Equal(42.5m, dividend.NetAmount.Amount);
            Assert.Equal("Test Account", dividend.AccountName);
            Assert.Equal("Equity", dividend.AssetClass);
            Assert.Equal("Technology", dividend.Sector);
        }

        [Fact]
        public async Task GetMonthlyDividendsAsync_ReturnsAggregatedData()
        {
            // Arrange
            var targetCurrency = Currency.USD;
            
            var platform = new Platform("Test Platform");
            var account = new Account("Test Account") { Platform = platform };
            var symbolProfile = new SymbolProfile
            {
                Symbol = "AAPL",
                Name = "Apple Inc.",
                AssetClass = AssetClass.Equity
            };
            var holding = new Holding();
            holding.SymbolProfiles.Add(symbolProfile);

            var dividendActivities = new List<DividendActivity>
            {
                new DividendActivity(
                    account,
                    holding,
                    new List<PartialSymbolIdentifier>(),
                    new DateTime(2024, 1, 15),
                    new Money(Currency.USD, 50),
                    "DIV001",
                    1,
                    "Apple dividend 1"
                ),
                new DividendActivity(
                    account,
                    holding,
                    new List<PartialSymbolIdentifier>(),
                    new DateTime(2024, 1, 20),
                    new Money(Currency.USD, 30),
                    "DIV002",
                    2,
                    "Apple dividend 2"
                ),
                new DividendActivity(
                    account,
                    holding,
                    new List<PartialSymbolIdentifier>(),
                    new DateTime(2024, 2, 15),
                    new Money(Currency.USD, 45),
                    "DIV003",
                    3,
                    "Apple dividend 3"
                )
            };

            var dbContextMock = new Mock<DatabaseContext>();
            dbContextMock.Setup(x => x.Activities).ReturnsDbSet(dividendActivities.Cast<Model.Activities.Activity>().ToList());

            var currencyExchangeMock = new Mock<ICurrencyExchange>();
            currencyExchangeMock.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
                .ReturnsAsync((Money m, Currency c, DateOnly d) => m);

            var service = new DividendsDataService(dbContextMock.Object, currencyExchangeMock.Object);

            // Act
            var result = await service.GetMonthlyDividendsAsync(
                targetCurrency,
                new DateTime(2024, 1, 1),
                new DateTime(2024, 12, 31));

            // Assert
            Assert.Equal(2, result.Count);
            
            var january = result.First(r => r.Period == "2024-01");
            Assert.Equal(80, january.TotalAmount.Amount);
            Assert.Equal(2, january.DividendCount);
            
            var february = result.First(r => r.Period == "2024-02");
            Assert.Equal(45, february.TotalAmount.Amount);
            Assert.Equal(1, february.DividendCount);
        }

        [Fact]
        public async Task GetYearlyDividendsAsync_ReturnsAggregatedData()
        {
            // Arrange
            var targetCurrency = Currency.USD;
            
            var platform = new Platform("Test Platform");
            var account = new Account("Test Account") { Platform = platform };
            var symbolProfile = new SymbolProfile
            {
                Symbol = "AAPL",
                Name = "Apple Inc.",
                AssetClass = AssetClass.Equity
            };
            var holding = new Holding();
            holding.SymbolProfiles.Add(symbolProfile);

            var dividendActivities = new List<DividendActivity>
            {
                new DividendActivity(
                    account,
                    holding,
                    new List<PartialSymbolIdentifier>(),
                    new DateTime(2023, 6, 15),
                    new Money(Currency.USD, 40),
                    "DIV001",
                    1,
                    "Apple dividend 2023"
                ),
                new DividendActivity(
                    account,
                    holding,
                    new List<PartialSymbolIdentifier>(),
                    new DateTime(2024, 1, 15),
                    new Money(Currency.USD, 50),
                    "DIV002",
                    2,
                    "Apple dividend 2024"
                )
            };

            var dbContextMock = new Mock<DatabaseContext>();
            dbContextMock.Setup(x => x.Activities).ReturnsDbSet(dividendActivities.Cast<Model.Activities.Activity>().ToList());

            var currencyExchangeMock = new Mock<ICurrencyExchange>();
            currencyExchangeMock.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
                .ReturnsAsync((Money m, Currency c, DateOnly d) => m);

            var service = new DividendsDataService(dbContextMock.Object, currencyExchangeMock.Object);

            // Act
            var result = await service.GetYearlyDividendsAsync(
                targetCurrency,
                new DateTime(2023, 1, 1),
                new DateTime(2024, 12, 31));

            // Assert
            Assert.Equal(2, result.Count);
            
            var year2023 = result.First(r => r.Period == "2023");
            Assert.Equal(40, year2023.TotalAmount.Amount);
            Assert.Equal(1, year2023.DividendCount);
            
            var year2024 = result.First(r => r.Period == "2024");
            Assert.Equal(50, year2024.TotalAmount.Amount);
            Assert.Equal(1, year2024.DividendCount);
        }

        [Fact]
        public async Task GetDividendsAsync_WithAccountFilter_ReturnsFilteredResults()
        {
            // Arrange
            var targetCurrency = Currency.USD;
            
            var platform = new Platform("Test Platform");
            var account1 = new Account("Account 1") { Id = 1, Platform = platform };
            var account2 = new Account("Account 2") { Id = 2, Platform = platform };
            var symbolProfile = new SymbolProfile
            {
                Symbol = "AAPL",
                Name = "Apple Inc.",
                AssetClass = AssetClass.Equity
            };
            var holding1 = new Holding();
            holding1.SymbolProfiles.Add(symbolProfile);
            var holding2 = new Holding();
            holding2.SymbolProfiles.Add(symbolProfile);

            var dividendActivities = new List<DividendActivity>
            {
                new DividendActivity(
                    account1,
                    holding1,
                    new List<PartialSymbolIdentifier>(),
                    new DateTime(2024, 1, 15),
                    new Money(Currency.USD, 50),
                    "DIV001",
                    1,
                    "Account 1 dividend"
                ),
                new DividendActivity(
                    account2,
                    holding2,
                    new List<PartialSymbolIdentifier>(),
                    new DateTime(2024, 1, 20),
                    new Money(Currency.USD, 30),
                    "DIV002",
                    2,
                    "Account 2 dividend"
                )
            };

            var dbContextMock = new Mock<DatabaseContext>();
            dbContextMock.Setup(x => x.Activities).ReturnsDbSet(dividendActivities.Cast<Model.Activities.Activity>().ToList());

            var currencyExchangeMock = new Mock<ICurrencyExchange>();
            currencyExchangeMock.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
                .ReturnsAsync((Money m, Currency c, DateOnly d) => m);

            var service = new DividendsDataService(dbContextMock.Object, currencyExchangeMock.Object);

            // Act
            var result = await service.GetDividendsAsync(
                targetCurrency,
                new DateTime(2024, 1, 1),
                new DateTime(2024, 12, 31),
                accountId: 1);

            // Assert
            Assert.Single(result);
            Assert.Equal("Account 1", result[0].AccountName);
            Assert.Equal(50, result[0].Amount.Amount);
        }
    }
}