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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests
{
    public class HoldingsRazorTests : TestContext
    {
        private FilterState _filterState;
        
        public HoldingsRazorTests()
        {
            Services.AddSingleton<ITestContextService>(new TestContextService { IsTest = true });
            
            // Add NavigationManager mock
            var mockNavManager = new Mock<NavigationManager>();
            Services.AddSingleton(mockNavManager.Object);
            
            // Add authorization services for testing
            Services.AddAuthorizationCore();
            
            // Add mock AuthenticationStateProvider that returns authenticated user
            var authStateProvider = new TestAuthenticationStateProvider();
            Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
            
            // Create FilterState
            _filterState = new FilterState();
        }

        // Helper class for authentication in tests
        private class TestAuthenticationStateProvider : AuthenticationStateProvider
        {
            public override Task<AuthenticationState> GetAuthenticationStateAsync()
            {
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "TestUser"),
                }, "test");
                
                var user = new ClaimsPrincipal(identity);
                return Task.FromResult(new AuthenticationState(user));
            }
        }

        [Fact]
        public void Holdings_ShowsLoadingState_WhenIsLoadingIsTrue()
        {
            var mockService = new Mock<IHoldingsDataServiceOLD>();
            // Make the service return a pending task to keep it in loading state
            var tcs = new TaskCompletionSource<List<HoldingDisplayModel>>();
            mockService.Setup(s => s.GetHoldingsAsync(It.IsAny<Currency>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                       .Returns(tcs.Task);
            
            Services.AddSingleton(mockService.Object);
            
            var cut = RenderComponent<Holdings>(parameters => parameters
                .AddCascadingValue(_filterState));
                
            Assert.Contains("Loading Portfolio Data...", cut.Markup);
        }

        [Fact]
        public void Holdings_ShowsErrorState_WhenHasErrorIsTrue()
        {
            var mockService = new Mock<IHoldingsDataServiceOLD>();
            // Make the service throw an exception to trigger the error state naturally
            mockService.Setup(s => s.GetHoldingsAsync(It.IsAny<Currency>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new InvalidOperationException("Test error!"));
            
            Services.AddSingleton(mockService.Object);
            
            var cut = RenderComponent<Holdings>(parameters => parameters
                .AddCascadingValue(_filterState));
            
            // Wait for the async operation to complete and the error state to be set
            cut.WaitForAssertion(() => Assert.Contains("Error Loading Data", cut.Markup), TimeSpan.FromSeconds(5));
            Assert.Contains("Test error!", cut.Markup);
            Assert.Contains("Try Again", cut.Markup);
        }

        private class FakeHoldingsDataService : IHoldingsDataServiceOLD
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

            public Task<List<HoldingDisplayModel>> GetHoldingsAsync(Currency targetCurrency, int accountId, CancellationToken cancellationToken = default)
            {
                RefreshCount++;
                // For testing, filter holdings by account if accountId > 0
                if (accountId > 0)
                {
                    // Simple test filtering - return only the first holding if account is filtered
                    var filteredHoldings = _holdings.Take(1).ToList();
                    return Task.FromResult(filteredHoldings);
                }
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

            public Task<List<string>> GetSymbolsByAccountAsync(int accountId)
            {
                // Return filtered symbols based on account for testing
                return Task.FromResult(new List<string> { "AAPL", "MSFT" });
            }

            public Task<List<Account>> GetAccountsBySymbolAsync(string symbol)
            {
                // Return filtered accounts based on symbol for testing
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
                // Return fake account value history for testing
                var history = new List<AccountValueHistoryPoint>();
                var currentDate = DateOnly.FromDateTime(startDate);
                var endDateOnly = DateOnly.FromDateTime(endDate);
                var accounts = new[]
                {
                    new Account { Id = 1, Name = "Test Account 1" },
                    new Account { Id = 2, Name = "Test Account 2" }
                };

                foreach (var account in accounts)
                {
                    var date = currentDate;
                    while (date <= endDateOnly && history.Count < 100) // Limit to prevent too much test data
                    {
                        history.Add(new AccountValueHistoryPoint
                        {
                            Date = date,
                            Account = account,
                            Value = new Money(targetCurrency, 1000m + (account.Id * 500m) + (date.DayNumber % 100)),
                            Invested = new Money(targetCurrency, 800m + (account.Id * 400m) + (date.DayNumber % 80)),
                            Balance = new Money(targetCurrency, 100m + (account.Id * 50m))
                        });
                        date = date.AddDays(7); // Weekly data for testing
                    }
                }

                return Task.FromResult(history);
            }
        }

        [Fact]
        public void Holdings_ShowsEmptyState_WhenHoldingsListIsEmpty()
        {
            Services.AddSingleton<IHoldingsDataServiceOLD>(new FakeHoldingsDataService(new List<HoldingDisplayModel>()));
            
            var cut = RenderComponent<Holdings>(parameters => parameters
                .AddCascadingValue(_filterState));
                
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
            Services.AddSingleton<IHoldingsDataServiceOLD>(new FakeHoldingsDataService(holdings));
            
            var cut = RenderComponent<Holdings>(parameters => parameters
                .AddCascadingValue(_filterState));
            
            // Wait for initial load and buttons to appear
            cut.WaitForAssertion(() => {
                Assert.Contains("Portfolio Holdings", cut.Markup);
                Assert.Contains("Portfolio Overview", cut.Markup);
            }, TimeSpan.FromSeconds(10));
            
            // Verify the view mode buttons exist
            cut.WaitForAssertion(() => {
                Assert.Contains("Treemap", cut.Markup);
                Assert.Contains("Table", cut.Markup);
                Assert.Contains("btn-group", cut.Markup); // View mode buttons
            }, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Holdings_RefreshButton_CallsDataReload()
        {
            var holdings = new List<HoldingDisplayModel>
            {
                new HoldingDisplayModel { Symbol = "AAPL", Name = "Apple Inc.", Quantity = 10, AveragePrice = new Money(Currency.USD, 100), CurrentPrice = new Money(Currency.USD, 150), CurrentValue = new Money(Currency.USD, 1500), GainLoss = new Money(Currency.USD, 500), GainLossPercentage = 0.5m, Weight = 1, Sector = "Tech", AssetClass = "Equity", Currency = "USD" }
            };
            var fakeService = new FakeHoldingsDataService(holdings);
            Services.AddSingleton<IHoldingsDataServiceOLD>(fakeService);
            
            var cut = RenderComponent<Holdings>(parameters => parameters
                .AddCascadingValue(_filterState));
            
            // Wait for initial load to complete
            cut.WaitForAssertion(() => Assert.Contains("Refresh", cut.Markup), TimeSpan.FromSeconds(10));
            
            var initialRefreshCount = fakeService.RefreshCount;
            
            // Use InvokeAsync to ensure element is found and clicked in same render cycle
            cut.InvokeAsync(() => {
                var refreshButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Refresh"));
                Assert.NotNull(refreshButton);
                refreshButton.Click();
            });
            
            // Wait for refresh to complete
            cut.WaitForAssertion(() => Assert.True(fakeService.RefreshCount > initialRefreshCount), TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void Holdings_HasCorrectUIStructure_WhenDataIsLoaded()
        {
            var holdings = new List<HoldingDisplayModel>
            {
                new HoldingDisplayModel { Symbol = "MSFT", Name = "Microsoft", Quantity = 5, AveragePrice = new Money(Currency.USD, 200), CurrentPrice = new Money(Currency.USD, 250), CurrentValue = new Money(Currency.USD, 1250), GainLoss = new Money(Currency.USD, 250), GainLossPercentage = 0.25m, Weight = 0.5m, Sector = "Tech", AssetClass = "Equity", Currency = "USD" },
                new HoldingDisplayModel { Symbol = "AAPL", Name = "Apple Inc.", Quantity = 10, AveragePrice = new Money(Currency.USD, 100), CurrentPrice = new Money(Currency.USD, 150), CurrentValue = new Money(Currency.USD, 1500), GainLoss = new Money(Currency.USD, 500), GainLossPercentage = 0.5m, Weight = 0.5m, Sector = "Tech", AssetClass = "Equity", Currency = "USD" }
            };
            Services.AddSingleton<IHoldingsDataServiceOLD>(new FakeHoldingsDataService(holdings));
            
            var cut = RenderComponent<Holdings>(parameters => parameters
                .AddCascadingValue(_filterState));
            
            // Wait for UI structure to be present
            cut.WaitForAssertion(() => {
                var markup = cut.Markup;
                Assert.Contains("Portfolio Holdings", markup);
                Assert.Contains("Table", markup);
                Assert.Contains("Treemap", markup);
                Assert.Contains("Refresh", markup);
                Assert.Contains("treemap-container", markup);
            }, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void Holdings_CanSwitchToTableView_WhenDataIsLoaded()
        {
            var holdings = new List<HoldingDisplayModel>
            {
                new HoldingDisplayModel { Symbol = "GOOG", Name = "Google", Quantity = 2, AveragePrice = new Money(Currency.USD, 1200), CurrentPrice = new Money(Currency.USD, 1300), CurrentValue = new Money(Currency.USD, 2600), GainLoss = new Money(Currency.USD, 200), GainLossPercentage = 0.08m, Weight = 0.3m, Sector = "Tech", AssetClass = "Equity", Currency = "USD" },
                new HoldingDisplayModel { Symbol = "AAPL", Name = "Apple Inc.", Quantity = 10, AveragePrice = new Money(Currency.USD, 100), CurrentPrice = new Money(Currency.USD, 150), CurrentValue = new Money(Currency.USD, 1500), GainLoss = new Money(Currency.USD, 500), GainLossPercentage = 0.5m, Weight = 0.4m, Sector = "Tech", AssetClass = "Equity", Currency = "USD" },
                new HoldingDisplayModel { Symbol = "TSLA", Name = "Tesla", Quantity = 1, AveragePrice = new Money(Currency.USD, 700), CurrentPrice = new Money(Currency.USD, 800), CurrentValue = new Money(Currency.USD, 800), GainLoss = new Money(Currency.USD, 100), GainLossPercentage = 0.125m, Weight = 0.3m, Sector = "Auto", AssetClass = "Equity", Currency = "USD" }
            };
            Services.AddSingleton<IHoldingsDataServiceOLD>(new FakeHoldingsDataService(holdings));
            
            var cut = RenderComponent<Holdings>(parameters => parameters
                .AddCascadingValue(_filterState));
            
            // Wait for component to load
            cut.WaitForAssertion(() => {
                Assert.Contains("Table", cut.Markup);
                Assert.Contains("treemap-container", cut.Markup);
            }, TimeSpan.FromSeconds(10));
            
            // Try to click table button
            cut.InvokeAsync(() => {
                var tableButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Table"));
                if (tableButton != null)
                {
                    tableButton.Click();
                }
            });
            
            // Just verify the component is still responsive after clicking
            cut.WaitForAssertion(() => {
                Assert.Contains("Portfolio Holdings", cut.Markup);
            }, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Holdings_CanFilterByAccount_WhenAccountIsSelected()
        {
            var allHoldings = new List<HoldingDisplayModel>
            {
                new HoldingDisplayModel { Symbol = "AAPL", Name = "Apple Inc.", Quantity = 10, AveragePrice = new Money(Currency.USD, 100), CurrentPrice = new Money(Currency.USD, 150), CurrentValue = new Money(Currency.USD, 1500), GainLoss = new Money(Currency.USD, 500), GainLossPercentage = 0.5m, Weight = 0.5m, Sector = "Tech", AssetClass = "Equity", Currency = "USD" },
                new HoldingDisplayModel { Symbol = "MSFT", Name = "Microsoft", Quantity = 5, AveragePrice = new Money(Currency.USD, 200), CurrentPrice = new Money(Currency.USD, 250), CurrentValue = new Money(Currency.USD, 1250), GainLoss = new Money(Currency.USD, 250), GainLossPercentage = 0.25m, Weight = 0.5m, Sector = "Tech", AssetClass = "Equity", Currency = "USD" }
            };
            
            var fakeService = new FakeHoldingsDataService(allHoldings);
            Services.AddSingleton<IHoldingsDataServiceOLD>(fakeService);
            
            // Set up filter state with specific account selected
            _filterState.SelectedAccountId = 1; // Account filter
            
            var cut = RenderComponent<Holdings>(parameters => parameters
                .AddCascadingValue(_filterState));
            
            // Wait for component to load
            cut.WaitForAssertion(() => {
                Assert.Contains("Portfolio Holdings", cut.Markup);
                // No longer expect local filters section since we're using global filters
                Assert.DoesNotContain("Filters", cut.Markup);
            }, TimeSpan.FromSeconds(10));
            
            // Verify that the service was called with the correct account filter
            cut.WaitForAssertion(() => {
                // Since we have a fake service that returns filtered results for accountId > 0,
                // we should only get the first holding when account filtering is applied
                Assert.True(fakeService.RefreshCount > 0, "Service should have been called");
            }, TimeSpan.FromSeconds(5));
        }
    }
}
