using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Performance;
using Microsoft.Data.Sqlite;

namespace GhostfolioSidekick.UnitTests.Performance
{
    public class UpcomingDividendsTaskTests
    {
        private static DbContextOptions<DatabaseContext> CreateOptions(SqliteConnection connection) =>
            new DbContextOptionsBuilder<DatabaseContext>()
                .UseSqlite(connection)
                .Options;

        [Fact]
        public async Task DoWork_OfficialDividends_TimelineEntryIsCreated()
        {
            using var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var options = CreateOptions(connection);
            var dbContext = new DatabaseContext(options);
            await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

            var holding = new Holding
            {
                Id = 1,
                SymbolProfiles = [new SymbolProfile { Symbol = "TEST", Currency = Currency.USD, DataSource = "YAHOO", Name = "Test", AssetClass = AssetClass.Equity, CountryWeight = [], SectorWeights = [], Identifiers = [] }],
                CalculatedSnapshots = [new CalculatedSnapshot { Date = DateOnly.FromDateTime(DateTime.Today), Quantity = 1, Currency = Currency.USD }]
            };
            dbContext.Holdings.Add(holding);
            dbContext.Dividends.Add(new Dividend
            {
                ExDividendDate = DateOnly.FromDateTime(DateTime.Today),
                PaymentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                DividendType = DividendType.Cash,
                DividendState = DividendState.Declared,
                Amount = new Money(Currency.USD, 100),
                SymbolProfileSymbol = "TEST",
                SymbolProfileDataSource = "YAHOO"
            });
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
            dbFactoryMock.Setup(x => x.CreateDbContextAsync(default)).ReturnsAsync(() => new DatabaseContext(options));

            var currencyExchangeMock = new Mock<GhostfolioSidekick.Database.Repository.ICurrencyExchange>();
            currencyExchangeMock.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
                .ReturnsAsync((Money m, Currency c, DateOnly d) => new Money(c, m.Amount));

            var settings = new Settings { PrimaryCurrency = "USD", DataProviderPreference = "YAHOO;COINGECKO" };
            var configInstance = new ConfigurationInstance { Settings = settings };
            var appSettingsMock = new Mock<IApplicationSettings>();
            appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configInstance);

            var loggerMock = new Mock<ILogger>();
            var task = new UpcomingDividendsTask(dbFactoryMock.Object, currencyExchangeMock.Object, appSettingsMock.Object);
            await task.DoWork(loggerMock.Object);

            var result = dbContext.UpcomingDividendTimelineEntries.FirstOrDefault(x => x.HoldingId == 1);
            Assert.NotNull(result);
            Assert.Equal(100, result.Amount);
            Assert.Equal("USD", result.Currency.Symbol);
            Assert.Equal(DividendType.Cash, result.DividendType);
            Assert.Equal(DividendState.Declared, result.DividendState);
        }

        [Fact]
      public async Task DoWork_PredictsFromPastDividendActivity_WhenNoOfficial()
        {
            using var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var options = CreateOptions(connection);
            var dbContext = new DatabaseContext(options);
            await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

            var account = new Account { Id = 1, Name = "Test Account" };
            dbContext.Accounts.Add(account);
            var holding = new Holding
            {
                Id = 2,
                SymbolProfiles = [new SymbolProfile { Symbol = "TEST2", Currency = Currency.EUR, DataSource = "YAHOO", Name = "Test2", AssetClass = AssetClass.Equity, CountryWeight = [], SectorWeights = [], Identifiers = [] }]
            };
            dbContext.Holdings.Add(holding);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var eur = Currency.GetCurrency("EUR");
            var activities = new List<DividendActivity>
            {
                new DividendActivity { Amount = new Money(eur, 50), Date = DateTime.Today.AddMonths(-3), TransactionId = Guid.NewGuid().ToString(), Account = account, Holding = holding },
                new DividendActivity { Amount = new Money(eur, 60), Date = DateTime.Today.AddMonths(-6), TransactionId = Guid.NewGuid().ToString(), Account = account, Holding = holding },
                new DividendActivity { Amount = new Money(eur, 70), Date = DateTime.Today.AddMonths(-9), TransactionId = Guid.NewGuid().ToString(), Account = account, Holding = holding }
            };
            dbContext.AddRange(activities);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
            dbFactoryMock.Setup(x => x.CreateDbContextAsync(default)).ReturnsAsync(() => new DatabaseContext(options));

            var currencyExchangeMock = new Mock<GhostfolioSidekick.Database.Repository.ICurrencyExchange>();
            currencyExchangeMock.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
                .ReturnsAsync((Money m, Currency c, DateOnly d) => new Money(c, m.Amount));

            var settings = new Settings { PrimaryCurrency = "EUR", DataProviderPreference = "YAHOO;COINGECKO" };
            var configInstance = new ConfigurationInstance { Settings = settings };
            var appSettingsMock = new Mock<IApplicationSettings>();
            appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configInstance);

            var loggerMock = new Mock<ILogger>();
            var task = new UpcomingDividendsTask(dbFactoryMock.Object, currencyExchangeMock.Object, appSettingsMock.Object);
            await task.DoWork(loggerMock.Object);

            var result = dbContext.UpcomingDividendTimelineEntries.FirstOrDefault(x => x.HoldingId == 2);
            Assert.NotNull(result);
            Assert.Equal("EUR", result.Currency.Symbol);
            Assert.True(result.Amount > 0);
            Assert.Equal(DividendType.Cash, result.DividendType);
            Assert.Equal(DividendState.Predicted, result.DividendState);
        }
    }
}
