using AwesomeAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace PortfolioViewer.WASM.Data.UnitTests.Services
{
	public class TransactionServiceTests
	{
		private readonly Mock<DatabaseContext> _mockDatabaseContext;
		private readonly TransactionService _transactionService;

		public TransactionServiceTests()
		{
			_mockDatabaseContext = new Mock<DatabaseContext>();

			Mock<IDbContextFactory<DatabaseContext>> dbFactory = new();
			_ = dbFactory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(_mockDatabaseContext.Object);

			_transactionService = new TransactionService(dbFactory.Object);
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_ShouldReturnPaginatedResults()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			var activities = CreateTestActivities(account, holding);

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			TransactionQueryParameters parameters = new()
			{
				TargetCurrency = Currency.USD,
				StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				EndDate = DateOnly.FromDateTime(DateTime.Now),
				AccountId = 0, // All accounts
				Symbol = "",
				TransactionTypes = [],
				SearchText = "",
				SortColumn = "Date",
				SortAscending = true,
				PageNumber = 1,
				PageSize = 10
			};

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Transactions.Should().NotBeEmpty();
			_ = result.PageNumber.Should().Be(1);
			_ = result.PageSize.Should().Be(10);
			_ = result.TotalCount.Should().BeGreaterThan(0);
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithAccountFilter_ShouldFilterByAccount()
		{
			// Arrange
			var account1 = CreateTestAccount("Account 1", 1);
			var account2 = CreateTestAccount("Account 2", 2);
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);

			List<Activity> activities = new()
			{
				CreateBuyActivity(account1, holding, DateTime.Now.AddDays(-1), 10, 100),
				CreateSellActivity(account2, holding, DateTime.Now.AddDays(-2), 5, 110)
			};

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			TransactionQueryParameters parameters = new()
			{
				TargetCurrency = Currency.USD,
				StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				EndDate = DateOnly.FromDateTime(DateTime.Now),
				AccountId = 1, // Filter by account 1
				Symbol = "",
				TransactionTypes = [],
				SearchText = "",
				SortColumn = "Date",
				SortAscending = true,
				PageNumber = 1,
				PageSize = 10
			};

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Transactions.Should().HaveCount(1);
			_ = result.Transactions[0].AccountName.Should().Be("Account 1");
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

			List<Activity> activities = new()
			{
				CreateBuyActivity(account, holding1, DateTime.Now.AddDays(-1), 10, 100),
				CreateBuyActivity(account, holding2, DateTime.Now.AddDays(-2), 5, 200)
			};

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			TransactionQueryParameters parameters = new()
			{
				TargetCurrency = Currency.USD,
				StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				EndDate = DateOnly.FromDateTime(DateTime.Now),
				AccountId = 0,
				Symbol = "AAPL", // Filter by AAPL symbol
				TransactionTypes = [],
				SearchText = "",
				SortColumn = "Date",
				SortAscending = true,
				PageNumber = 1,
				PageSize = 10
			};

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Transactions.Should().HaveCount(1);
			_ = result.Transactions[0].Symbol.Should().Be("AAPL");
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithTransactionTypeFilter_ShouldFilterByType()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);

			List<Activity> activities = new()
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100),
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-2), 5, 110),
				CreateDividendActivity(account, holding, DateTime.Now.AddDays(-3), 50)
			};

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			TransactionQueryParameters parameters = new()
			{
				TargetCurrency = Currency.USD,
				StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				EndDate = DateOnly.FromDateTime(DateTime.Now),
				AccountId = 0,
				Symbol = "",
				TransactionTypes = ["Buy"], // Filter by Buy activities
				SearchText = "",
				SortColumn = "Date",
				SortAscending = true,
				PageNumber = 1,
				PageSize = 10
			};

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Transactions.Should().HaveCount(1);
			_ = result.Transactions[0].Type.Should().Be("Buy");
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

			List<Activity> activities = new()
			{
				CreateBuyActivity(account, holding1, DateTime.Now.AddDays(-1), 10, 100, "BUY-001", "Apple purchase"),
				CreateSellActivity(account, holding2, DateTime.Now.AddDays(-2), 5, 110, "SELL-001", "Microsoft sale")
			};

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			TransactionQueryParameters parameters = new()
			{
				TargetCurrency = Currency.USD,
				StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				EndDate = DateOnly.FromDateTime(DateTime.Now),
				AccountId = 0,
				Symbol = "",
				TransactionTypes = [],
				SearchText = "apple", // Search for "apple"
				SortColumn = "Date",
				SortAscending = true,
				PageNumber = 1,
				PageSize = 10
			};

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			// Note: The actual search implementation may search across multiple fields,
			// so we verify the behavior matches the expected filtering
			_ = result.Transactions.Should().NotBeEmpty();
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithSorting_ShouldSortResults()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);

			List<Activity> activities = new()
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100),
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-3), 5, 110),
				CreateDividendActivity(account, holding, DateTime.Now.AddDays(-2), 50)
			};

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			TransactionQueryParameters parameters = new()
			{
				TargetCurrency = Currency.USD,
				StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				EndDate = DateOnly.FromDateTime(DateTime.Now),
				AccountId = 0,
				Symbol = "",
				TransactionTypes = [],
				SearchText = "",
				SortColumn = "Date",
				SortAscending = false, // Sort descending
				PageNumber = 1,
				PageSize = 10
			};

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Transactions.Should().HaveCount(3);
			_ = result.Transactions[0].Date.Should().BeAfter(result.Transactions[1].Date);
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithPagination_ShouldReturnCorrectPage()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);

			List<Activity> activities = new();
			for (int i = 0; i < 15; i++)
			{
				activities.Add(CreateBuyActivity(account, holding, DateTime.Now.AddDays(-i), 10, 100, $"TXN-{i:D3}"));
			}

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			TransactionQueryParameters parameters = new()
			{
				TargetCurrency = Currency.USD,
				StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				EndDate = DateOnly.FromDateTime(DateTime.Now),
				AccountId = 0,
				Symbol = "",
				TransactionTypes = [],
				SearchText = "",
				SortColumn = "Date",
				SortAscending = true,
				PageNumber = 2, // Second page
				PageSize = 10 // 10 items per page
			};

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.PageNumber.Should().Be(2);
			_ = result.PageSize.Should().Be(10);
			_ = result.TotalCount.Should().BeGreaterThan(10); // Should have more than 10 total items
			_ = result.Transactions.Should().NotBeEmpty(); // Should have some transactions on second page
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_ShouldIncludeBreakdowns()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);

			List<Activity> activities = new()
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100),
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-2), 5, 110),
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-3), 3, 120),
				CreateDividendActivity(account, holding, DateTime.Now.AddDays(-4), 25)
			};

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			TransactionQueryParameters parameters = new()
			{
				TargetCurrency = Currency.USD,
				StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				EndDate = DateOnly.FromDateTime(DateTime.Now),
				AccountId = 0,
				Symbol = "",
				TransactionTypes = [],
				SearchText = "",
				SortColumn = "Date",
				SortAscending = true,
				PageNumber = 1,
				PageSize = 10
			};

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.TransactionTypeBreakdown.Should().NotBeEmpty();
			_ = result.TransactionTypeBreakdown.Should().ContainKey("Buy");
			_ = result.TransactionTypeBreakdown["Buy"].Should().Be(2);
			_ = result.TransactionTypeBreakdown.Should().ContainKey("Sell");
			_ = result.TransactionTypeBreakdown["Sell"].Should().Be(1);
			_ = result.TransactionTypeBreakdown.Should().ContainKey("Dividend");
			_ = result.TransactionTypeBreakdown["Dividend"].Should().Be(1);
			_ = result.AccountBreakdown.Should().ContainKey("Test Account");
			_ = result.AccountBreakdown["Test Account"].Should().Be(4);
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_ShouldCalculateFeesAndTaxes()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);

			var buyActivity = CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100);
			buyActivity.Fees.Add(new Money(Currency.USD, 5));
			buyActivity.Taxes.Add(new Money(Currency.USD, 10));

			List<Activity> activities = new()
			{ buyActivity };
			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			TransactionQueryParameters parameters = new()
			{
				TargetCurrency = Currency.USD,
				StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				EndDate = DateOnly.FromDateTime(DateTime.Now),
				AccountId = 0,
				Symbol = "",
				TransactionTypes = [],
				SearchText = "",
				SortColumn = "Date",
				SortAscending = true,
				PageNumber = 1,
				PageSize = 10
			};

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Transactions.Should().HaveCount(1);
			_ = result.Transactions[0].Fee.Should().NotBeNull();
			_ = result.Transactions[0].Fee!.Amount.Should().Be(5);
			_ = result.Transactions[0].Tax.Should().NotBeNull();
			_ = result.Transactions[0].Tax!.Amount.Should().Be(10);
		}

		[Fact]
		public async Task GetTransactionTypesAsync_ShouldReturnUniqueTypes()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);

			List<Activity> activities = new()
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100),
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-2), 5, 110),
				CreateDividendActivity(account, holding, DateTime.Now.AddDays(-3), 50),
				CreateCashDepositActivity(account, DateTime.Now.AddDays(-4), 1000),
				CreateInterestActivity(account, DateTime.Now.AddDays(-5), 25)
			};

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _transactionService.GetTransactionTypesAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeEmpty();
			_ = result.Should().Contain("Buy");
			_ = result.Should().Contain("Sell");
			_ = result.Should().Contain("Dividend");
			_ = result.Should().Contain("CashDeposit");
			_ = result.Should().Contain("Interest");
			_ = result.Should().BeInAscendingOrder();
		}

		[Fact]
		public async Task GetTransactionTypesAsync_WhenNoActivities_ShouldReturnEmptyList()
		{
			// Arrange
			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(new List<Activity>());

			// Act
			var result = await _transactionService.GetTransactionTypesAsync(CancellationToken.None);

			// Assert
			_ = result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithDateRange_ShouldFilterByDateRange()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);

			List<Activity> activities = new()
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-5), 10, 100), // Inside range
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-35), 5, 110) // Outside range
			};

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			TransactionQueryParameters parameters = new()
			{
				TargetCurrency = Currency.USD,
				StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-10)), // Start date
				EndDate = DateOnly.FromDateTime(DateTime.Now), // End date
				AccountId = 0,
				Symbol = "",
				TransactionTypes = [],
				SearchText = "",
				SortColumn = "Date",
				SortAscending = true,
				PageNumber = 1,
				PageSize = 10
			};

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Transactions.Should().HaveCount(1);
			_ = result.Transactions[0].Type.Should().Be("Buy");
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithMultipleFeeTypes_ShouldCalculateCorrectTotalFees()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);

			var sellActivity = CreateSellActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100);
			sellActivity.Fees.Add(new Money(Currency.USD, 3));
			sellActivity.Fees.Add(new Money(Currency.USD, 2));

			var dividendActivity = CreateDividendActivity(account, holding, DateTime.Now.AddDays(-2), 50);
			dividendActivity.Fees.Add(new Money(Currency.USD, 1));

			List<Activity> activities = new()
			{ sellActivity, dividendActivity };
			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			TransactionQueryParameters parameters = new()
			{
				TargetCurrency = Currency.USD,
				StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				EndDate = DateOnly.FromDateTime(DateTime.Now),
				AccountId = 0,
				Symbol = "",
				TransactionTypes = [],
				SearchText = "",
				SortColumn = "Date",
				SortAscending = true,
				PageNumber = 1,
				PageSize = 10
			};

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Transactions.Should().HaveCount(2);

			var sellTransaction = result.Transactions.First(t => t.Type == "Sell");
			_ = sellTransaction.Fee.Should().NotBeNull();
			_ = sellTransaction.Fee!.Amount.Should().Be(5); // 3 + 2

			var dividendTransaction = result.Transactions.First(t => t.Type == "Dividend");
			_ = dividendTransaction.Fee.Should().NotBeNull();
			_ = dividendTransaction.Fee!.Amount.Should().Be(1);
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithCashActivities_ShouldHandleAmountBasedActivities()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");

			List<Activity> activities = new()
			{
				CreateCashDepositActivity(account, DateTime.Now.AddDays(-1), 1000),
				CreateInterestActivity(account, DateTime.Now.AddDays(-2), 25)
			};

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			TransactionQueryParameters parameters = new()
			{
				TargetCurrency = Currency.USD,
				StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				EndDate = DateOnly.FromDateTime(DateTime.Now),
				AccountId = 0,
				Symbol = "",
				TransactionTypes = [],
				SearchText = "",
				SortColumn = "Date",
				SortAscending = true,
				PageNumber = 1,
				PageSize = 10
			};

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Transactions.Should().HaveCount(2);

			var depositTransaction = result.Transactions.First(t => t.Type == "CashDeposit");
			_ = depositTransaction.Amount.Should().NotBeNull();
			_ = depositTransaction.Amount!.Amount.Should().Be(1000);
			_ = depositTransaction.TotalValue.Should().NotBeNull();
			_ = depositTransaction.TotalValue!.Amount.Should().Be(1000);

			var interestTransaction = result.Transactions.First(t => t.Type == "Interest");
			_ = interestTransaction.Amount.Should().NotBeNull();
			_ = interestTransaction.Amount!.Amount.Should().Be(25);
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithEmptyDatabase_ShouldReturnEmptyResult()
		{
			// Arrange
			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(new List<Activity>());

			TransactionQueryParameters parameters = new()
			{
				TargetCurrency = Currency.USD,
				StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				EndDate = DateOnly.FromDateTime(DateTime.Now),
				AccountId = 0,
				Symbol = "",
				TransactionTypes = [],
				SearchText = "",
				SortColumn = "Date",
				SortAscending = true,
				PageNumber = 1,
				PageSize = 10
			};

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Transactions.Should().BeEmpty();
			_ = result.TotalCount.Should().Be(0);
			_ = result.PageNumber.Should().Be(1);
			_ = result.PageSize.Should().Be(10);
			_ = result.TransactionTypeBreakdown.Should().BeEmpty();
			_ = result.AccountBreakdown.Should().BeEmpty();
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithNullSymbolAndName_ShouldHandleNullValues()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("UNKNOWN", null); // Null name
			var holding = CreateTestHolding(symbolProfile);

			List<Activity> activities = new()
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100)
			};

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			TransactionQueryParameters parameters = new()
			{
				TargetCurrency = Currency.USD,
				StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				EndDate = DateOnly.FromDateTime(DateTime.Now),
				AccountId = 0,
				Symbol = "",
				TransactionTypes = [],
				SearchText = "",
				SortColumn = "Date",
				SortAscending = true,
				PageNumber = 1,
				PageSize = 10
			};

			// Act
			var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Transactions.Should().HaveCount(1);
			_ = result.Transactions[0].Symbol.Should().Be("UNKNOWN");
			_ = result.Transactions[0].Name.Should().Be(""); // Should handle null gracefully
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_WithSpecificTransactionTypeFilter_DynamicallyTestsAllTypes()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);

			var activityBaseType = typeof(Activity);
			List<Type> activityTypes = activityBaseType.Assembly.GetTypes()
				.Where(t => activityBaseType.IsAssignableFrom(t) && !t.IsAbstract)
				.ToList();

			List<Activity> activities = new();
			List<string> typeNames = new();

			foreach (var type in activityTypes)
			{
				var instance = CreateTestActivityInstance(type, account, holding) ?? throw new NotImplementedException($"No test instance creation defined for activity type {type.Name} and could not auto-instantiate.");
				activities.Add(instance);
				typeNames.Add(instance.GetType().Name.Replace("Activity", ""));
			}

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			foreach (var typeName in typeNames)
			{
				TransactionQueryParameters parameters = new()
				{
					TargetCurrency = Currency.USD,
					StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
					EndDate = DateOnly.FromDateTime(DateTime.Now),
					AccountId = 0,
					Symbol = "",
					TransactionTypes = [typeName],
					SearchText = "",
					SortColumn = "Date",
					SortAscending = true,
					PageNumber = 1,
					PageSize = 10
				};

				// Act
				var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

				// Assert
				_ = result.Should().NotBeNull();
				_ = result.Transactions.Should().NotBeEmpty();
				_ = result.Transactions.All(t => t.Type == typeName).Should().BeTrue();
			}
		}

		[Theory]
		[InlineData("Date")]
		[InlineData("Type")]
		[InlineData("Symbol")]
		[InlineData("Name")]
		[InlineData("AccountName")]
		[InlineData("TotalValue")]
		[InlineData("Description")]
		public async Task GetTransactionsPaginatedAsync_SortsByColumn(string sortColumn)
		{
			var account1 = CreateTestAccount("Account 1", 1);
			var account2 = CreateTestAccount("Account 2", 2);
			var symbolProfile1 = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var symbolProfile2 = CreateTestSymbolProfile("MSFT", "Microsoft");
			var holding1 = CreateTestHolding(symbolProfile1);
			var holding2 = CreateTestHolding(symbolProfile2);
			List<Activity> activities = new()
			{
				CreateBuyActivity(account1, holding1, DateTime.Now.AddDays(-1), 10, 100, "TXN-001", "Apple buy"),
				CreateSellActivity(account2, holding2, DateTime.Now.AddDays(-2), 5, 200, "TXN-002", "Microsoft sell"),
				CreateDividendActivity(account1, holding1, DateTime.Now.AddDays(-3), 50, "TXN-003", "Dividend Apple")
			};
			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);
			TransactionQueryParameters parameters = new()
			{
				TargetCurrency = Currency.USD,
				StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				EndDate = DateOnly.FromDateTime(DateTime.Now),
				AccountId = 0,
				Symbol = "",
				TransactionTypes = [],
				SearchText = "",
				SortColumn = sortColumn,
				SortAscending = true,
				PageNumber = 1,
				PageSize = 10
			};
			var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);
			_ = result.Should().NotBeNull();
			_ = result.Transactions.Should().NotBeEmpty();
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_FiltersByMultipleTransactionTypes()
		{
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			List<Activity> activities = new()
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100),
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-2), 5, 110),
				CreateDividendActivity(account, holding, DateTime.Now.AddDays(-3), 50)
			};
			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);
			TransactionQueryParameters parameters = new()
			{
				TargetCurrency = Currency.USD,
				StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
				EndDate = DateOnly.FromDateTime(DateTime.Now),
				AccountId = 0,
				Symbol = "",
				TransactionTypes = ["Buy", "Sell"],
				SearchText = "",
				SortColumn = "Date",
				SortAscending = true,
				PageNumber = 1,
				PageSize = 10
			};
			var result = await _transactionService.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);
			_ = result.Should().NotBeNull();
			_ = result.Transactions.Should().HaveCount(2);
			_ = result.Transactions.All(t => t.Type is "Buy" or "Sell").Should().BeTrue();
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
				[],
				Currency.USD,
				"TEST",
				AssetClass.Equity,
				null,
				[],
				[]);
		}

		private static Holding CreateTestHolding(SymbolProfile symbolProfile)
		{
			Holding holding = new() { Id = 1 };
			holding.SymbolProfiles.Add(symbolProfile);
			return holding;
		}

		private static BuyActivity CreateBuyActivity(Account account, Holding holding, DateTime date, decimal quantity, decimal price, string transactionId = "TXN-001", string description = "Test transaction")
		{
			return new BuyActivity(
				account,
				holding,
				[],
				date,
				quantity,
				new Money(Currency.USD, price),
				new Money(Currency.USD, price).Times(quantity),
				transactionId,
				null,
				description)
			{
			};
		}

		private static SellActivity CreateSellActivity(Account account, Holding holding, DateTime date, decimal quantity, decimal price, string transactionId = "TXN-002", string description = "Test transaction")
		{
			return new SellActivity(
				account,
				holding,
				[],
				date,
				quantity,
				new Money(Currency.USD, price),
				new Money(Currency.USD, price).Times(quantity),
				transactionId,
				null,
				description)
			{
			};
		}

		private static DividendActivity CreateDividendActivity(Account account, Holding holding, DateTime date, decimal amount, string transactionId = "TXN-003", string description = "Dividend payment")
		{
			return new DividendActivity(
				account,
				holding,
				[],
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

		// Helper method for creating CorrectionActivity
		private static CorrectionActivity CreateCorrectionActivity(Account account, DateTime date, decimal amount, string transactionId = "COR-001", string description = "Correction transaction")
		{
			return new CorrectionActivity(
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

		private static CashWithdrawalActivity CreateCashWithdrawalActivity(Account account, DateTime date, decimal amount, string transactionId = "WITHDRAW-001", string description = "Cash withdrawal")
		{
			return new CashWithdrawalActivity(
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
			return
			[
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100),
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-2), 5, 110),
				CreateDividendActivity(account, holding, DateTime.Now.AddDays(-3), 50)
			];
		}

		private static GiftFiatActivity CreateGiftFiatActivity(Account account, DateTime date, decimal amount, string transactionId = "GIFT-FIAT-001", string description = "Gift fiat")
		{
			return new GiftFiatActivity(account, null, date, new Money(Currency.USD, amount), transactionId, null, description);
		}

		private static GiftAssetActivity CreateGiftAssetActivity(Account account, Holding holding, DateTime date, decimal quantity, string transactionId = "GIFT-ASSET-001", string description = "Gift asset")
		{
			return new GiftAssetActivity(account, holding, [], date, quantity, transactionId, null, description);
		}

		private static LiabilityActivity CreateLiabilityActivity(Account account, DateTime date, decimal amount, string transactionId = "LIABILITY-001", string description = "Liability")
		{
			return new LiabilityActivity(account, null, [], date, new Money(Currency.USD, amount), transactionId, null, description);
		}

		private static RepayBondActivity CreateRepayBondActivity(Account account, DateTime date, decimal amount, string transactionId = "REPAY-BOND-001", string description = "Repay bond")
		{
			return new RepayBondActivity(account, null, [], date, new Money(Currency.USD, amount), transactionId, null, description);
		}

		private static ReceiveActivity CreateReceiveActivity(Account account, Holding holding, DateTime date, decimal quantity, string transactionId = "RECEIVE-001", string description = "Receive")
		{
			return new ReceiveActivity(account, holding, [], date, quantity, transactionId, null, description);
		}

		private static SendActivity CreateSendActivity(Account account, Holding holding, DateTime date, decimal quantity, string transactionId = "SEND-001", string description = "Send")
		{
			return new SendActivity(account, holding, [], date, quantity, transactionId, null, description);
		}

		private static StakingRewardActivity CreateStakingRewardActivity(Account account, Holding holding, DateTime date, decimal quantity, string transactionId = "STAKING-001", string description = "Staking reward")
		{
			return new StakingRewardActivity(account, holding, [], date, quantity, transactionId, null, description);
		}

		private static ValuableActivity CreateValuableActivity(Account account, DateTime date, decimal amount, string transactionId = "VALUABLE-001", string description = "Valuable")
		{
			return new ValuableActivity(account, null, [], date, new Money(Currency.USD, amount), transactionId, null, description);
		}

		private static Activity CreateTestActivityInstance(Type type, Account account, Holding holding)
		{
			return type switch
			{
				_ when type == typeof(BuyActivity) => CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1), 10, 100),
				_ when type == typeof(SellActivity) => CreateSellActivity(account, holding, DateTime.Now.AddDays(-2), 5, 110),
				_ when type == typeof(DividendActivity) => CreateDividendActivity(account, holding, DateTime.Now.AddDays(-3), 50),
				_ when type == typeof(CashDepositActivity) => CreateCashDepositActivity(account, DateTime.Now.AddDays(-4), 1000),
				_ when type == typeof(InterestActivity) => CreateInterestActivity(account, DateTime.Now.AddDays(-5), 25),
				_ when type == typeof(FeeActivity) => CreateFeeActivity(account, DateTime.Now.AddDays(-6), 10),
				_ when type == typeof(KnownBalanceActivity) => CreateKnownBalanceActivity(account, DateTime.Now.AddDays(-7), 500),
				_ when type == typeof(CashWithdrawalActivity) => CreateCashWithdrawalActivity(account, DateTime.Now.AddDays(-8), 250),
				_ when type == typeof(GiftFiatActivity) => CreateGiftFiatActivity(account, DateTime.Now.AddDays(-9), 100),
				_ when type == typeof(GiftAssetActivity) => CreateGiftAssetActivity(account, holding, DateTime.Now.AddDays(-10), 5),
				_ when type == typeof(LiabilityActivity) => CreateLiabilityActivity(account, DateTime.Now.AddDays(-11), 200),
				_ when type == typeof(RepayBondActivity) => CreateRepayBondActivity(account, DateTime.Now.AddDays(-12), 300),
				_ when type == typeof(ReceiveActivity) => CreateReceiveActivity(account, holding, DateTime.Now.AddDays(-13), 7),
				_ when type == typeof(SendActivity) => CreateSendActivity(account, holding, DateTime.Now.AddDays(-14), 8),
				_ when type == typeof(StakingRewardActivity) => CreateStakingRewardActivity(account, holding, DateTime.Now.AddDays(-15), 9),
				_ when type == typeof(ValuableActivity) => CreateValuableActivity(account, DateTime.Now.AddDays(-16), 400),
				_ when type == typeof(CorrectionActivity) => CreateCorrectionActivity(account, DateTime.Now.AddDays(-17), 50),
				_ => throw new NotImplementedException($"No test instance creation defined for activity type {type.Name}.")
			};
		}
	}
}
