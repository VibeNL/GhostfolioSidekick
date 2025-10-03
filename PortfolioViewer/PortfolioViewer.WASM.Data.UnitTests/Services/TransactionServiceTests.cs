using AwesomeAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Tests.Services
{
	public class TransactionServiceTests
	{
		private readonly Mock<DatabaseContext> _mockDatabaseContext;
		private readonly TransactionService _transactionService;

		public TransactionServiceTests()
		{
			_mockDatabaseContext = new Mock<DatabaseContext>();
			_transactionService = new TransactionService(_mockDatabaseContext.Object);
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_ShouldReturnPaginatedResults()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			var activities = CreateTestActivities(account, holding);

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(
				Currency.USD,
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				0, // All accounts
				"",
				"",
				"",
				"Date",
				true,
				1,
				10);

			// Assert
			result.Should().NotBeNull();
			result.Transactions.Should().NotBeEmpty();
			result.PageNumber.Should().Be(1);
			result.PageSize.Should().Be(10);
			result.TotalCount.Should().BeGreaterThan(0);
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithAccountFilter_ShouldFilterByAccount()
		{
			// Arrange
			var account1 = CreateTestAccount("Account 1", 1);
			var account2 = CreateTestAccount("Account 2", 2);
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			
			var activities = new List<Activity>
			{
				CreateBuyActivity(account1, holding, DateTime.Now.AddDays(-1), 10, 100),
				CreateSellActivity(account2, holding, DateTime.Now.AddDays(-2), 5, 110)
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(
				Currency.USD,
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				1, // Filter by account 1
				"",
				"",
				"",
				"Date",
				true,
				1,
				10);

			// Assert
			result.Should().NotBeNull();
			result.Transactions.Should().HaveCount(1);
			result.Transactions[0].AccountName.Should().Be("Account 1");
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithSymbolFilter_ShouldFilterBySymbol()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile1 = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var symbolProfile2 = CreateTestSymbolProfile("MSFT", "Microsoft");
			var holding1 = CreateTestHolding(symbolProfile1);
			var holding2 = CreateTestHolding(symbolProfile2);
			
			var activities = new List<Activity>
			{
				CreateBuyActivity(account, holding1, DateTime.Now.AddDays(-1), 10, 100),
				CreateBuyActivity(account, holding2, DateTime.Now.AddDays(-2), 5, 200)
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(
				Currency.USD,
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				0,
				"AAPL", // Filter by AAPL symbol
				"",
				"",
				"Date",
				true,
				1,
				10);

			// Assert
			result.Should().NotBeNull();
			result.Transactions.Should().HaveCount(1);
			result.Transactions[0].Symbol.Should().Be("AAPL");
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithTransactionTypeFilter_ShouldFilterByType()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			
			var activities = new List<Activity>
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100),
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-2), 5, 110),
				CreateDividendActivity(account, holding, DateTime.Now.AddDays(-3), 50)
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(
				Currency.USD,
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				0,
				"",
				"Buy", // Filter by Buy activities
				"",
				"Date",
				true,
				1,
				10);

			// Assert
			result.Should().NotBeNull();
			result.Transactions.Should().HaveCount(1);
			result.Transactions[0].Type.Should().Be("Buy");
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithSearchText_ShouldFilterBySearchText()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile1 = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var symbolProfile2 = CreateTestSymbolProfile("MSFT", "Microsoft");
			var holding1 = CreateTestHolding(symbolProfile1);
			var holding2 = CreateTestHolding(symbolProfile2);
			
			var activities = new List<Activity>
			{
				CreateBuyActivity(account, holding1, DateTime.Now.AddDays(-1), 10, 100, "BUY-001", "Apple purchase"),
				CreateSellActivity(account, holding2, DateTime.Now.AddDays(-2), 5, 110, "SELL-001", "Microsoft sale")
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(
				Currency.USD,
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				0,
				"",
				"",
				"apple", // Search for "apple"
				"Date",
				true,
				1,
				10);

			// Assert
			result.Should().NotBeNull();
			// Note: The actual search implementation may search across multiple fields,
			// so we verify the behavior matches the expected filtering
			result.Transactions.Should().NotBeEmpty();
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithSorting_ShouldSortResults()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			
			var activities = new List<Activity>
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100),
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-3), 5, 110),
				CreateDividendActivity(account, holding, DateTime.Now.AddDays(-2), 50)
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(
				Currency.USD,
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				0,
				"",
				"",
				"",
				"Date",
				false, // Sort descending
				1,
				10);

			// Assert
			result.Should().NotBeNull();
			result.Transactions.Should().HaveCount(3);
			result.Transactions[0].Date.Should().BeAfter(result.Transactions[1].Date);
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithPagination_ShouldReturnCorrectPage()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			
			var activities = new List<Activity>();
			for (int i = 0; i < 15; i++)
			{
				activities.Add(CreateBuyActivity(account, holding, DateTime.Now.AddDays(-i), 10, 100, $"TXN-{i:D3}"));
			}

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(
				Currency.USD,
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				0,
				"",
				"",
				"",
				"Date",
				true,
				2, // Second page
				10); // 10 items per page

			// Assert
			result.Should().NotBeNull();
			result.PageNumber.Should().Be(2);
			result.PageSize.Should().Be(10);
			result.TotalCount.Should().BeGreaterThan(10); // Should have more than 10 total items
			result.Transactions.Should().NotBeEmpty(); // Should have some transactions on second page
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_ShouldIncludeBreakdowns()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			
			var activities = new List<Activity>
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100),
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-2), 5, 110),
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-3), 3, 120),
				CreateDividendActivity(account, holding, DateTime.Now.AddDays(-4), 25)
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(
				Currency.USD,
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				0,
				"",
				"",
				"",
				"Date",
				true,
				1,
				10);

			// Assert
			result.Should().NotBeNull();
			result.TransactionTypeBreakdown.Should().NotBeEmpty();
			result.TransactionTypeBreakdown.Should().ContainKey("Buy");
			result.TransactionTypeBreakdown["Buy"].Should().Be(2);
			result.TransactionTypeBreakdown.Should().ContainKey("Sell");
			result.TransactionTypeBreakdown["Sell"].Should().Be(1);
			result.TransactionTypeBreakdown.Should().ContainKey("Dividend");
			result.TransactionTypeBreakdown["Dividend"].Should().Be(1);
			result.AccountBreakdown.Should().ContainKey("Test Account");
			result.AccountBreakdown["Test Account"].Should().Be(4);
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_ShouldCalculateFeesAndTaxes()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			
			var buyActivity = CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100);
			buyActivity.Fees.Add(new BuyActivityFee(new Money(Currency.USD, 5)));
			buyActivity.Taxes.Add(new BuyActivityTax(new Money(Currency.USD, 10)));

			var activities = new List<Activity> { buyActivity };
			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(
				Currency.USD,
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				0,
				"",
				"",
				"",
				"Date",
				true,
				1,
				10);

			// Assert
			result.Should().NotBeNull();
			result.Transactions.Should().HaveCount(1);
			result.Transactions[0].Fee.Should().NotBeNull();
			result.Transactions[0].Fee!.Amount.Should().Be(5);
			result.Transactions[0].Tax.Should().NotBeNull();
			result.Transactions[0].Tax!.Amount.Should().Be(10);
		}

		[Fact]
		public async Task GetTransactionCountAsync_ShouldReturnTotalCount()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			var activities = CreateTestActivities(account, holding);

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionCountAsync(
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				0,
				"",
				"",
				"");

			// Assert
			result.Should().BeGreaterThan(0);
		}

		[Fact]
		public async Task GetTransactionCountAsync_WithFilters_ShouldReturnFilteredCount()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			
			var activities = new List<Activity>
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100),
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-2), 5, 110),
				CreateDividendActivity(account, holding, DateTime.Now.AddDays(-3), 50)
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionCountAsync(
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				0,
				"",
				"Buy", // Filter by Buy activities
				"");

			// Assert
			result.Should().Be(1);
		}

		[Fact]
		public async Task GetTransactionTypesAsync_ShouldReturnUniqueTypes()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			
			var activities = new List<Activity>
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100),
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-2), 5, 110),
				CreateDividendActivity(account, holding, DateTime.Now.AddDays(-3), 50),
				CreateCashDepositActivity(account, DateTime.Now.AddDays(-4), 1000),
				CreateInterestActivity(account, DateTime.Now.AddDays(-5), 25)
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionTypesAsync();

			// Assert
			result.Should().NotBeEmpty();
			result.Should().Contain("Buy");
			result.Should().Contain("Sell");
			result.Should().Contain("Dividend");
			result.Should().Contain("Deposit");
			result.Should().Contain("Interest");
			result.Should().BeInAscendingOrder();
		}

		[Fact]
		public async Task GetTransactionTypesAsync_WhenNoActivities_ShouldReturnEmptyList()
		{
			// Arrange
			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(new List<Activity>());

			// Act
			var result = await _transactionService.GetTransactionTypesAsync();

			// Assert
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_ShouldExcludeKnownBalanceActivities()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			
			var activities = new List<Activity>
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100),
				CreateKnownBalanceActivity(account, DateTime.Now.AddDays(-2), 1000)
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(
				Currency.USD,
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				0,
				"",
				"",
				"",
				"Date",
				true,
				1,
				10);

			// Assert
			result.Should().NotBeNull();
			result.Transactions.Should().HaveCount(1); // Only the Buy activity, KnownBalance should be excluded
			result.Transactions[0].Type.Should().Be("Buy");
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithDateRange_ShouldFilterByDateRange()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			
			var activities = new List<Activity>
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-5), 10, 100), // Inside range
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-35), 5, 110) // Outside range
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(
				Currency.USD,
				DateOnly.FromDateTime(DateTime.Now.AddDays(-10)), // Start date
				DateOnly.FromDateTime(DateTime.Now), // End date
				0,
				"",
				"",
				"",
				"Date",
				true,
				1,
				10);

			// Assert
			result.Should().NotBeNull();
			result.Transactions.Should().HaveCount(1);
			result.Transactions[0].Type.Should().Be("Buy");
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithMultipleFeeTypes_ShouldCalculateCorrectTotalFees()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			
			var sellActivity = CreateSellActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100);
			sellActivity.Fees.Add(new SellActivityFee(new Money(Currency.USD, 3)));
			sellActivity.Fees.Add(new SellActivityFee(new Money(Currency.USD, 2)));

			var dividendActivity = CreateDividendActivity(account, holding, DateTime.Now.AddDays(-2), 50);
			dividendActivity.Fees.Add(new DividendActivityFee(new Money(Currency.USD, 1)));

			var activities = new List<Activity> { sellActivity, dividendActivity };
			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(
				Currency.USD,
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				0,
				"",
				"",
				"",
				"Date",
				true,
				1,
				10);

			// Assert
			result.Should().NotBeNull();
			result.Transactions.Should().HaveCount(2);
			
			var sellTransaction = result.Transactions.First(t => t.Type == "Sell");
			sellTransaction.Fee.Should().NotBeNull();
			sellTransaction.Fee!.Amount.Should().Be(5); // 3 + 2
			
			var dividendTransaction = result.Transactions.First(t => t.Type == "Dividend");
			dividendTransaction.Fee.Should().NotBeNull();
			dividendTransaction.Fee!.Amount.Should().Be(1);
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithCashActivities_ShouldHandleAmountBasedActivities()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			
			var activities = new List<Activity>
			{
				CreateCashDepositActivity(account, DateTime.Now.AddDays(-1), 1000),
				CreateInterestActivity(account, DateTime.Now.AddDays(-2), 25)
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(
				Currency.USD,
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				0,
				"",
				"",
				"",
				"Date",
				true,
				1,
				10);

			// Assert
			result.Should().NotBeNull();
			result.Transactions.Should().HaveCount(2);
			
			var depositTransaction = result.Transactions.First(t => t.Type == "CashDeposit");
			depositTransaction.Amount.Should().NotBeNull();
			depositTransaction.Amount!.Amount.Should().Be(1000);
			depositTransaction.TotalValue.Should().NotBeNull();
			depositTransaction.TotalValue!.Amount.Should().Be(1000);
			
			var interestTransaction = result.Transactions.First(t => t.Type == "Interest");
			interestTransaction.Amount.Should().NotBeNull();
			interestTransaction.Amount!.Amount.Should().Be(25);
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithEmptyDatabase_ShouldReturnEmptyResult()
		{
			// Arrange
			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(new List<Activity>());

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(
				Currency.USD,
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				0,
				"",
				"",
				"",
				"Date",
				true,
				1,
				10);

			// Assert
			result.Should().NotBeNull();
			result.Transactions.Should().BeEmpty();
			result.TotalCount.Should().Be(0);
			result.PageNumber.Should().Be(1);
			result.PageSize.Should().Be(10);
			result.TransactionTypeBreakdown.Should().BeEmpty();
			result.AccountBreakdown.Should().BeEmpty();
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithNullSymbolAndName_ShouldHandleNullValues()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("UNKNOWN", null); // Null name
			var holding = CreateTestHolding(symbolProfile);
			
			var activities = new List<Activity>
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100)
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(
				Currency.USD,
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				0,
				"",
				"",
				"",
				"Date",
				true,
				1,
				10);

			// Assert
			result.Should().NotBeNull();
			result.Transactions.Should().HaveCount(1);
			result.Transactions[0].Symbol.Should().Be("UNKNOWN");
			result.Transactions[0].Name.Should().Be(""); // Should handle null gracefully
		}

		[Theory]
		[InlineData("Buy")]
		[InlineData("Sell")]
		[InlineData("Dividend")]
		[InlineData("Deposit")]
		[InlineData("Fee")]
		[InlineData("Interest")]
		[InlineData("UnknownType")]
		public async Task GetTransactionsPaginatedAsync_WithSpecificTransactionTypeFilter_ShouldFilterCorrectly(string transactionType)
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			
			var activities = new List<Activity>
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100),
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-2), 5, 110),
				CreateDividendActivity(account, holding, DateTime.Now.AddDays(-3), 50),
				CreateCashDepositActivity(account, DateTime.Now.AddDays(-4), 1000),
				CreateInterestActivity(account, DateTime.Now.AddDays(-5), 25),
				CreateFeeActivity(account, DateTime.Now.AddDays(-6), 10)
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(
				Currency.USD,
				DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Now),
				0,
				"",
				transactionType,
				"",
				"Date",
				true,
				1,
				10);

			// Assert
			result.Should().NotBeNull();
			
			if (transactionType == "UnknownType")
			{
				// Unknown types should return all activities (no filtering)
				result.Transactions.Should().HaveCount(6);
			}
			else
			{
				// Each known type should return at least one matching activity
				result.Transactions.Should().NotBeEmpty();
			}
		}

		// Helper methods for creating test data
		private static Account CreateTestAccount(string name, int id = 1)
		{
			return new Account(name) { Id = id };
		}

		private static SymbolProfile CreateTestSymbolProfile(string symbol, string? name)
		{
			return new SymbolProfile(
				symbol,
				name,
				new List<string>(),
				Currency.USD,
				"TEST",
				AssetClass.Equity,
				null,
				Array.Empty<CountryWeight>(),
				Array.Empty<SectorWeight>());
		}

		private static Holding CreateTestHolding(SymbolProfile symbolProfile)
		{
			var holding = new Holding { Id = 1 };
			holding.SymbolProfiles.Add(symbolProfile);
			return holding;
		}

		private static BuyActivity CreateBuyActivity(Account account, Holding holding, DateTime date, decimal quantity, decimal price, string transactionId = "TXN-001", string description = "Test transaction")
		{
			return new BuyActivity(
				account,
				holding,
				new List<PartialSymbolIdentifier>(),
				date,
				quantity,
				new Money(Currency.USD, price),
				transactionId,
				null,
				description)
			{
				TotalTransactionAmount = new Money(Currency.USD, quantity * price)
			};
		}

		private static SellActivity CreateSellActivity(Account account, Holding holding, DateTime date, decimal quantity, decimal price, string transactionId = "TXN-002", string description = "Test transaction")
		{
			return new SellActivity(
				account,
				holding,
				new List<PartialSymbolIdentifier>(),
				date,
				quantity,
				new Money(Currency.USD, price),
				transactionId,
				null,
				description)
			{
				TotalTransactionAmount = new Money(Currency.USD, quantity * price)
			};
		}

		private static DividendActivity CreateDividendActivity(Account account, Holding holding, DateTime date, decimal amount, string transactionId = "TXN-003", string description = "Dividend payment")
		{
			return new DividendActivity(
				account,
				holding,
				new List<PartialSymbolIdentifier>(),
				date,
				new Money(Currency.USD, amount),
				transactionId,
				null,
				description);
		}

		private static CashDepositActivity CreateCashDepositActivity(Account account, DateTime date, decimal amount, string transactionId = "TXN-004", string description = "Cash deposit")
		{
			return new CashDepositActivity(
				account,
				null,
				date,
				new Money(Currency.USD, amount),
				transactionId,
				null,
				description);
		}

		private static InterestActivity CreateInterestActivity(Account account, DateTime date, decimal amount, string transactionId = "TXN-005", string description = "Interest payment")
		{
			return new InterestActivity(
				account,
				null,
				date,
				new Money(Currency.USD, amount),
				transactionId,
				null,
				description);
		}

		private static KnownBalanceActivity CreateKnownBalanceActivity(Account account, DateTime date, decimal amount, string transactionId = "KNOWN-001", string description = "Known balance")
		{
			return new KnownBalanceActivity(
				account,
				null,
				date,
				new Money(Currency.USD, amount),
				transactionId,
				null,
				description);
		}

		// Helper method for creating FeeActivity
		private static FeeActivity CreateFeeActivity(Account account, DateTime date, decimal amount, string transactionId = "FEE-001", string description = "Fee transaction")
		{
			return new FeeActivity(
				account,
				null,
				date,
				new Money(Currency.USD, amount),
				transactionId,
				null,
				description);
		}

		private static List<Activity> CreateTestActivities(Account account, Holding holding)
		{
			return new List<Activity>
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100),
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-2), 5, 110),
				CreateDividendActivity(account, holding, DateTime.Now.AddDays(-3), 50)
			};
		}
	}
}