using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;

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
                ExpectedDate = DateOnly.FromDateTime(DateTime.Today),
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
                ExpectedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(60)),
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

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockContext.Object);

            var mockConfigService = new Mock<IServerConfigurationService>();
            mockConfigService.Setup(x => x.GetPrimaryCurrencyAsync()).ReturnsAsync(Currency.USD);

            var service = new DividendsService(mockFactory.Object, mockConfigService.Object);

            var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
            var endDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30));

            // Act
            var result = await service.GetDividendsAsync(startDate, endDate);

            // Assert
            Assert.Single(result);
            Assert.Equal(50.0m, result[0].Amount);
        }
    }
}
