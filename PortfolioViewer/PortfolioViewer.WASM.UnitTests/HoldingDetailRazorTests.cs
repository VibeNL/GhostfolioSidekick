using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using GhostfolioSidekick.PortfolioViewer.WASM.Pages;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.Model;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests
{
    public class HoldingDetailRazorTests : TestContext
    {
        public HoldingDetailRazorTests()
        {
            Services.AddSingleton<ITestContextService>(new TestContextService { IsTest = true });
        }

        [Fact]
        public void HoldingDetail_ShowsLoadingState_WhenIsLoadingIsTrue()
        {
            var mockService = new Mock<IHoldingsDataService>();
            Services.AddSingleton(mockService.Object);
            Services.AddSingleton<NavigationManager>(new MockNavigationManager());
            
            var cut = RenderComponent<HoldingDetail>(parameters => parameters.Add(p => p.Symbol, "AAPL"));
            
            Assert.Contains("Loading Holding Data...", cut.Markup);
        }

        [Fact]
        public void HoldingDetail_ShowsNotFound_WhenHoldingDoesNotExist()
        {
            var holdings = new List<HoldingDisplayModel>();
            var fakeService = new FakeHoldingDetailDataService(holdings);
            Services.AddSingleton<IHoldingsDataService>(fakeService);
            Services.AddSingleton<NavigationManager>(new MockNavigationManager());
            
            var cut = RenderComponent<HoldingDetail>(parameters => parameters.Add(p => p.Symbol, "NOTFOUND"));
            
            cut.WaitForAssertion(() => Assert.Contains("Holding Not Found", cut.Markup));
        }

        [Fact]
        public void HoldingDetail_ShowsHoldingInfo_WhenHoldingExists()
        {
            var holdings = new List<HoldingDisplayModel>
            {
                new HoldingDisplayModel 
                { 
                    Symbol = "AAPL", 
                    Name = "Apple Inc.", 
                    Quantity = 10, 
                    AveragePrice = new Money(Currency.USD, 100), 
                    CurrentPrice = new Money(Currency.USD, 150), 
                    CurrentValue = new Money(Currency.USD, 1500), 
                    GainLoss = new Money(Currency.USD, 500), 
                    GainLossPercentage = 0.5m, 
                    Weight = 1, 
                    Sector = "Tech", 
                    AssetClass = "Equity", 
                    Currency = "USD"
                }
            };
            var fakeService = new FakeHoldingDetailDataService(holdings);
            Services.AddSingleton<IHoldingsDataService>(fakeService);
            Services.AddSingleton<NavigationManager>(new MockNavigationManager());
            
            var cut = RenderComponent<HoldingDetail>(parameters => parameters.Add(p => p.Symbol, "AAPL"));
            
            cut.WaitForAssertion(() => 
            {
                Assert.Contains("AAPL", cut.Markup);
                Assert.Contains("Apple Inc.", cut.Markup);
                Assert.Contains("Current Position", cut.Markup);
                Assert.Contains("Price History", cut.Markup);
            });
        }

        [Fact]
        public void HoldingDetail_ShowsPriceHistory_WhenAvailable()
        {
            var holdings = new List<HoldingDisplayModel>
            {
                new HoldingDisplayModel 
                { 
                    Symbol = "AAPL", 
                    Name = "Apple Inc.", 
                    Quantity = 10, 
                    AveragePrice = new Money(Currency.USD, 100), 
                    CurrentPrice = new Money(Currency.USD, 150), 
                    CurrentValue = new Money(Currency.USD, 1500), 
                    GainLoss = new Money(Currency.USD, 500), 
                    GainLossPercentage = 0.5m, 
                    Weight = 1, 
                    Sector = "Tech", 
                    AssetClass = "Equity", 
                    Currency = "USD"
                }
            };
            var fakeService = new FakeHoldingDetailDataService(holdings);
            Services.AddSingleton<IHoldingsDataService>(fakeService);
            Services.AddSingleton<NavigationManager>(new MockNavigationManager());
            
            var cut = RenderComponent<HoldingDetail>(parameters => parameters.Add(p => p.Symbol, "AAPL"));
            
            cut.WaitForAssertion(() => 
            {
                Assert.Contains("Recent Price Data", cut.Markup);
                // Should show some price history data from the fake service
                Assert.Contains("$", cut.Markup); // Price data contains currency symbol
            });
        }

        private class FakeHoldingDetailDataService : IHoldingsDataService
        {
            private List<HoldingDisplayModel> _holdings;

            public FakeHoldingDetailDataService(List<HoldingDisplayModel> holdings) => _holdings = holdings;

            public Task<List<HoldingDisplayModel>> GetHoldingsAsync(Currency targetCurrency, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_holdings);
            }

            public async Task<DateOnly> GetMinDateAsync(CancellationToken cancellationToken = default)
            {
                await Task.Delay(10);
                return DateOnly.FromDateTime(new DateTime(2020, 1, 1));
            }

            public Task<List<PortfolioValueHistoryPoint>> GetPortfolioValueHistoryAsync(
                Currency targetCurrency,
                DateTime startDate,
                DateTime endDate,
                int accountId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new List<PortfolioValueHistoryPoint>());
            }

            public Task<List<GhostfolioSidekick.Model.Accounts.Account>> GetAccountsAsync()
            {
                return Task.FromResult(new List<GhostfolioSidekick.Model.Accounts.Account>());
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
                var baseAveragePrice = 95m; // Slightly lower average price

                for (int i = 0; i < 5; i++) // Just 5 data points for testing
                {
                    priceHistory.Add(new HoldingPriceHistoryPoint
                    {
                        Date = currentDate.AddDays(i),
                        Price = new Money(Currency.USD, basePrice + i * 5),
                        AveragePrice = new Money(Currency.USD, baseAveragePrice + i * 2)
                    });
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

        private class MockNavigationManager : NavigationManager
        {
            public MockNavigationManager() : base()
            {
                Initialize("https://localhost/", "https://localhost/");
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                // Mock implementation - do nothing for tests
            }
        }
    }
}