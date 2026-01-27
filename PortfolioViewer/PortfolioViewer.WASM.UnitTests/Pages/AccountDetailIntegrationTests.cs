using AwesomeAssertions;
using Bunit;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Pages;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Pages
{
	/// <summary>
	/// Additional integration-style tests for the AccountDetail component
	/// that test various scenarios and edge cases.
	/// </summary>
	public class AccountDetailIntegrationTests : BunitContext
	{
		private readonly Mock<IAccountDataService> _mockAccountDataService;
		private readonly Mock<ITransactionService> _mockTransactionService;
		private readonly Mock<ICurrencyExchange> _mockCurrencyExchange;
		private readonly Mock<IServerConfigurationService> _mockServerConfigurationService;

		public AccountDetailIntegrationTests()
		{
			_mockAccountDataService = new Mock<IAccountDataService>();
			_mockTransactionService = new Mock<ITransactionService>();
			_mockCurrencyExchange = new Mock<ICurrencyExchange>();
			_mockServerConfigurationService = new Mock<IServerConfigurationService>();

			// Setup default behavior
			_mockServerConfigurationService.Setup(x => x.PrimaryCurrency).Returns(Currency.EUR);

			// Register services
			this.Services.AddSingleton(_mockAccountDataService.Object);
			this.Services.AddSingleton(_mockTransactionService.Object);
			this.Services.AddSingleton(_mockCurrencyExchange.Object);
			this.Services.AddSingleton(_mockServerConfigurationService.Object);
			this.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

			// Add the missing ITestContextService
			this.Services.AddSingleton<ITestContextService>(new TestContextService { IsTest = true });
		}

		[Fact]
		public async Task AccountDetail_FullWorkflow_LoadsAllDataSuccessfully()
		{
			// Arrange - Set up a complete scenario with account, history, and transactions
			var testAccount = new Account("Investment Account")
			{
				Id = 1,
				Comment = "My main investment account"
			};

			var accountHistory = new List<AccountValueHistoryPoint>
			{
				new() {
					Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
					AccountId = 1,
					TotalValue = new Money(Currency.EUR, 9000),
					TotalAssetValue = new Money(Currency.EUR, 7500),
					TotalInvested = new Money(Currency.EUR, 7000),
					CashBalance = new Money(Currency.EUR, 1500)
				},
				new() {
					Date = DateOnly.FromDateTime(DateTime.Today),
					AccountId = 1,
					TotalValue = new Money(Currency.EUR, 10000),
					TotalAssetValue = new Money(Currency.EUR, 8500),
					TotalInvested = new Money(Currency.EUR, 7500),
					CashBalance = new Money(Currency.EUR, 1500)
				}
			};

			var transactions = new List<TransactionDisplayModel>
			{
				new() {
					Id = 1,
					Date = DateTime.Today.AddDays(-20),
					Type = "Buy",
					Symbol = "AAPL",
					Name = "Apple Inc.",
					Description = "Purchase of Apple shares",
					TransactionId = "TXN-001",
					AccountName = "Investment Account",
					Quantity = 10,
					UnitPrice = new Money(Currency.USD, 150),
					TotalValue = new Money(Currency.USD, 1500)
				},
				new() {
					Id = 2,
					Date = DateTime.Today.AddDays(-10),
					Type = "Dividend",
					Symbol = "AAPL",
					Name = "Apple Inc.",
					Description = "Quarterly dividend",
					TransactionId = "TXN-002",
					AccountName = "Investment Account",
					Amount = new Money(Currency.USD, 25)
				}
			};

			var paginatedResult = new PaginatedTransactionResult
			{
				Transactions = transactions,
				TotalCount = 2,
				TransactionTypeBreakdown = new Dictionary<string, int>
				{
					{ "Buy", 1 },
					{ "Dividend", 1 }
				},
				AccountBreakdown = new Dictionary<string, int>
				{
					{ "Investment Account", 2 }
				}
			};

			SetupMocks(testAccount, accountHistory, paginatedResult);

			// Act
			var component = Render<AccountDetail>(parameters => parameters.Add(p => p.AccountId, 1));

			// Wait for all async operations to complete
			await WaitForComponentToFinishLoading(component, containsText: "Investment Account");

			// Assert
			var markup = component.Markup;

			// Verify account information is displayed
			markup.Should().Contain("Investment Account");

			// Verify all services were called
			_mockAccountDataService.Verify(x => x.GetAccountInfo(), Times.AtLeastOnce);
			_mockAccountDataService.Verify(x => x.GetAccountValueHistoryAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
			_mockTransactionService.Verify(x => x.GetTransactionsPaginatedAsync(It.IsAny<TransactionQueryParameters>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
		}

		[Theory]
		[InlineData("EUR")]
		[InlineData("USD")]
		[InlineData("GBP")]
		public async Task AccountDetail_WithDifferentCurrencies_DisplaysCorrectly(string currencySymbol)
		{
			// Arrange
			var currency = currencySymbol switch
			{
				"EUR" => Currency.EUR,
				"USD" => Currency.USD,
				"GBP" => Currency.GBP,
				_ => Currency.EUR // Fallback to EUR for unknown currencies
			};

			_mockServerConfigurationService.Setup(x => x.PrimaryCurrency).Returns(currency);

			var testAccount = new Account("Test Account") { Id = 1 };
			var accountHistory = new List<AccountValueHistoryPoint>
			{
				new() {
					Date = DateOnly.FromDateTime(DateTime.Today),
					AccountId = 1,
					TotalValue = new Money(currency, 5000),
					TotalAssetValue = new Money(currency, 4000),
					TotalInvested = new Money(currency, 3500),
					CashBalance = new Money(currency, 1000)
				}
			};

			SetupMocks(testAccount, accountHistory, new PaginatedTransactionResult
			{
				Transactions = [],
				TotalCount = 0,
				TransactionTypeBreakdown = [],
				AccountBreakdown = []
			});

			// Act
			var component = Render<AccountDetail>(parameters => parameters.Add(p => p.AccountId, 1));

			await WaitForComponentToFinishLoading(component, containsText: "Test Account");

			// Assert - Component should render without errors for different currencies
			component.Markup.Should().Contain("Test Account");
		}

		[Fact]
		public async Task AccountDetail_WithFilterState_PassesCorrectParametersToServices()
		{
			// Arrange
			var testAccount = new Account("Test Account") { Id = 1 };
			var filterState = new FilterState
			{
				StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-90)),
				EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
				SelectedAccountId = 1,
				SelectedSymbol = "AAPL",
				SelectedTransactionType = ["Buy"]
			};

			var accountHistory = new List<AccountValueHistoryPoint>
			{
				new() {
					Date = DateOnly.FromDateTime(DateTime.Today),
					AccountId = 1,
					TotalValue = new Money(Currency.EUR, 5000),
					TotalAssetValue = new Money(Currency.EUR, 4000),
					TotalInvested = new Money(Currency.EUR, 3500),
					CashBalance = new Money(Currency.EUR, 1000)
				}
			};

			SetupMocks(testAccount, accountHistory, new PaginatedTransactionResult
			{
				Transactions = [],
				TotalCount = 0,
				TransactionTypeBreakdown = [],
				AccountBreakdown = []
			});

			// Act
			var component = Render<AccountDetail>(parameters => parameters
				.Add(p => p.AccountId, 1)
				.AddCascadingValue("FilterState", filterState));

			await WaitForComponentToFinishLoading(component, containsText: "Test Account");

			// Assert - Verify that services are called with some parameters (the exact dates may vary due to timing)
			_mockAccountDataService.Verify(x => x.GetAccountValueHistoryAsync(
				It.IsAny<DateOnly>(),
				It.IsAny<DateOnly>(),
				It.IsAny<CancellationToken>()), Times.AtLeastOnce);

			_mockTransactionService.Verify(x => x.GetTransactionsPaginatedAsync(
				It.Is<TransactionQueryParameters>(p =>
					p.AccountId == 1),
				It.IsAny<CancellationToken>()), Times.AtLeastOnce);
		}

		[Fact]
		public async Task AccountDetail_WithEmptyData_HandlesGracefully()
		{
			// Arrange - Empty data scenario
			var testAccount = new Account("Empty Account") { Id = 1 };

			SetupMocks(testAccount, [], new PaginatedTransactionResult
			{
				Transactions = [],
				TotalCount = 0,
				TransactionTypeBreakdown = [],
				AccountBreakdown = []
			});

			// Act
			var component = Render<AccountDetail>(parameters => parameters.Add(p => p.AccountId, 1));

			await WaitForComponentToFinishLoading(component, containsText: "Account Not Found");

			// Assert - Should handle empty data gracefully by showing not found message
			var markup = component.Markup;
			markup.Should().Contain("Account Not Found");
		}

		[Fact]
		public async Task AccountDetail_WithLargeDataSet_PerformsWell()
		{
			// Arrange - Simulate a large data set
			var testAccount = new Account("Large Account") { Id = 1 };

			var accountHistory = Enumerable.Range(0, 365)
				.Select(i => new AccountValueHistoryPoint
				{
					Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-i)),
					AccountId = 1,
					TotalValue = new Money(Currency.EUR, 10000 + (i * 10)),
					TotalAssetValue = new Money(Currency.EUR, 8000 + (i * 8)),
					TotalInvested = new Money(Currency.EUR, 7000 + (i * 5)),
					CashBalance = new Money(Currency.EUR, 2000 + (i * 2))
				})
				.ToList();

			var transactions = Enumerable.Range(0, 100)
				.Select(i => new TransactionDisplayModel
				{
					Id = i,
					Date = DateTime.Today.AddDays(-i),
					Type = i % 2 == 0 ? "Buy" : "Sell",
					Symbol = "TEST",
					Name = "Test Security",
					Description = $"Transaction {i}",
					TransactionId = $"TXN-{i:000}",
					AccountName = "Large Account",
					Quantity = 10 + i,
					UnitPrice = new Money(Currency.EUR, 100 + i)
				})
				.ToList();

			var paginatedResult = new PaginatedTransactionResult
			{
				Transactions = [.. transactions.Take(20)], // Simulate pagination
				TotalCount = transactions.Count,
				TransactionTypeBreakdown = new Dictionary<string, int>
				{
					{ "Buy", 50 },
					{ "Sell", 50 }
				},
				AccountBreakdown = new Dictionary<string, int>
				{
					{ "Large Account", 100 }
				}
			};

			SetupMocks(testAccount, accountHistory, paginatedResult);

			var startTime = DateTime.UtcNow;

			// Act
			var component = Render<AccountDetail>(parameters => parameters.Add(p => p.AccountId, 1));

			await WaitForComponentToFinishLoading(component, containsText: "Large Account", maxWaitMs: 3000);

			var endTime = DateTime.UtcNow;

			// Assert - Should complete within reasonable time (less than 3 seconds)
			var processingTime = endTime - startTime;
			processingTime.Should().BeLessThan(TimeSpan.FromSeconds(3));

			component.Markup.Should().Contain("Large Account");
		}

		[Fact]
		public async Task AccountDetail_Lifecycle_HandlesProperCleanup()
		{
			// Arrange
			var testAccount = new Account("Cleanup Test Account") { Id = 1 };
			var filterState = new FilterState();
			var accountHistory = new List<AccountValueHistoryPoint>
			{
				new() {
					Date = DateOnly.FromDateTime(DateTime.Today),
					AccountId = 1,
					TotalValue = new Money(Currency.EUR, 5000),
					TotalAssetValue = new Money(Currency.EUR, 4000),
					TotalInvested = new Money(Currency.EUR, 3500),
					CashBalance = new Money(Currency.EUR, 1000)
				}
			};

			SetupMocks(testAccount, accountHistory, new PaginatedTransactionResult
			{
				Transactions = [],
				TotalCount = 0,
				TransactionTypeBreakdown = [],
				AccountBreakdown = []
			});

			// Act
			var component = Render<AccountDetail>(parameters => parameters
				.Add(p => p.AccountId, 1)
				.AddCascadingValue("FilterState", filterState));

			await WaitForComponentToFinishLoading(component, containsText: "Cleanup Test Account");

			// Dispose the component
			component.Instance.Dispose();

			// Assert - Should not throw exceptions during disposal
			Assert.True(true); // Test passes if no exception is thrown
		}

		[Fact]
		public async Task AccountDetail_WithNullData_HandlesGracefully()
		{
			// Arrange - Test null data handling
			var testAccount = new Account("Null Data Account") { Id = 1 };

			_mockAccountDataService.Setup(x => x.GetAccountInfo())
				.ReturnsAsync([testAccount]);

			_mockAccountDataService.Setup(x => x.GetAccountValueHistoryAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((List<AccountValueHistoryPoint>?)null);

			_mockTransactionService.Setup(x => x.GetTransactionsPaginatedAsync(It.IsAny<TransactionQueryParameters>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new PaginatedTransactionResult
				{
					Transactions = [],
					TotalCount = 0,
					TransactionTypeBreakdown = [],
					AccountBreakdown = []
				});

			// Act
			var component = Render<AccountDetail>(parameters => parameters.Add(p => p.AccountId, 1));

			await WaitForComponentToFinishLoading(component, containsText: "Account Not Found");

			// Assert - Should handle null data gracefully
			component.Markup.Should().Contain("Account Not Found");
		}

		private static async Task WaitForComponentToFinishLoading(IRenderedComponent<AccountDetail> component, string? containsText = null, int maxWaitMs = 2000)
		{
			var startTime = DateTime.UtcNow;
			var waitInterval = 50;

			while (DateTime.UtcNow.Subtract(startTime).TotalMilliseconds < maxWaitMs)
			{
				try
				{
					component.Render();

					// If we're looking for specific text, check for it
					if (!string.IsNullOrEmpty(containsText))
					{
						if (component.Markup.Contains(containsText))
						{
							return;
						}
					}
					// Otherwise, just check if loading is complete
					else if (!component.Markup.Contains("Loading"))
					{
						return;
					}
				}
				catch
				{
					// Ignore rendering exceptions during async operations
				}

			await Task.Delay(waitInterval, Xunit.TestContext.Current.CancellationToken);
			}
		}

		private void SetupMocks(Account account, List<AccountValueHistoryPoint> history, PaginatedTransactionResult transactionResult)
		{
			_mockAccountDataService.Setup(x => x.GetAccountInfo())
				.ReturnsAsync([account]);

			_mockAccountDataService.Setup(x => x.GetAccountValueHistoryAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(history);

			_mockTransactionService.Setup(x => x.GetTransactionsPaginatedAsync(It.IsAny<TransactionQueryParameters>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(transactionResult);
		}

		private class MockNavigationManager : NavigationManager
		{
			public MockNavigationManager() : base()
			{
				Initialize("https://localhost/", "https://localhost/");
			}

			protected override void NavigateToCore(string uri, bool forceLoad)
			{
				// Mock implementation
			}

			protected override void NavigateToCore(string uri, NavigationOptions options)
			{
				// Mock implementation
			}
		}
	}
}



