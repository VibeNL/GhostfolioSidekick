using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
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
        public async Task DoWork_WithAnnouncedDividendAndQuarterlyPattern_AddsPredictedAnnouncedActivity()
        {
            using var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var options = CreateOptions(connection);

            await using (var setupContext = new DatabaseContext(options))
            {
                await setupContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
                var today = DateOnly.FromDateTime(DateTime.Today);
                var announcedDate = today.AddDays(40);

                var account = new Account("Main") { Id = 1 };
                var symbolProfile = new SymbolProfile
                {
                    Symbol = "QDIV",
                    Name = "Quarterly Dividend",
                    Currency = Currency.USD,
                    DataSource = "YAHOO",
                    AssetClass = AssetClass.Equity,
                    CountryWeight = [],
                    SectorWeights = [],
                    Identifiers = [],
                    Dividends =
                    [
                        new Dividend
                        {
                            ExDividendDate = announcedDate.AddDays(-14),
                            PaymentDate = announcedDate,
                            DividendType = DividendType.Cash,
                            DividendState = DividendState.Declared,
                            Amount = new Money(Currency.USD, 0),
                            SymbolProfileSymbol = "QDIV",
                            SymbolProfileDataSource = "YAHOO"
                        }
                    ]
                };

                var holding = new Holding
                {
                    Id = 101,
                    SymbolProfiles = [symbolProfile],
                    CalculatedSnapshots =
                    [
                        new CalculatedSnapshot { HoldingId = 101, AccountId = 1, Date = today.AddMonths(-13), Quantity = 10, Currency = Currency.USD },
                        new CalculatedSnapshot { HoldingId = 101, AccountId = 1, Date = today.AddDays(-20), Quantity = 25, Currency = Currency.USD }
                    ]
                };

                setupContext.Accounts.Add(account);
                setupContext.Holdings.Add(holding);
                await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                setupContext.Activities.AddRange(
                    BuildHistoricalDividend(account, holding, "QDIV", DateTime.Today.AddMonths(-12), 11m),
                    BuildHistoricalDividend(account, holding, "QDIV", DateTime.Today.AddMonths(-9), 12m),
                    BuildHistoricalDividend(account, holding, "QDIV", DateTime.Today.AddMonths(-6), 13m),
                    BuildHistoricalDividend(account, holding, "QDIV", DateTime.Today.AddMonths(-3), 14m));
                await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var task = CreateTask(options, "USD");
            await task.DoWork(new Mock<ILogger>().Object);

            await using var verifyContext = new DatabaseContext(options);
            var predictedActivities = await verifyContext.Activities
                .OfType<DividendActivity>()
                .Where(x => x.IsPredicted)
                .ToListAsync(TestContext.Current.CancellationToken);

            var announcedPrediction = predictedActivities
                .FirstOrDefault(x => DateOnly.FromDateTime(x.Date) == DateOnly.FromDateTime(DateTime.Today).AddDays(40));

            Assert.NotNull(announcedPrediction);
            Assert.Contains("predicted-announced", announcedPrediction.TransactionId);
            Assert.True(announcedPrediction.IsPredicted);
            Assert.Equal(31.25m, announcedPrediction.Amount.Amount);
        }

        [Fact]
        public async Task DoWork_WithMonthlyPatternAndNoAnnouncements_AddsPredictedUnannouncedActivities()
        {
            using var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var options = CreateOptions(connection);

            DateOnly expectedFirstProjectedDate;
            await using (var setupContext = new DatabaseContext(options))
            {
                await setupContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
                var today = DateOnly.FromDateTime(DateTime.Today);

                var account = new Account("Main") { Id = 2 };
                var holding = new Holding
                {
                    Id = 102,
                    SymbolProfiles =
                    [
                        new SymbolProfile
                        {
                            Symbol = "MDIV",
                            Name = "Monthly Dividend",
                            Currency = Currency.EUR,
                            DataSource = "YAHOO",
                            AssetClass = AssetClass.Equity,
                            CountryWeight = [],
                            SectorWeights = [],
                            Identifiers = []
                        }
                    ],
                    CalculatedSnapshots =
                    [
                        new CalculatedSnapshot { HoldingId = 102, AccountId = 2, Date = today.AddMonths(-5), Quantity = 5, Currency = Currency.EUR },
                        new CalculatedSnapshot { HoldingId = 102, AccountId = 2, Date = today.AddDays(-15), Quantity = 20, Currency = Currency.EUR }
                    ]
                };

                setupContext.Accounts.Add(account);
                setupContext.Holdings.Add(holding);
                await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                var d1 = DateTime.Today.AddMonths(-4);
                var d2 = DateTime.Today.AddMonths(-3);
                var d3 = DateTime.Today.AddMonths(-2);
                var d4 = DateTime.Today.AddMonths(-1);
                setupContext.Activities.AddRange(
                    BuildHistoricalDividend(account, holding, "MDIV", d1, 8m),
                    BuildHistoricalDividend(account, holding, "MDIV", d2, 8.5m),
                    BuildHistoricalDividend(account, holding, "MDIV", d3, 9m),
                    BuildHistoricalDividend(account, holding, "MDIV", d4, 9.5m));
                await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                expectedFirstProjectedDate = DateOnly.FromDateTime(d4).AddDays(30);
                while (expectedFirstProjectedDate < today)
                {
                    expectedFirstProjectedDate = expectedFirstProjectedDate.AddDays(30);
                }
            }

            var task = CreateTask(options, "EUR");
            await task.DoWork(new Mock<ILogger>().Object);

            await using var verifyContext = new DatabaseContext(options);
            var predictedActivities = await verifyContext.Activities
                .OfType<DividendActivity>()
                .Where(x => x.IsPredicted)
                .OrderBy(x => x.Date)
                .ToListAsync(TestContext.Current.CancellationToken);

            Assert.NotEmpty(predictedActivities);
            Assert.Contains(predictedActivities, x => x.TransactionId.Contains("predicted-unannounced"));
            var firstProjected = predictedActivities.FirstOrDefault(x => DateOnly.FromDateTime(x.Date) == expectedFirstProjectedDate);
            Assert.NotNull(firstProjected);
            Assert.Equal(35m, firstProjected.Amount.Amount);
        }

        [Fact]
        public async Task DoWork_WithInsufficientHistory_DoesNotAddPredictions()
        {
            using var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var options = CreateOptions(connection);

            await using (var setupContext = new DatabaseContext(options))
            {
                await setupContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

                var account = new Account("Main") { Id = 3 };
                var holding = new Holding
                {
                    Id = 103,
                    SymbolProfiles =
                    [
                        new SymbolProfile
                        {
                            Symbol = "NOPATTERN",
                            Name = "No Pattern",
                            Currency = Currency.USD,
                            DataSource = "YAHOO",
                            AssetClass = AssetClass.Equity,
                            CountryWeight = [],
                            SectorWeights = [],
                            Identifiers = []
                        }
                    ],
                    CalculatedSnapshots =
                    [
                        new CalculatedSnapshot { HoldingId = 103, AccountId = 3, Date = DateOnly.FromDateTime(DateTime.Today.AddMonths(-6)), Quantity = 10, Currency = Currency.USD }
                    ]
                };

                setupContext.Accounts.Add(account);
                setupContext.Holdings.Add(holding);
                await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                setupContext.Activities.AddRange(
                    BuildHistoricalDividend(account, holding, "NOPATTERN", DateTime.Today.AddMonths(-4), 10m),
                    BuildHistoricalDividend(account, holding, "NOPATTERN", DateTime.Today.AddMonths(-1), 11m));
                await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var task = CreateTask(options, "USD");
            await task.DoWork(new Mock<ILogger>().Object);

            await using var verifyContext = new DatabaseContext(options);
            var predictedCount = await verifyContext.Activities
                .OfType<DividendActivity>()
                .CountAsync(x => x.IsPredicted, TestContext.Current.CancellationToken);

            Assert.Equal(0, predictedCount);
        }

        [Fact]
        public async Task DoWork_WithNoPatternButDeclaredDividend_AddsDeclaredFallbackActivity()
        {
            using var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var options = CreateOptions(connection);

            await using (var setupContext = new DatabaseContext(options))
            {
                await setupContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
                var today = DateOnly.FromDateTime(DateTime.Today);

                var account = new Account("Main") { Id = 4 };
                var symbolProfile = new SymbolProfile
                {
                    Symbol = "DECLONLY",
                    Name = "Declared Only",
                    Currency = Currency.USD,
                    DataSource = "YAHOO",
                    AssetClass = AssetClass.Equity,
                    CountryWeight = [],
                    SectorWeights = [],
                    Identifiers = [],
                    Dividends =
                    [
                        new Dividend
                        {
                            ExDividendDate = today.AddDays(20),
                            PaymentDate = today.AddDays(30),
                            DividendType = DividendType.Cash,
                            DividendState = DividendState.Declared,
                            Amount = new Money(Currency.USD, 1.5m),
                            SymbolProfileSymbol = "DECLONLY",
                            SymbolProfileDataSource = "YAHOO"
                        }
                    ]
                };

                var holding = new Holding
                {
                    Id = 104,
                    SymbolProfiles = [symbolProfile],
                    CalculatedSnapshots =
                    [
                        new CalculatedSnapshot { HoldingId = 104, AccountId = 4, Date = today.AddDays(-5), Quantity = 10, Currency = Currency.USD }
                    ]
                };

                setupContext.Accounts.Add(account);
                setupContext.Holdings.Add(holding);
                await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                setupContext.Activities.AddRange(
                    BuildHistoricalDividend(account, holding, "DECLONLY", DateTime.Today.AddMonths(-4), 10m),
                    BuildHistoricalDividend(account, holding, "DECLONLY", DateTime.Today.AddMonths(-1), 11m));
                await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var task = CreateTask(options, "USD");
            await task.DoWork(new Mock<ILogger>().Object);

            await using var verifyContext = new DatabaseContext(options);
            var fallback = await verifyContext.Activities
                .OfType<DividendActivity>()
                .FirstOrDefaultAsync(x => x.IsPredicted && x.TransactionId.Contains("declared-fallback"), TestContext.Current.CancellationToken);

            Assert.NotNull(fallback);
            Assert.Equal(15m, fallback.Amount.Amount);
            Assert.Contains("Declared dividend", fallback.Description);
        }

        private static UpcomingDividendsActivitiesTask CreateTask(DbContextOptions<DatabaseContext> options, string primaryCurrencySymbol)
        {
            var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
            dbFactoryMock.Setup(x => x.CreateDbContextAsync(default)).ReturnsAsync(() => new DatabaseContext(options));

            var currencyExchangeMock = new Mock<GhostfolioSidekick.Database.Repository.ICurrencyExchange>();
            currencyExchangeMock
                .Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
                .ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

            var settings = new Settings
            {
                PrimaryCurrency = primaryCurrencySymbol,
                DataProviderPreference = "YAHOO;COINGECKO"
            };
            var configInstance = new ConfigurationInstance { Settings = settings };
            var appSettingsMock = new Mock<IApplicationSettings>();
            appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configInstance);

            return new UpcomingDividendsActivitiesTask(dbFactoryMock.Object, currencyExchangeMock.Object, appSettingsMock.Object);
        }

        private static DividendActivity BuildHistoricalDividend(Account account, Holding holding, string symbol, DateTime date, decimal amount)
        {
            var identifier = PartialSymbolIdentifier.CreateGeneric(IdentifierType.Ticker, symbol, null);
            return new DividendActivity
            {
                Account = account,
                Holding = holding,
                Date = date,
                Amount = new Money(Currency.USD, amount),
                TransactionId = Guid.NewGuid().ToString(),
                PartialSymbolIdentifiers = identifier == null ? [] : [identifier],
                IsPredicted = false
            };
        }
    }
}
