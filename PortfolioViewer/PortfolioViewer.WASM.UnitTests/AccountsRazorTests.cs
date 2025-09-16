using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using GhostfolioSidekick.PortfolioViewer.WASM.Pages;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Database.Repository;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using Xunit.Abstractions;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests
{
    public class AccountsRazorTests : TestContext
    {
        private FilterState _filterState;
        private readonly ITestOutputHelper _output;
        
        public AccountsRazorTests(ITestOutputHelper output)
        {
            _output = output;
            Services.AddSingleton<ITestContextService>(new TestContextService { IsTest = true });
            
            // Add ICurrencyExchange mock that the Accounts page needs
            var mockCurrencyExchange = new Mock<ICurrencyExchange>();
            mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
                .ReturnsAsync((Money money, Currency target, DateOnly date) => new Money(target, money.Amount));
            Services.AddSingleton(mockCurrencyExchange.Object);
            
            // Configure JS Interop for Plotly charts - comprehensive mock setup
            JSInterop.SetupVoid("importScript", args => true);
            JSInterop.SetupVoid("plotly", args => true);
            
            var plotlyModule = JSInterop.SetupModule("./_content/Plotly.Blazor/plotly-interop-6.0.1.js");
            plotlyModule.SetupVoid("newPlot", args => true);
            plotlyModule.SetupVoid("react", args => true);
            plotlyModule.SetupVoid("purge", args => true);
            plotlyModule.SetupVoid("importScript", args => true);
            
            // Add authorization services for testing
            Services.AddAuthorizationCore();
            
            // Add mock AuthenticationStateProvider that returns authenticated user
            var authStateProvider = new TestAuthenticationStateProvider();
            Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
            
            // Create FilterState - ensure consistent currency
            _filterState = new FilterState();
            _filterState.SelectedCurrency = "EUR"; // Match our test data
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

        private class FakeAccountsDataService : IHoldingsDataServiceOLD
        {
            private readonly List<AccountValueHistoryPoint> _accountHistory;

            public FakeAccountsDataService(List<AccountValueHistoryPoint> accountHistory)
            {
                _accountHistory = accountHistory;
            }

            public Task<List<HoldingDisplayModel>> GetHoldingsAsync(Currency targetCurrency, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new List<HoldingDisplayModel>());
            }

            public Task<List<HoldingDisplayModel>> GetHoldingsAsync(Currency targetCurrency, int accountId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new List<HoldingDisplayModel>());
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

            public Task<List<GhostfolioSidekick.Model.Accounts.Account>> GetAccountsAsync()
            {
                return Task.FromResult(new List<GhostfolioSidekick.Model.Accounts.Account>
                {
                    new GhostfolioSidekick.Model.Accounts.Account { Id = 1, Name = "Test Account 1" },
                    new GhostfolioSidekick.Model.Accounts.Account { Id = 2, Name = "Test Account 2" }
                });
            }

            public Task<List<HoldingPriceHistoryPoint>> GetHoldingPriceHistoryAsync(
                string symbol,
                DateTime startDate,
                DateTime endDate,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new List<HoldingPriceHistoryPoint>());
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

            public Task<List<GhostfolioSidekick.Model.Accounts.Account>> GetAccountsBySymbolAsync(string symbol)
            {
                return Task.FromResult(new List<GhostfolioSidekick.Model.Accounts.Account>
                {
                    new GhostfolioSidekick.Model.Accounts.Account { Id = 1, Name = "Test Account 1" },
                    new GhostfolioSidekick.Model.Accounts.Account { Id = 2, Name = "Test Account 2" }
                });
            }

            public Task<List<AccountValueHistoryPoint>> GetAccountValueHistoryAsync(
                Currency targetCurrency,
                DateTime startDate,
                DateTime endDate,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_accountHistory);
            }
        }

        [Fact]
        public void Accounts_ShowsLoadingState_WhenIsLoadingIsTrue()
        {
            var mockService = new Mock<IHoldingsDataServiceOLD>();
            // Make the service return a pending task to keep it in loading state
            var tcs = new TaskCompletionSource<List<AccountValueHistoryPoint>>();
            mockService.Setup(s => s.GetAccountValueHistoryAsync(It.IsAny<Currency>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                       .Returns(tcs.Task);
            
            mockService.Setup(s => s.GetMinDateAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(DateOnly.FromDateTime(new DateTime(2020, 1, 1)));
            
            Services.AddSingleton(mockService.Object);
            
            var cut = RenderComponent<Accounts>(parameters => parameters
                .AddCascadingValue(_filterState));
                
            Assert.Contains("Loading Account Data...", cut.Markup);
        }

        [Fact]
        public void Accounts_ShowsEmptyState_WhenNoAccountDataExists()
        {
            Services.AddSingleton<IHoldingsDataServiceOLD>(new FakeAccountsDataService(new List<AccountValueHistoryPoint>()));
            
            var cut = RenderComponent<Accounts>(parameters => parameters
                .AddCascadingValue(_filterState));
                
            cut.WaitForAssertion(() => Assert.Contains("No Account Data Found", cut.Markup), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Accounts_ShowsAccountData_WhenDataExists()
        {
            var accountHistory = new List<AccountValueHistoryPoint>
            {
                new AccountValueHistoryPoint
                {
                    Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                    Account = new GhostfolioSidekick.Model.Accounts.Account { Id = 1, Name = "Test Account 1" },
                    Value = new Money(Currency.EUR, 1000),       // Use EUR to match FilterState
                    Invested = new Money(Currency.EUR, 800),     // Use EUR to match FilterState
                    Balance = new Money(Currency.EUR, 200)       // Use EUR to match FilterState
                }
            };
            
            Services.AddSingleton<IHoldingsDataServiceOLD>(new FakeAccountsDataService(accountHistory));
            
            var cut = RenderComponent<Accounts>(parameters => parameters
                .AddCascadingValue(_filterState));
            
            // Wait for async operations and check for key elements
            cut.WaitForAssertion(() => {
                var markup = cut.Markup;
                Assert.Contains("Account Values Over Time", markup);
                // Check that we're not in loading or error state
                Assert.DoesNotContain("Loading Account Data...", markup);
                Assert.DoesNotContain("No Account Data Found", markup);
                Assert.DoesNotContain("Error Loading Data", markup);
            }, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Accounts_HasCorrectUIStructure_WhenDataIsLoaded()
        {
            var accountHistory = new List<AccountValueHistoryPoint>
            {
                new AccountValueHistoryPoint
                {
                    Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                    Account = new GhostfolioSidekick.Model.Accounts.Account { Id = 1, Name = "Savings Account" },
                    Value = new Money(Currency.EUR, 5000),       // Use EUR to match FilterState
                    Invested = new Money(Currency.EUR, 4000),    // Use EUR to match FilterState
                    Balance = new Money(Currency.EUR, 1000)      // Use EUR to match FilterState
                }
            };
            
            Services.AddSingleton<IHoldingsDataServiceOLD>(new FakeAccountsDataService(accountHistory));
            
            var cut = RenderComponent<Accounts>(parameters => parameters
                .AddCascadingValue(_filterState));
            
            // Wait and then test for successful rendering
            cut.WaitForAssertion(() => {
                var markup = cut.Markup;
                Assert.Contains("Account Values Over Time", markup);
                Assert.Contains("Account Value History", markup); // Card title
                Assert.Contains("Portfolio Summary", markup);
                Assert.DoesNotContain("Error Loading Data", markup);
            }, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Accounts_ShowsCorrectFinancialMetrics_WhenDataExists()
        {
            var accountHistory = new List<AccountValueHistoryPoint>
            {
                new AccountValueHistoryPoint
                {
                    Date = DateOnly.FromDateTime(DateTime.Today),
                    Account = new GhostfolioSidekick.Model.Accounts.Account { Id = 1, Name = "Test Account" },
                    Value = new Money(Currency.EUR, 1000),       // Use EUR to match FilterState
                    Invested = new Money(Currency.EUR, 800),     // Use EUR to match FilterState
                    Balance = new Money(Currency.EUR, 200)       // Use EUR to match FilterState
                }
            };
            
            Services.AddSingleton<IHoldingsDataServiceOLD>(new FakeAccountsDataService(accountHistory));
            
            var cut = RenderComponent<Accounts>(parameters => parameters
                .AddCascadingValue(_filterState));
            
            cut.WaitForAssertion(() => {
                var markup = cut.Markup;
                Assert.Contains("Portfolio Summary", markup);
                Assert.Contains("Account Breakdown", markup);
                Assert.Contains("Test Account", markup);
                Assert.DoesNotContain("Error Loading Data", markup);
            }, TimeSpan.FromSeconds(5));
        }
    }
}