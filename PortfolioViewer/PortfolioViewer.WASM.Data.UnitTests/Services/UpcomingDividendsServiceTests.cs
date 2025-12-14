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

            var holdingAggregated = new HoldingAggregated
            {
                Symbol = "AAPL",
                CalculatedSnapshotsPrimaryCurrency = new List<CalculatedSnapshotPrimaryCurrency>
                {
                    new CalculatedSnapshotPrimaryCurrency { Date = DateOnly.FromDateTime(DateTime.Today), Quantity = 10m }
                }
            };

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.Dividends).ReturnsDbSet(new List<Dividend> { dividend });
            mockContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile> { symbolProfile });
            mockContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(new List<HoldingAggregated> { holdingAggregated });

            var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(mockContext.Object);

            var service = new UpcomingDividendsService(mockFactory.Object);

            // Act
            var result = await service.GetUpcomingDividendsAsync();

            // Assert
            Assert.Single(result);
            var div = result[0];
            Assert.Equal("AAPL", div.Symbol);
            Assert.Equal("Apple Inc.", div.CompanyName);
            Assert.Equal(25.0m, div.Amount); // 2.5 * 10
            Assert.Equal("USD", div.Currency);
        }
    }
}
