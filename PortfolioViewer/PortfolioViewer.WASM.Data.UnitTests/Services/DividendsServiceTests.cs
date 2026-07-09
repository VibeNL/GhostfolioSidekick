using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Symbols;

namespace PortfolioViewer.WASM.Data.UnitTests.Services
{
    public class DividendsServiceTests
    {
        [Fact]
        public async Task GetDividendsAsync_ReturnsExpectedDividends()
        {
            // Arrange
            var timelineEntry = new UpcomingDividendTimelineEntry
            {
                Id = Guid.NewGuid(),
                HoldingId = 42,
                ExpectedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                Amount = 100.0m,
                Currency = new Currency { Symbol = "USD" },
                AmountPrimaryCurrency = 100.0m,
                DividendType = DividendType.Cash,
                DividendState = DividendState.Declared
            };

          var holding = new Holding
            {
                Id = 42,
                SymbolProfiles = [],
                CalculatedSnapshots = []
            };

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.UpcomingDividendTimelineEntries).ReturnsDbSet(new List<UpcomingDividendTimelineEntry> { timelineEntry });
            mockContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding> { holding });
            mockContext.Setup(x => x.Activities).ReturnsDbSet(new List<Activity>());

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockContext.Object);

            var mockConfigService = new Mock<IServerConfigurationService>();
            mockConfigService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.USD);

            var service = new DividendsService(mockFactory.Object, mockConfigService.Object);

            // Act
            var result = await service.GetDividendsAsync();

            // Assert
            Assert.Single(result);
            var div = result[0];
            Assert.Equal("42", div.Symbol);
            Assert.Equal(string.Empty, div.CompanyName);
            Assert.Equal(100.0m, div.Amount);
            Assert.Equal("USD", div.Currency);
            Assert.Equal(100.0m, div.AmountPrimaryCurrency);
            Assert.Equal("USD", div.PrimaryCurrency);
            Assert.True(div.PaymentDate > DateOnly.FromDateTime(DateTime.Today));
            Assert.Equal(0, div.DividendPerShare);
            Assert.Null(div.DividendPerSharePrimaryCurrency);
            Assert.Equal(0, div.Quantity);
        }

        [Fact]
        public async Task GetDividendsAsync_WithCurrencyConversion_ReturnsConvertedAmounts()
        {
            // Arrange
            var timelineEntry = new UpcomingDividendTimelineEntry
            {
                Id = Guid.NewGuid(),
                HoldingId = 99,
                ExpectedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                Amount = 200.0m,
                Currency = new Currency { Symbol = "EUR" },
                AmountPrimaryCurrency = 220.0m, // Simulate conversion (e.g., EUR->USD)
                DividendType = DividendType.Cash,
                DividendState = DividendState.Declared
            };

          var holding = new Holding
            {
                Id = 99,
                SymbolProfiles = [],
                CalculatedSnapshots = []
            };

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.UpcomingDividendTimelineEntries).ReturnsDbSet(new List<UpcomingDividendTimelineEntry> { timelineEntry });
            mockContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding> { holding });
            mockContext.Setup(x => x.Activities).ReturnsDbSet(new List<Activity>());

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockContext.Object);

            var mockConfigService = new Mock<IServerConfigurationService>();
            mockConfigService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.USD);

            var service = new DividendsService(mockFactory.Object, mockConfigService.Object);

            // Act
            var result = await service.GetDividendsAsync();

            // Assert
            Assert.Single(result);
            var div = result[0];
            Assert.Equal("99", div.Symbol);
            Assert.Equal(string.Empty, div.CompanyName);
            Assert.Equal(200.0m, div.Amount);
            Assert.Equal("EUR", div.Currency);
            Assert.Equal(220.0m, div.AmountPrimaryCurrency);
            Assert.Equal("USD", div.PrimaryCurrency);
            Assert.True(div.PaymentDate > DateOnly.FromDateTime(DateTime.Today));
            Assert.Equal(0, div.DividendPerShare);
            Assert.Null(div.DividendPerSharePrimaryCurrency);
            Assert.Equal(0, div.Quantity);
        }

        [Fact]
        public async Task GetDividendsAsync_WithDateFilter_ExcludesDividendsOutsideRange()
        {
            // Arrange
            var entryInRange = new UpcomingDividendTimelineEntry
            {
                Id = Guid.NewGuid(),
                HoldingId = 1,
                ExpectedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
                Amount = 50.0m,
                Currency = new Currency { Symbol = "USD" },
                AmountPrimaryCurrency = 50.0m,
                DividendType = DividendType.Cash,
                DividendState = DividendState.Declared
            };

            var entryOutOfRange = new UpcomingDividendTimelineEntry
            {
                Id = Guid.NewGuid(),
                HoldingId = 2,
                ExpectedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-60)),
                Amount = 75.0m,
                Currency = new Currency { Symbol = "USD" },
                AmountPrimaryCurrency = 75.0m,
                DividendType = DividendType.Cash,
                DividendState = DividendState.Declared
            };

            var holding1 = new Holding { Id = 1, SymbolProfiles = [], CalculatedSnapshots = [] };
            var holding2 = new Holding { Id = 2, SymbolProfiles = [], CalculatedSnapshots = [] };

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.UpcomingDividendTimelineEntries)
                .ReturnsDbSet(new List<UpcomingDividendTimelineEntry> { entryInRange, entryOutOfRange });
            mockContext.Setup(x => x.Holdings)
                .ReturnsDbSet(new List<Holding> { holding1, holding2 });
            mockContext.Setup(x => x.Activities).ReturnsDbSet(new List<Activity>());

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockContext.Object);

            var mockConfigService = new Mock<IServerConfigurationService>();
            mockConfigService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.USD);

            var service = new DividendsService(mockFactory.Object, mockConfigService.Object);

            var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
            var endDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

            // Act
            var result = await service.GetDividendsAsync(startDate, endDate);

            // Assert
            Assert.Single(result);
            Assert.Equal(50.0m, result[0].Amount);
        }

        [Fact]
        public async Task GetDividendsAsync_IncludesDividendActivities()
        {
            // Arrange
            var account = new Account("TestAccount") { Id = 1 };
            var holding = new Holding { Id = 10, SymbolProfiles = [], CalculatedSnapshots = [] };
            holding.SymbolProfiles.Add(new SymbolProfile("MSFT", "Microsoft", [], Currency.USD, "NASDAQ", AssetClass.Equity, null, [], []));

            var activityDate = DateTime.Today.AddDays(-5);
            var dividendActivity = new DividendActivity(
                account,
                holding,
                [PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Ticker, "MSFT", null)!],
                activityDate,
                new Money(Currency.USD, 150.0m),
                "DIV-001",
                null,
                "Dividend payment")
            {
                Id = 1,
                Quantity = 100
            };

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.UpcomingDividendTimelineEntries).ReturnsDbSet(new List<UpcomingDividendTimelineEntry>());
            mockContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding> { holding });
            mockContext.Setup(x => x.Activities).ReturnsDbSet(new List<Activity> { dividendActivity });

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockContext.Object);

            var mockConfigService = new Mock<IServerConfigurationService>();
            mockConfigService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.USD);

            var service = new DividendsService(mockFactory.Object, mockConfigService.Object);

            // Act
            var result = await service.GetDividendsAsync();

            // Assert
            Assert.Single(result);
            var div = result[0];
            Assert.Equal("MSFT", div.Symbol);
            Assert.Equal("Microsoft", div.CompanyName);
            Assert.Equal(150.0m, div.Amount);
            Assert.Equal("USD", div.Currency);
            Assert.Equal(150.0m, div.AmountPrimaryCurrency);
            Assert.Equal("USD", div.PrimaryCurrency);
            Assert.Equal(DateOnly.FromDateTime(activityDate), div.PaymentDate);
            Assert.False(div.IsPredicted);
        }

        [Fact]
        public async Task GetDividendsAsync_CombinesUpcomingAndActivityDividends()
        {
            // Arrange
            var timelineEntry = new UpcomingDividendTimelineEntry
            {
                Id = Guid.NewGuid(),
                HoldingId = 1,
                ExpectedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                Amount = 100.0m,
                Currency = new Currency { Symbol = "USD" },
                AmountPrimaryCurrency = 100.0m,
                DividendType = DividendType.Cash,
                DividendState = DividendState.Declared
            };

            var holdingForTimeline = new Holding { Id = 1, SymbolProfiles = [], CalculatedSnapshots = [] };

            var account = new Account("TestAccount") { Id = 1 };
            var holdingForActivity = new Holding { Id = 2, SymbolProfiles = [], CalculatedSnapshots = [] };
            holdingForActivity.SymbolProfiles.Add(new SymbolProfile("AAPL", "Apple Inc", [], Currency.USD, "NASDAQ", AssetClass.Equity, null, [], []));

            var dividendActivity = new DividendActivity(
                account,
                holdingForActivity,
                [PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Ticker, "AAPL", null)!],
                DateTime.Today.AddDays(-3),
                new Money(Currency.USD, 75.0m),
                "DIV-002",
                null,
                "Dividend payment")
            {
                Id = 2,
                Quantity = 50
            };

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.UpcomingDividendTimelineEntries).ReturnsDbSet(new List<UpcomingDividendTimelineEntry> { timelineEntry });
            mockContext.Setup(x => x.Holdings).ReturnsDbSet(new List<Holding> { holdingForTimeline, holdingForActivity });
            mockContext.Setup(x => x.Activities).ReturnsDbSet(new List<Activity> { dividendActivity });

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockContext.Object);

            var mockConfigService = new Mock<IServerConfigurationService>();
            mockConfigService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.USD);

            var service = new DividendsService(mockFactory.Object, mockConfigService.Object);

            // Act
            var result = await service.GetDividendsAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, d => d.Amount == 100.0m);
            Assert.Contains(result, d => d.Amount == 75.0m && d.Symbol == "AAPL");
        }
    }
}
