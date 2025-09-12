using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using GhostfolioSidekick.PortfolioViewer.WASM.Pages;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;

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
            var priceHistory = new List<HoldingPriceHistoryPoint>();
            var fakeService = new FakeHoldingDetailDataService(priceHistory);
            Services.AddSingleton<IHoldingsDataService>(fakeService);
            Services.AddSingleton<NavigationManager>(new MockNavigationManager());
            
            var cut = RenderComponent<HoldingDetail>(parameters => parameters.Add(p => p.Symbol, "NOTFOUND"));
            
            cut.WaitForAssertion(() => Assert.Contains("Holding Not Found", cut.Markup));
        }

        [Fact]
        public void HoldingDetail_ShowsHoldingInfo_WhenHoldingExists()
        {
            var holding = new HoldingDisplayModel 
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
            };
            var priceHistory = new List<HoldingPriceHistoryPoint>();
            var fakeService = new FakeHoldingDetailDataService(priceHistory, holding);
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
            var holding = new HoldingDisplayModel 
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
            };
            var priceHistory = new List<HoldingPriceHistoryPoint>
            {
                new HoldingPriceHistoryPoint
                {
                    Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                    Price = new Money(Currency.USD, 150)
                }
            };
            var fakeService = new FakeHoldingDetailDataService(priceHistory, holding);
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
            private readonly List<HoldingPriceHistoryPoint> _priceHistory;
            private readonly HoldingDisplayModel? _holding;

            public FakeHoldingDetailDataService(List<HoldingPriceHistoryPoint> priceHistory, HoldingDisplayModel? holding = null)
            {
                _priceHistory = priceHistory;
                _holding = holding;
            }

            public Task<List<HoldingDisplayModel>> GetHoldingsAsync(Currency targetCurrency, CancellationToken cancellationToken = default)
            {
                var result = _holding != null ? new List<HoldingDisplayModel> { _holding } : new List<HoldingDisplayModel>();
                return Task.FromResult(result);
            }

            public Task<List<HoldingDisplayModel>> GetHoldingsAsync(Currency targetCurrency, int accountId, CancellationToken cancellationToken = default)
            {
                var result = _holding != null ? new List<HoldingDisplayModel> { _holding } : new List<HoldingDisplayModel>();
                return Task.FromResult(result);
            }

            public Task<DateOnly> GetMinDateAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(DateOnly.FromDateTime(new DateTime(2020, 1, 1)));
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
                return Task.FromResult(_priceHistory);
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

            public Task<List<string>> GetSymbolsByAccountAsync(int accountId)
            {
                return Task.FromResult(new List<string> { "AAPL", "MSFT" });
            }

            public Task<List<Account>> GetAccountsBySymbolAsync(string symbol)
            {
                return Task.FromResult(new List<Account>
                {
                    new Account { Id = 1, Name = "Test Account 1" },
                    new Account { Id = 2, Name = "Test Account 2" }
                });
            }

            public Task<List<AccountValueHistoryPoint>> GetAccountValueHistoryAsync(
                Currency targetCurrency,
                DateTime startDate,
                DateTime endDate,
                CancellationToken cancellationToken = default)
            {
                // Return empty list for testing
                return Task.FromResult(new List<AccountValueHistoryPoint>());
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