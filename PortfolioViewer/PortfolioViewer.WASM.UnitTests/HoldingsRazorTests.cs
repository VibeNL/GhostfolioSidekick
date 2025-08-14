using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using GhostfolioSidekick.PortfolioViewer.WASM.Pages;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests
{
    public class HoldingsRazorTests : TestContext
    {
        public HoldingsRazorTests()
        {
            Services.AddSingleton<ITestContextService>(new TestContextService { IsTest = true });
        }

        [Fact]
        public void Holdings_ShowsLoadingState_WhenIsLoadingIsTrue()
        {
            var mockService = new Mock<IHoldingsDataService>();
            Services.AddSingleton(mockService.Object);
            var cut = RenderComponent<Holdings>();
            Assert.Contains("Loading Portfolio Data...", cut.Markup);
        }

        [Fact]
        public void Holdings_ShowsErrorState_WhenHasErrorIsTrue()
        {
            var mockService = new Mock<IHoldingsDataService>();
            // Make the service throw an exception to trigger the error state naturally
            mockService.Setup(s => s.GetHoldingsAsync(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new InvalidOperationException("Test error!"));
            
            Services.AddSingleton(mockService.Object);
            var cut = RenderComponent<Holdings>();
            
            // Wait for the async operation to complete and the error state to be set
            cut.WaitForAssertion(() => Assert.Contains("Error Loading Data", cut.Markup), TimeSpan.FromSeconds(5));
            Assert.Contains("Test error!", cut.Markup);
            Assert.Contains("Try Again", cut.Markup);
        }

        private class FakeHoldingsDataService : IHoldingsDataService
        {
            private List<HoldingDisplayModel> _holdings;
            public int RefreshCount { get; private set; } = 0;
            public FakeHoldingsDataService(List<HoldingDisplayModel> holdings) => _holdings = holdings;
            public void SetHoldings(List<HoldingDisplayModel> holdings) => _holdings = holdings;
            public Task<List<HoldingDisplayModel>> GetHoldingsAsync(Currency targetCurrency, CancellationToken cancellationToken = default)
            {
                RefreshCount++;
                return Task.FromResult(_holdings);
            }

			public async Task<DateOnly> GetMinDateAsync(CancellationToken cancellationToken = default)
			{
				// For testing, return a fixed date
				await Task.Delay(10); // Simulate async delay
				return DateOnly.FromDateTime(new DateTime(2020, 1, 1));
			}

            // Updated stub implementation to satisfy IHoldingsDataService
            public Task<List<PortfolioValueHistoryPoint>> GetPortfolioValueHistoryAsync(
                Currency targetCurrency,
                DateTime startDate,
                DateTime endDate,
                int accountId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new List<PortfolioValueHistoryPoint>());
            }

            public Task<List<Account>> GetAccountsAsync()
            {
                return Task.FromResult(new List<Account>());
            }

            public Task<List<HoldingPriceHistoryPoint>> GetHoldingPriceHistoryAsync(
                string symbol,
                DateTime startDate,
                DateTime endDate,
                CancellationToken cancellationToken = default)
            {
                // Return fake price history data for testing
                var priceHistory = new List<HoldingPriceHistoryPoint>();
                var currentDate = DateOnly.FromDateTime(startDate);
                var endDateOnly = DateOnly.FromDateTime(endDate);
                var basePrice = 100m;

                while (currentDate <= endDateOnly)
                {
                    priceHistory.Add(new HoldingPriceHistoryPoint
                    {
                        Date = currentDate,
                        Price = new Money(Currency.USD, basePrice + (decimal)(Math.Sin(currentDate.DayNumber * 0.1) * 10))
                    });
                    currentDate = currentDate.AddDays(1);
                }

                return Task.FromResult(priceHistory);
            }

            public Task<List<TransactionDisplayModel>> GetTransactionsAsync(
                Currency targetCurrency,
                DateTime startDate,
                DateTime endDate,
                int accountId,
                string symbol,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new List<TransactionDisplayModel>());
            }

            public Task<List<string>> GetSymbolsAsync()
            {
                return Task.FromResult(new List<string> { "AAPL", "MSFT", "GOOGL" });
            }
        }

        [Fact]
        public void Holdings_ShowsEmptyState_WhenHoldingsListIsEmpty()
        {
            Services.AddSingleton<IHoldingsDataService>(new FakeHoldingsDataService(new List<HoldingDisplayModel>()));
            var cut = RenderComponent<Holdings>();
            cut.WaitForAssertion(() => Assert.Contains("No Holdings Found", cut.Markup));
        }

        [Fact]
        public void Holdings_RendersTable_WhenHoldingsExistAndViewModeIsTable()
        {
            var holdings = new List<HoldingDisplayModel>
            {
                new HoldingDisplayModel {
                    Symbol = "AAPL", Name = "Apple Inc.", Quantity = 10, AveragePrice = new Money(Currency.USD, 100), CurrentPrice = new Money(Currency.USD, 150), CurrentValue = new Money(Currency.USD, 1500), GainLoss = new Money(Currency.USD, 500), GainLossPercentage = 0.5m, Weight = 1, Sector = "Tech", AssetClass = "Equity", Currency = "USD"
                }
            };
            Services.AddSingleton<IHoldingsDataService>(new FakeHoldingsDataService(holdings));
            var cut = RenderComponent<Holdings>();
            cut.WaitForAssertion(() => Assert.Contains("Table", cut.Markup));
            var tableButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Table"));
            if (tableButton == null)
            {
                throw new Xunit.Sdk.XunitException($"Table button not found. Markup: {cut.Markup}");
            }
            tableButton.Click();
            cut.WaitForAssertion(() => Assert.Contains("Apple Inc.", cut.Markup));
            Assert.Contains("AAPL", cut.Markup);
            Assert.Contains("Current Value", cut.Markup);
        }

        [Fact]
        public void Holdings_RendersTreemap_WhenViewModeIsTreemap()
        {
            var holdings = new List<HoldingDisplayModel>
            {
                new HoldingDisplayModel {
                    Symbol = "AAPL", Name = "Apple Inc.", Quantity = 10, AveragePrice = new Money(Currency.USD, 100), CurrentPrice = new Money(Currency.USD, 150), CurrentValue = new Money(Currency.USD, 1500), GainLoss = new Money(Currency.USD, 500), GainLossPercentage = 0.5m, Weight = 1, Sector = "Tech", AssetClass = "Equity", Currency = "USD"
                }
            };
            Services.AddSingleton<IHoldingsDataService>(new FakeHoldingsDataService(holdings));
            var cut = RenderComponent<Holdings>();
            cut.WaitForAssertion(() =>
            {
                if (!cut.Markup.Contains("treemap-container"))
                {
                    throw new Xunit.Sdk.XunitException($"treemap-container not found. Markup: {cut.Markup}");
                }
            });
        }

        [Fact]
        public void Holdings_RefreshButton_CallsDataReload()
        {
            var holdings = new List<HoldingDisplayModel>
            {
                new HoldingDisplayModel { Symbol = "AAPL", Name = "Apple Inc.", Quantity = 10, AveragePrice = new Money(Currency.USD, 100), CurrentPrice = new Money(Currency.USD, 150), CurrentValue = new Money(Currency.USD, 1500), GainLoss = new Money(Currency.USD, 500), GainLossPercentage = 0.5m, Weight = 1, Sector = "Tech", AssetClass = "Equity", Currency = "USD" }
            };
            var fakeService = new FakeHoldingsDataService(holdings);
            Services.AddSingleton<IHoldingsDataService>(fakeService);
            var cut = RenderComponent<Holdings>();
            cut.WaitForAssertion(() => Assert.Contains("Refresh", cut.Markup));
            var refreshButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Refresh"));
            if (refreshButton == null)
            {
                throw new Xunit.Sdk.XunitException($"Refresh button not found. Markup: {cut.Markup}");
            }
            refreshButton.Click();
            cut.WaitForAssertion(() => Assert.True(fakeService.RefreshCount > 1));
        }

        [Fact]
        public void Holdings_Sorting_WorksOnSymbolColumn()
        {
            var holdings = new List<HoldingDisplayModel>
            {
                new HoldingDisplayModel { Symbol = "MSFT", Name = "Microsoft", Quantity = 5, AveragePrice = new Money(Currency.USD, 200), CurrentPrice = new Money(Currency.USD, 250), CurrentValue = new Money(Currency.USD, 1250), GainLoss = new Money(Currency.USD, 250), GainLossPercentage = 0.25m, Weight = 0.5m, Sector = "Tech", AssetClass = "Equity", Currency = "USD" },
                new HoldingDisplayModel { Symbol = "AAPL", Name = "Apple Inc.", Quantity = 10, AveragePrice = new Money(Currency.USD, 100), CurrentPrice = new Money(Currency.USD, 150), CurrentValue = new Money(Currency.USD, 1500), GainLoss = new Money(Currency.USD, 500), GainLossPercentage = 0.5m, Weight = 0.5m, Sector = "Tech", AssetClass = "Equity", Currency = "USD" }
            };
            Services.AddSingleton<IHoldingsDataService>(new FakeHoldingsDataService(holdings));
            var cut = RenderComponent<Holdings>();
            cut.WaitForAssertion(() => Assert.Contains("Table", cut.Markup));
            var tableButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Table"));
            if (tableButton == null)
            {
                throw new Xunit.Sdk.XunitException($"Table button not found. Markup: {cut.Markup}");
            }
            tableButton.Click();
            cut.WaitForAssertion(() => Assert.Contains("Symbol", cut.Markup));
            var symbolHeader = cut.FindAll("th button").FirstOrDefault(b => b.TextContent.Contains("Symbol"));
            if (symbolHeader == null)
            {
                throw new Xunit.Sdk.XunitException($"Symbol column header not found. Markup: {cut.Markup}");
            }
            // Try single click first
            symbolHeader.Click();
            cut.WaitForAssertion(() =>
            {
                var rows = cut.FindAll("tbody tr");
                var rowHtml = string.Join("\n---\n", rows.Select(r => r.InnerHtml));
                if (!rows[0].InnerHtml.Contains("AAPL") || !rows[1].InnerHtml.Contains("MSFT"))
                {
                    // Try double click (descending)
                    symbolHeader.Click();
                    rows = cut.FindAll("tbody tr");
                    rowHtml = string.Join("\n---\n", rows.Select(r => r.InnerHtml));
                    if (!rows[0].InnerHtml.Contains("AAPL") || !rows[1].InnerHtml.Contains("MSFT"))
                    {
                        throw new Xunit.Sdk.XunitException($"Sorting failed. Table rows after sort:\n{rowHtml}");
                    }
                }
            });
        }

        [Fact]
        public void Holdings_RendersMultipleFakeHoldings_AndSortsByValue()
        {
            var holdings = new List<HoldingDisplayModel>
            {
                new HoldingDisplayModel { Symbol = "GOOG", Name = "Google", Quantity = 2, AveragePrice = new Money(Currency.USD, 1200), CurrentPrice = new Money(Currency.USD, 1300), CurrentValue = new Money(Currency.USD, 2600), GainLoss = new Money(Currency.USD, 200), GainLossPercentage = 0.08m, Weight = 0.3m, Sector = "Tech", AssetClass = "Equity", Currency = "USD" },
                new HoldingDisplayModel { Symbol = "AAPL", Name = "Apple Inc.", Quantity = 10, AveragePrice = new Money(Currency.USD, 100), CurrentPrice = new Money(Currency.USD, 150), CurrentValue = new Money(Currency.USD, 1500), GainLoss = new Money(Currency.USD, 500), GainLossPercentage = 0.5m, Weight = 0.4m, Sector = "Tech", AssetClass = "Equity", Currency = "USD" },
                new HoldingDisplayModel { Symbol = "TSLA", Name = "Tesla", Quantity = 1, AveragePrice = new Money(Currency.USD, 700), CurrentPrice = new Money(Currency.USD, 800), CurrentValue = new Money(Currency.USD, 800), GainLoss = new Money(Currency.USD, 100), GainLossPercentage = 0.125m, Weight = 0.3m, Sector = "Auto", AssetClass = "Equity", Currency = "USD" }
            };
            Services.AddSingleton<IHoldingsDataService>(new FakeHoldingsDataService(holdings));
            var cut = RenderComponent<Holdings>();
            cut.WaitForAssertion(() => Assert.Contains("Table", cut.Markup));
            var tableButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Table"));
            if (tableButton == null)
            {
                throw new Xunit.Sdk.XunitException($"Table button not found. Markup: {cut.Markup}");
            }
            tableButton.Click();
            cut.WaitForAssertion(() => Assert.Contains("Current Value", cut.Markup));
            var valueHeader = cut.FindAll("th button").FirstOrDefault(b => b.TextContent.Contains("Current Value"));
            if (valueHeader == null)
            {
                throw new Xunit.Sdk.XunitException($"Current Value column header not found. Markup: {cut.Markup}");
            }
            // Click twice for descending order
            valueHeader.Click();
            valueHeader.Click();
            cut.WaitForAssertion(() =>
            {
                var rows = cut.FindAll("tbody tr");
                var rowHtml = string.Join("\n---\n", rows.Select(r => r.InnerHtml));
                if (!rows[0].InnerHtml.Contains("GOOG") || !rows[1].InnerHtml.Contains("AAPL") || !rows[2].InnerHtml.Contains("TSLA"))
                {
                    throw new Xunit.Sdk.XunitException($"Sorting by value failed. Table rows after sort:\n{rowHtml}");
                }
            });
        }
    }
}
