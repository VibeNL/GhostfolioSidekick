using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
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

            var mockSetDividends = new List<Dividend> { dividend }.AsQueryable().BuildMockDbSet();
            var mockSetSymbolProfiles = new List<SymbolProfile> { symbolProfile }.AsQueryable().BuildMockDbSet();
            var mockSetHoldings = new[]
            {
                new HoldingAggregated
                {
                    Symbol = "AAPL",
                    CalculatedSnapshotsPrimaryCurrency = new List<CalculatedSnapshotPrimaryCurrency>
                    {
                        new CalculatedSnapshotPrimaryCurrency { Date = DateOnly.FromDateTime(DateTime.Today), Quantity = 10m }
                    }
                }
            }.AsQueryable().BuildMockDbSet();

            var mockContext = new Mock<DatabaseContext>();
            mockContext.Setup(x => x.Dividends).Returns(mockSetDividends.Object);
            mockContext.Setup(x => x.SymbolProfiles).Returns(mockSetSymbolProfiles.Object);
            mockContext.Setup(x => x.HoldingAggregateds).Returns(mockSetHoldings.Object);

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

    // Helper for mocking DbSet
    public static class DbSetMockingExtensions
    {
        public static Mock<DbSet<T>> BuildMockDbSet<T>(this IQueryable<T> data) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            return mockSet;
        }
    }
}
