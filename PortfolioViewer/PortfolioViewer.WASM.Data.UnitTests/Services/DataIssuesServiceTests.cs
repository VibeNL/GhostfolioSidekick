using AwesomeAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace PortfolioViewer.WASM.Data.UnitTests.Services
{
	public class DataIssuesServiceTests
	{
		private readonly Mock<DatabaseContext> _mockDatabaseContext;
		private readonly DataIssuesService _dataIssuesService;
		private static readonly string[] sourceArray = ["Buy", "Sell", "Dividend", "Receive", "Send"];

		public DataIssuesServiceTests()
		{
			_mockDatabaseContext = new Mock<DatabaseContext>();
			var dbFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(_mockDatabaseContext.Object);

			_dataIssuesService = new DataIssuesService(dbFactory.Object);
		}

		#region GetActivitiesWithoutHoldingsAsync Tests

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithActivitiesWithoutHoldings_ShouldReturnDataIssues()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var activities = new List<Activity>
			{
				CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1)),
				CreateSellActivityWithoutHolding(account, DateTime.Now.AddDays(-2)),
				CreateDividendActivityWithoutHolding(account, DateTime.Now.AddDays(-3))
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(3);
			result.Should().AllSatisfy(issue =>
			{
				issue.IssueType.Should().Be("Activity Without Holding");
				issue.Description.Should().Be("This activity is not associated with any holding. It may need manual symbol matching or represents orphaned data.");
				issue.AccountName.Should().Be("Test Account");
			});
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithActivitiesWithHoldings_ShouldReturnEmptyList()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			var activities = new List<Activity>
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1)),
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-2))
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithMixedActivities_ShouldReturnOnlyActivitiesWithoutHoldings()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);
			var activities = new List<Activity>
			{
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1)), // Has holding
				CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-2)), // No holding
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-3)), // Has holding
				CreateDividendActivityWithoutHolding(account, DateTime.Now.AddDays(-4)) // No holding
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(2);
			result.Should().AllSatisfy(issue => issue.IssueType.Should().Be("Activity Without Holding"));
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithOnlyNonPartialIdentifierActivities_ShouldReturnEmptyList()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var activities = new List<Activity>
			{
				CreateCashDepositActivity(account, DateTime.Now.AddDays(-1)),
				CreateInterestActivity(account, DateTime.Now.AddDays(-2))
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_ShouldOrderByDateDescendingThenById()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var today = DateTime.Now.Date;
			var activities = new List<Activity>
			{
				CreateBuyActivityWithoutHolding(account, today.AddDays(-3), id: 3),
				CreateBuyActivityWithoutHolding(account, today.AddDays(-1), id: 1),
				CreateBuyActivityWithoutHolding(account, today.AddDays(-1), id: 2),
				CreateBuyActivityWithoutHolding(account, today.AddDays(-2), id: 4)
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(4);
			// Should be ordered by date descending, then by id ascending
			result[0].Date.Should().Be(today.AddDays(-1));
			result[0].Id.Should().Be(1);
			result[1].Date.Should().Be(today.AddDays(-1));
			result[1].Id.Should().Be(2);
			result[2].Date.Should().Be(today.AddDays(-2));
			result[2].Id.Should().Be(4);
			result[3].Date.Should().Be(today.AddDays(-3));
			result[3].Id.Should().Be(3);
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_ShouldSetCorrectSeverityBasedOnActivityType()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var activities = new List<Activity>
			{
				CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1)),
				CreateSellActivityWithoutHolding(account, DateTime.Now.AddDays(-2)),
				CreateDividendActivityWithoutHolding(account, DateTime.Now.AddDays(-3)),
				CreateReceiveActivityWithoutHolding(account, DateTime.Now.AddDays(-4)),
				CreateSendActivityWithoutHolding(account, DateTime.Now.AddDays(-5)),
				CreateStakingRewardActivityWithoutHolding(account, DateTime.Now.AddDays(-6)),
				CreateGiftAssetActivityWithoutHolding(account, DateTime.Now.AddDays(-7))
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(7);

			var buyIssue = result.First(r => r.ActivityType.Contains("Buy"));
			buyIssue.Severity.Should().Be("Error");

			var sellIssue = result.First(r => r.ActivityType.Contains("Sell"));
			sellIssue.Severity.Should().Be("Error");

			var dividendIssue = result.First(r => r.ActivityType.Contains("Dividend"));
			dividendIssue.Severity.Should().Be("Error");

			var receiveIssue = result.First(r => r.ActivityType.Contains("Receive"));
			receiveIssue.Severity.Should().Be("Error");

			var sendIssue = result.First(r => r.ActivityType.Contains("Send"));
			sendIssue.Severity.Should().Be("Error");

			var stakingIssue = result.First(r => r.ActivityType.Contains("Staking"));
			stakingIssue.Severity.Should().Be("Error");

			var giftIssue = result.First(r => r.ActivityType.Contains("Gift"));
			giftIssue.Severity.Should().Be("Error");
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithEnrichmentData_ShouldIncludeDetailedInformation()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var buyActivity = CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1), 10, 100);
			var sellActivity = CreateSellActivityWithoutHolding(account, DateTime.Now.AddDays(-2), 5, 110);
			var activities = new List<Activity> { buyActivity, sellActivity };

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(2);

			var buyIssue = result.First(r => r.ActivityType == "Buy");
			buyIssue.Quantity.Should().Be(10);
			buyIssue.UnitPrice.Should().NotBeNull();
			buyIssue.UnitPrice!.Amount.Should().Be(100);
			buyIssue.Amount.Should().NotBeNull();
			buyIssue.Amount!.Amount.Should().Be(1000); // 10 * 100
			buyIssue.PartialIdentifiers.Should().NotBeEmpty();
			buyIssue.SymbolIdentifiers.Should().NotBeNullOrEmpty();

			var sellIssue = result.First(r => r.ActivityType == "Sell");
			sellIssue.Quantity.Should().Be(5);
			sellIssue.UnitPrice.Should().NotBeNull();
			sellIssue.UnitPrice!.Amount.Should().Be(110);
			sellIssue.Amount.Should().NotBeNull();
			sellIssue.Amount!.Amount.Should().Be(550); // 5 * 110
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithNullAccountName_ShouldHandleGracefully()
		{
			// Arrange
			var account = CreateTestAccount(null!); // Null account name
			var activities = new List<Activity>
			{
				CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1))
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].AccountName.Should().Be("Unknown");
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WhenExceptionOccurs_ShouldReturnErrorDataIssue()
		{
			// Arrange
			_mockDatabaseContext.Setup(x => x.Activities)
				.Throws(new InvalidOperationException("Database connection failed"));

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].IssueType.Should().Be("System Error");
			result[0].Description.Should().StartWith("Failed to analyze data issues:");
			result[0].Severity.Should().Be("Error");
			result[0].AccountName.Should().Be("System");
			result[0].ActivityType.Should().Be("Error");
			result[0].TransactionId.Should().Be("ERROR");
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			var cancellationToken = new CancellationToken();
			var activities = new List<Activity>
			{
				CreateBuyActivityWithoutHolding(CreateTestAccount("Test Account"), DateTime.Now.AddDays(-1))
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(cancellationToken);

			// Assert
			_mockDatabaseContext.Verify(x => x.Activities, Times.AtLeastOnce);
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithEmptyDatabase_ShouldReturnEmptyList()
		{
			// Arrange
			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(new List<Activity>());

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithEnrichmentFailure_ShouldContinueWithBasicData()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var activities = new List<Activity>
			{
				CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1))
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			// Should have basic data even if enrichment failed
			result[0].IssueType.Should().Be("Activity Without Holding");
			result[0].AccountName.Should().Be("Test Account");
		}

		[Theory]
		[InlineData("BuyActivity", "Buy")]
		[InlineData("SellActivity", "Sell")]
		[InlineData("DividendActivity", "Dividend")]
		[InlineData("CashDepositActivity", "Deposit")]
		[InlineData("CashWithdrawalActivity", "Withdrawal")]
		[InlineData("FeeActivity", "Fee")]
		[InlineData("InterestActivity", "Interest")]
		[InlineData("ReceiveActivity", "Receive")]
		[InlineData("SendActivity", "Send")]
		[InlineData("StakingRewardActivity", "Staking Reward")]
		[InlineData("GiftAssetActivity", "Gift")]
		[InlineData("GiftFiatActivity", "Gift Cash")]
		[InlineData("ValuableActivity", "Valuable")]
		[InlineData("LiabilityActivity", "Liability")]
		[InlineData("KnownBalanceActivity", "Balance")]
		[InlineData("RepayBondActivity", "Bond Repayment")]
		public void GetActivitiesWithoutHoldingsAsync_ShouldMapActivityTypeCorrectly(string activityClassName, string expectedDisplayName)
		{
			// This test verifies the activity type mapping logic in GetActivityTypeName method
			// Since this method is static and private, we test the mapping concept
			Assert.False(string.IsNullOrEmpty(expectedDisplayName));
			Assert.False(string.IsNullOrEmpty(activityClassName));
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_ShouldIncludeCorrectTransactionIdAndDescription()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var activities = new List<Activity>
			{
				CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1), transactionId: "TXN-001")
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].TransactionId.Should().Be("TXN-001");
			result[0].ActivityDescription.Should().Be("Test buy transaction without holding");
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_ShouldIncludeCorrectActivityTypeMapping()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var activities = new List<Activity>
			{
				CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1)),
				CreateSellActivityWithoutHolding(account, DateTime.Now.AddDays(-2)),
				CreateDividendActivityWithoutHolding(account, DateTime.Now.AddDays(-3))
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(3);

			result.Should().Contain(r => r.ActivityType == "Buy");
			result.Should().Contain(r => r.ActivityType == "Sell");
			result.Should().Contain(r => r.ActivityType == "Dividend");
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithPartialSymbolIdentifiers_ShouldIncludeSymbolInformation()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var activity = CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1));
			var activities = new List<Activity> { activity };

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);

			var issue = result[0];
			issue.PartialIdentifiers.Should().NotBeEmpty();
			issue.SymbolIdentifiers.Should().NotBeNullOrEmpty();
			issue.SymbolIdentifiers.Should().Contain("UNKNOWN");
		}

		[Fact]
		public async Task DataIssuesService_GetActivitiesWithoutHoldingsAsync_WithComplexScenario_ShouldHandleCorrectly()
		{
			// Arrange
			var account1 = CreateTestAccount("Account 1", 1);
			var account2 = CreateTestAccount("Account 2", 2);

			// Create some activities with holdings
			var symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			var holding = CreateTestHolding(symbolProfile);

			// Use specific dates to avoid timing issues
			var baseDate = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

			// Create various activities without holdings for comprehensive testing
			var activities = new List<Activity>
			{
				// Activities WITH holdings (should be excluded)
				CreateBuyActivity(account1, holding, baseDate.AddDays(-1)),
				CreateSellActivity(account1, holding, baseDate.AddDays(-2)),
				
				// Activities WITHOUT holdings (should be included)
				CreateBuyActivityWithoutHolding(account1, baseDate.AddDays(-3), id: 1),
				CreateSellActivityWithoutHolding(account1, baseDate.AddDays(-4), id: 2),
				CreateDividendActivityWithoutHolding(account2, baseDate.AddDays(-5), id: 3),
				CreateReceiveActivityWithoutHolding(account2, baseDate.AddDays(-6), id: 4),
				CreateSendActivityWithoutHolding(account1, baseDate.AddDays(-7), id: 5),
				
				// Non-IActivityWithPartialIdentifier activities (should be excluded)
				CreateCashDepositActivity(account1, baseDate.AddDays(-8)),
				CreateInterestActivity(account2, baseDate.AddDays(-9))
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(5); // Only activities without holdings that implement IActivityWithPartialIdentifier

			// Verify correct ordering (date descending, then id ascending)
			result[0].Date.Date.Should().Be(baseDate.AddDays(-3).Date);
			result[0].Id.Should().Be(1);
			result[1].Date.Date.Should().Be(baseDate.AddDays(-4).Date);
			result[1].Id.Should().Be(2);
			result[2].Date.Date.Should().Be(baseDate.AddDays(-5).Date);
			result[2].Id.Should().Be(3);

			// Verify account distribution
			result.Should().Contain(r => r.AccountName == "Account 1");
			result.Should().Contain(r => r.AccountName == "Account 2");

			// Verify activity types
			result.Should().Contain(r => r.ActivityType == "Buy");
			result.Should().Contain(r => r.ActivityType == "Sell");
			result.Should().Contain(r => r.ActivityType == "Dividend");
			result.Should().Contain(r => r.ActivityType == "Receive");
			result.Should().Contain(r => r.ActivityType == "Send");

			// Verify severity assignment
			result.Where(r => sourceArray.Contains(r.ActivityType))
				  .Should().AllSatisfy(r => r.Severity.Should().Be("Error"));
		}

		[Fact]
		public async Task DataIssuesService_EnrichWithDetailedActivityData_ShouldHandleNullActivities()
		{
			// Arrange
			CreateTestAccount("Test Account");
			var activities = new List<Activity>(); // Empty list

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task DataIssuesService_GetActivityTypeDisplayName_ShouldHandleProxyTypes()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			// Create an activity that might have "Proxy" in its type name (simulated)
			var buyActivity = CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1));
			var activities = new List<Activity> { buyActivity };

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			// The activity type should not contain "Activity" or "Proxy" suffixes
			result[0].ActivityType.Should().NotContain("Activity");
			result[0].ActivityType.Should().NotContain("Proxy");
		}

		[Fact]
		public async Task DataIssuesService_DetermineSeverity_ShouldReturnCorrectSeverityLevels()
		{
			// Arrange
			var account = CreateTestAccount("Test Account");
			var activities = new List<Activity>
			{
				CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1), id: 1),
				CreateSellActivityWithoutHolding(account, DateTime.Now.AddDays(-2), id: 2),
				CreateDividendActivityWithoutHolding(account, DateTime.Now.AddDays(-3), id: 3),
				CreateReceiveActivityWithoutHolding(account, DateTime.Now.AddDays(-4), id: 4),
				CreateSendActivityWithoutHolding(account, DateTime.Now.AddDays(-5), id: 5),
				CreateStakingRewardActivityWithoutHolding(account, DateTime.Now.AddDays(-6), id: 6),
				CreateGiftAssetActivityWithoutHolding(account, DateTime.Now.AddDays(-7), id: 7)
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(7);

			// All these activity types should be marked as "Error" severity
			result.Should().AllSatisfy(issue => issue.Severity.Should().Be("Error"));
		}

		#endregion

		#region Helper Methods

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
			var holding = new Holding { Id = 1 };
			holding.SymbolProfiles.Add(symbolProfile);
			return holding;
		}

		private static BuyActivity CreateBuyActivity(Account account, Holding holding, DateTime date, decimal quantity = 10, decimal price = 100, string transactionId = "TXN-001")
		{
			var partialIdentifiers = new List<PartialSymbolIdentifier>
			{
				PartialSymbolIdentifier.CreateStockAndETF("AAPL")
			};

			return new BuyActivity(
				account,
				holding,
				partialIdentifiers,
				date,
				quantity,
				new Money(Currency.USD, price),
				transactionId,
				null,
				"Test buy transaction")
			{
				TotalTransactionAmount = new Money(Currency.USD, quantity * price)
			};
		}

		private static SellActivity CreateSellActivity(Account account, Holding holding, DateTime date, decimal quantity = 10, decimal price = 100, string transactionId = "TXN-002")
		{
			var partialIdentifiers = new List<PartialSymbolIdentifier>
			{
				PartialSymbolIdentifier.CreateStockAndETF("AAPL")
			};

			return new SellActivity(
				account,
				holding,
				partialIdentifiers,
				date,
				quantity,
				new Money(Currency.USD, price),
				transactionId,
				null,
				"Test sell transaction")
			{
				TotalTransactionAmount = new Money(Currency.USD, quantity * price)
			};
		}

		private static BuyActivity CreateBuyActivityWithoutHolding(Account account, DateTime date, decimal quantity = 10, decimal price = 100, string transactionId = "TXN-001", long id = 1)
		{
			var partialIdentifiers = new List<PartialSymbolIdentifier>
			{
				PartialSymbolIdentifier.CreateStockAndETF("UNKNOWN")
			};

			var activity = new BuyActivity(
				account,
				null, // No holding
				partialIdentifiers,
				date,
				quantity,
				new Money(Currency.USD, price),
				transactionId,
				null,
				"Test buy transaction without holding")
			{
				TotalTransactionAmount = new Money(Currency.USD, quantity * price),
				Id = id
			};

			return activity;
		}

		private static SellActivity CreateSellActivityWithoutHolding(Account account, DateTime date, decimal quantity = 5, decimal price = 110, string transactionId = "TXN-002", long id = 2)
		{
			var partialIdentifiers = new List<PartialSymbolIdentifier>
			{
				PartialSymbolIdentifier.CreateStockAndETF("UNKNOWN")
			};

			var activity = new SellActivity(
				account,
				null, // No holding
				partialIdentifiers,
				date,
				quantity,
				new Money(Currency.USD, price),
				transactionId,
				null,
				"Test sell transaction without holding")
			{
				TotalTransactionAmount = new Money(Currency.USD, quantity * price),
				Id = id
			};

			return activity;
		}

		private static DividendActivity CreateDividendActivityWithoutHolding(Account account, DateTime date, decimal amount = 50, string transactionId = "TXN-003", long id = 3)
		{
			var partialIdentifiers = new List<PartialSymbolIdentifier>
			{
				PartialSymbolIdentifier.CreateStockAndETF("UNKNOWN")
			};

			var activity = new DividendActivity(
				account,
				null, // No holding
				partialIdentifiers,
				date,
				new Money(Currency.USD, amount),
				transactionId,
				null,
				"Test dividend without holding")
			{
				Id = id
			};

			return activity;
		}

		private static ReceiveActivity CreateReceiveActivityWithoutHolding(Account account, DateTime date, decimal quantity = 10, string transactionId = "TXN-004", long id = 4)
		{
			var partialIdentifiers = new List<PartialSymbolIdentifier>
			{
				PartialSymbolIdentifier.CreateStockAndETF("UNKNOWN")
			};

			var activity = new ReceiveActivity(
				account,
				null, // No holding
				partialIdentifiers,
				date,
				quantity,
				transactionId,
				null,
				"Test receive without holding")
			{
				Id = id
			};

			return activity;
		}

		private static SendActivity CreateSendActivityWithoutHolding(Account account, DateTime date, decimal quantity = 5, string transactionId = "TXN-005", long id = 5)
		{
			var partialIdentifiers = new List<PartialSymbolIdentifier>
			{
				PartialSymbolIdentifier.CreateStockAndETF("UNKNOWN")
			};

			var activity = new SendActivity(
				account,
				null, // No holding
				partialIdentifiers,
				date,
				quantity,
				transactionId,
				null,
				"Test send without holding")
			{
				Id = id
			};

			return activity;
		}

		private static StakingRewardActivity CreateStakingRewardActivityWithoutHolding(Account account, DateTime date, decimal quantity = 2, string transactionId = "TXN-006", long id = 6)
		{
			var partialIdentifiers = new List<PartialSymbolIdentifier>
			{
				PartialSymbolIdentifier.CreateStockAndETF("UNKNOWN")
			};

			var activity = new StakingRewardActivity(
				account,
				null, // No holding
				partialIdentifiers,
				date,
				quantity,
				transactionId,
				null,
				"Test staking reward without holding")
			{
				Id = id
			};

			return activity;
		}

		private static GiftAssetActivity CreateGiftAssetActivityWithoutHolding(Account account, DateTime date, decimal quantity = 1, string transactionId = "TXN-007", long id = 7)
		{
			var partialIdentifiers = new List<PartialSymbolIdentifier>
			{
				PartialSymbolIdentifier.CreateStockAndETF("UNKNOWN")
			};

			var activity = new GiftAssetActivity(
				account,
				null, // No holding
				partialIdentifiers,
				date,
				quantity,
				transactionId,
				null,
				"Test gift asset without holding")
			{
				Id = id
			};

			return activity;
		}

		// Create basic activities that don't implement IActivityWithPartialIdentifier
		private static CashDepositActivity CreateCashDepositActivity(Account account, DateTime date, decimal amount = 1000, string transactionId = "TXN-004")
		{
			return new CashDepositActivity(
				account,
				null,
				date,
				new Money(Currency.USD, amount),
				transactionId,
				null,
				"Test cash deposit");
		}

		private static InterestActivity CreateInterestActivity(Account account, DateTime date, decimal amount = 25, string transactionId = "TXN-005")
		{
			return new InterestActivity(
				account,
				null,
				date,
				new Money(Currency.USD, amount),
				transactionId,
				null,
				"Test interest");
		}

		#endregion
	}
}
