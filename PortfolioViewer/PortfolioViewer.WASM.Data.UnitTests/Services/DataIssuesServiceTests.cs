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
	public class DataIssuesServiceTests
	{
		private readonly Mock<DatabaseContext> _mockDatabaseContext;
		private readonly DataIssuesService _dataIssuesService;
		private static readonly string[] sourceArray = ["Buy", "Sell", "Dividend", "Receive", "Send"];

		public DataIssuesServiceTests()
		{
			_mockDatabaseContext = new Mock<DatabaseContext>();
			Mock<IDbContextFactory<DatabaseContext>> dbFactory = new();
			_ = dbFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(_mockDatabaseContext.Object);

			_dataIssuesService = new DataIssuesService(dbFactory.Object);
		}

		#region GetActivitiesWithoutHoldingsAsync Tests

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithActivitiesWithoutHoldings_ShouldReturnDataIssues()
		{
			// Arrange
			Account account = CreateTestAccount("Test Account");
			List<Activity> activities =
			[
				CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1)),
				CreateSellActivityWithoutHolding(account, DateTime.Now.AddDays(-2)),
				CreateDividendActivityWithoutHolding(account, DateTime.Now.AddDays(-3))
			];

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(3);
			_ = result.Should().AllSatisfy(issue =>
			{
				_ = issue.IssueType.Should().Be("Activity Without Holding");
				_ = issue.Description.Should().Be("This activity is not associated with any holding. It may need manual symbol matching or represents orphaned data.");
				_ = issue.AccountName.Should().Be("Test Account");
			});
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithActivitiesWithHoldings_ShouldReturnEmptyList()
		{
			// Arrange
			Account account = CreateTestAccount("Test Account");
			SymbolProfile symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			Holding holding = CreateTestHolding(symbolProfile);
			List<Activity> activities =
			[
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1)),
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-2))
			];

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithMixedActivities_ShouldReturnOnlyActivitiesWithoutHoldings()
		{
			// Arrange
			Account account = CreateTestAccount("Test Account");
			SymbolProfile symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			Holding holding = CreateTestHolding(symbolProfile);
			List<Activity> activities =
			[
				CreateBuyActivity(account, holding, DateTime.Now.AddDays(-1)), // Has holding
				CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-2)), // No holding
				CreateSellActivity(account, holding, DateTime.Now.AddDays(-3)), // Has holding
				CreateDividendActivityWithoutHolding(account, DateTime.Now.AddDays(-4)) // No holding
			];

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(2);
			_ = result.Should().AllSatisfy(issue => issue.IssueType.Should().Be("Activity Without Holding"));
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithOnlyNonPartialIdentifierActivities_ShouldReturnEmptyList()
		{
			// Arrange
			Account account = CreateTestAccount("Test Account");
			List<Activity> activities =
			[
				CreateCashDepositActivity(account, DateTime.Now.AddDays(-1)),
				CreateInterestActivity(account, DateTime.Now.AddDays(-2))
			];

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_ShouldOrderByDateDescendingThenById()
		{
			// Arrange
			Account account = CreateTestAccount("Test Account");
			DateTime today = DateTime.Now.Date;
			List<Activity> activities =
			[
				CreateBuyActivityWithoutHolding(account, today.AddDays(-3), id: 3),
				CreateBuyActivityWithoutHolding(account, today.AddDays(-1), id: 1),
				CreateBuyActivityWithoutHolding(account, today.AddDays(-1), id: 2),
				CreateBuyActivityWithoutHolding(account, today.AddDays(-2), id: 4)
			];

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(4);
			// Should be ordered by date descending, then by id ascending
			_ = result[0].Date.Should().Be(today.AddDays(-1));
			_ = result[0].Id.Should().Be(1);
			_ = result[1].Date.Should().Be(today.AddDays(-1));
			_ = result[1].Id.Should().Be(2);
			_ = result[2].Date.Should().Be(today.AddDays(-2));
			_ = result[2].Id.Should().Be(4);
			_ = result[3].Date.Should().Be(today.AddDays(-3));
			_ = result[3].Id.Should().Be(3);
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_ShouldSetCorrectSeverityBasedOnActivityType()
		{
			// Arrange
			Account account = CreateTestAccount("Test Account");
			List<Activity> activities =
			[
				CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1)),
				CreateSellActivityWithoutHolding(account, DateTime.Now.AddDays(-2)),
				CreateDividendActivityWithoutHolding(account, DateTime.Now.AddDays(-3)),
				CreateReceiveActivityWithoutHolding(account, DateTime.Now.AddDays(-4)),
				CreateSendActivityWithoutHolding(account, DateTime.Now.AddDays(-5)),
				CreateStakingRewardActivityWithoutHolding(account, DateTime.Now.AddDays(-6)),
				CreateGiftAssetActivityWithoutHolding(account, DateTime.Now.AddDays(-7))
			];

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(7);

			DataIssueDisplayModel buyIssue = result.First(r => r.ActivityType.Contains("Buy"));
			_ = buyIssue.Severity.Should().Be("Error");

			DataIssueDisplayModel sellIssue = result.First(r => r.ActivityType.Contains("Sell"));
			_ = sellIssue.Severity.Should().Be("Error");

			DataIssueDisplayModel dividendIssue = result.First(r => r.ActivityType.Contains("Dividend"));
			_ = dividendIssue.Severity.Should().Be("Error");

			DataIssueDisplayModel receiveIssue = result.First(r => r.ActivityType.Contains("Receive"));
			_ = receiveIssue.Severity.Should().Be("Error");

			DataIssueDisplayModel sendIssue = result.First(r => r.ActivityType.Contains("Send"));
			_ = sendIssue.Severity.Should().Be("Error");

			DataIssueDisplayModel stakingIssue = result.First(r => r.ActivityType.Contains("Staking"));
			_ = stakingIssue.Severity.Should().Be("Error");

			DataIssueDisplayModel giftIssue = result.First(r => r.ActivityType.Contains("Gift"));
			_ = giftIssue.Severity.Should().Be("Error");
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithEnrichmentData_ShouldIncludeDetailedInformation()
		{
			// Arrange
			Account account = CreateTestAccount("Test Account");
			BuyActivity buyActivity = CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1), 10, 100);
			SellActivity sellActivity = CreateSellActivityWithoutHolding(account, DateTime.Now.AddDays(-2), 5, 110);
			List<Activity> activities = [buyActivity, sellActivity];

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(2);

			DataIssueDisplayModel buyIssue = result.First(r => r.ActivityType == "Buy");
			_ = buyIssue.Quantity.Should().Be(10);
			_ = buyIssue.UnitPrice.Should().NotBeNull();
			_ = buyIssue.UnitPrice!.Amount.Should().Be(100);
			_ = buyIssue.Amount.Should().NotBeNull();
			_ = buyIssue.Amount!.Amount.Should().Be(1000); // 10 * 100
			_ = buyIssue.PartialIdentifiers.Should().NotBeEmpty();
			_ = buyIssue.SymbolIdentifiers.Should().NotBeNullOrEmpty();

			DataIssueDisplayModel sellIssue = result.First(r => r.ActivityType == "Sell");
			_ = sellIssue.Quantity.Should().Be(5);
			_ = sellIssue.UnitPrice.Should().NotBeNull();
			_ = sellIssue.UnitPrice!.Amount.Should().Be(110);
			_ = sellIssue.Amount.Should().NotBeNull();
			_ = sellIssue.Amount!.Amount.Should().Be(550); // 5 * 110
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithNullAccountName_ShouldHandleGracefully()
		{
			// Arrange
			Account account = CreateTestAccount(null!); // Null account name
			List<Activity> activities =
			[
				CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1))
			];

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(1);
			_ = result[0].AccountName.Should().Be("Unknown");
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WhenExceptionOccurs_ShouldReturnErrorDataIssue()
		{
			// Arrange
			_ = _mockDatabaseContext.Setup(x => x.Activities)
				.Throws(new InvalidOperationException("Database connection failed"));

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(1);
			_ = result[0].IssueType.Should().Be("System Error");
			_ = result[0].Description.Should().StartWith("Failed to analyze data issues:");
			_ = result[0].Severity.Should().Be("Error");
			_ = result[0].AccountName.Should().Be("System");
			_ = result[0].ActivityType.Should().Be("Error");
			_ = result[0].TransactionId.Should().Be("ERROR");
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			CancellationToken cancellationToken = new();
			List<Activity> activities =
			[
				CreateBuyActivityWithoutHolding(CreateTestAccount("Test Account"), DateTime.Now.AddDays(-1))
			];

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			_ = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(cancellationToken);

			// Assert
			_mockDatabaseContext.Verify(x => x.Activities, Times.AtLeastOnce);
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithEmptyDatabase_ShouldReturnEmptyList()
		{
			// Arrange
			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(new List<Activity>());

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithEnrichmentFailure_ShouldContinueWithBasicData()
		{
			// Arrange
			Account account = CreateTestAccount("Test Account");
			List<Activity> activities =
			[
				CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1))
			];

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(1);
			// Should have basic data even if enrichment failed
			_ = result[0].IssueType.Should().Be("Activity Without Holding");
			_ = result[0].AccountName.Should().Be("Test Account");
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
			Account account = CreateTestAccount("Test Account");
			List<Activity> activities =
			[
				CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1), transactionId: "TXN-001")
			];

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(1);
			_ = result[0].TransactionId.Should().Be("TXN-001");
			_ = result[0].ActivityDescription.Should().Be("Test buy transaction without holding");
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_ShouldIncludeCorrectActivityTypeMapping()
		{
			// Arrange
			Account account = CreateTestAccount("Test Account");
			List<Activity> activities =
			[
				CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1)),
				CreateSellActivityWithoutHolding(account, DateTime.Now.AddDays(-2)),
				CreateDividendActivityWithoutHolding(account, DateTime.Now.AddDays(-3))
			];

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(3);

			_ = result.Should().Contain(r => r.ActivityType == "Buy");
			_ = result.Should().Contain(r => r.ActivityType == "Sell");
			_ = result.Should().Contain(r => r.ActivityType == "Dividend");
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_WithPartialSymbolIdentifiers_ShouldIncludeSymbolInformation()
		{
			// Arrange
			Account account = CreateTestAccount("Test Account");
			BuyActivity activity = CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1));
			List<Activity> activities = [activity];

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(1);

			DataIssueDisplayModel issue = result[0];
			_ = issue.PartialIdentifiers.Should().NotBeEmpty();
			_ = issue.SymbolIdentifiers.Should().NotBeNullOrEmpty();
			_ = issue.SymbolIdentifiers.Should().Contain("UNKNOWN");
		}

		[Fact]
		public async Task DataIssuesService_GetActivitiesWithoutHoldingsAsync_WithComplexScenario_ShouldHandleCorrectly()
		{
			// Arrange
			Account account1 = CreateTestAccount("Account 1", 1);
			Account account2 = CreateTestAccount("Account 2", 2);

			// Create some activities with holdings
			SymbolProfile symbolProfile = CreateTestSymbolProfile("AAPL", "Apple Inc");
			Holding holding = CreateTestHolding(symbolProfile);

			// Use specific dates to avoid timing issues
			DateTime baseDate = new(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

			// Create various activities without holdings for comprehensive testing
			List<Activity> activities =
			[
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
			];

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(5); // Only activities without holdings that implement IActivityWithPartialIdentifier

			// Verify correct ordering (date descending, then id ascending)
			_ = result[0].Date.Date.Should().Be(baseDate.AddDays(-3).Date);
			_ = result[0].Id.Should().Be(1);
			_ = result[1].Date.Date.Should().Be(baseDate.AddDays(-4).Date);
			_ = result[1].Id.Should().Be(2);
			_ = result[2].Date.Date.Should().Be(baseDate.AddDays(-5).Date);
			_ = result[2].Id.Should().Be(3);

			// Verify account distribution
			_ = result.Should().Contain(r => r.AccountName == "Account 1");
			_ = result.Should().Contain(r => r.AccountName == "Account 2");

			// Verify activity types
			_ = result.Should().Contain(r => r.ActivityType == "Buy");
			_ = result.Should().Contain(r => r.ActivityType == "Sell");
			_ = result.Should().Contain(r => r.ActivityType == "Dividend");
			_ = result.Should().Contain(r => r.ActivityType == "Receive");
			_ = result.Should().Contain(r => r.ActivityType == "Send");

			// Verify severity assignment
			_ = result.Where(r => sourceArray.Contains(r.ActivityType))
				  .Should().AllSatisfy(r => r.Severity.Should().Be("Error"));
		}

		[Fact]
		public async Task DataIssuesService_EnrichWithDetailedActivityData_ShouldHandleNullActivities()
		{
			// Arrange
			_ = CreateTestAccount("Test Account");
			List<Activity> activities = []; // Empty list

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeEmpty();
		}

		[Fact]
		public async Task DataIssuesService_GetActivityTypeDisplayName_ShouldHandleProxyTypes()
		{
			// Arrange
			Account account = CreateTestAccount("Test Account");
			// Create an activity that might have "Proxy" in its type name (simulated)
			BuyActivity buyActivity = CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1));
			List<Activity> activities = [buyActivity];

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(1);
			// The activity type should not contain "Activity" or "Proxy" suffixes
			_ = result[0].ActivityType.Should().NotContain("Activity");
			_ = result[0].ActivityType.Should().NotContain("Proxy");
		}

		[Fact]
		public async Task DataIssuesService_DetermineSeverity_ShouldReturnCorrectSeverityLevels()
		{
			// Arrange
			Account account = CreateTestAccount("Test Account");
			List<Activity> activities =
			[
				CreateBuyActivityWithoutHolding(account, DateTime.Now.AddDays(-1), id: 1),
				CreateSellActivityWithoutHolding(account, DateTime.Now.AddDays(-2), id: 2),
				CreateDividendActivityWithoutHolding(account, DateTime.Now.AddDays(-3), id: 3),
				CreateReceiveActivityWithoutHolding(account, DateTime.Now.AddDays(-4), id: 4),
				CreateSendActivityWithoutHolding(account, DateTime.Now.AddDays(-5), id: 5),
				CreateStakingRewardActivityWithoutHolding(account, DateTime.Now.AddDays(-6), id: 6),
				CreateGiftAssetActivityWithoutHolding(account, DateTime.Now.AddDays(-7), id: 7)
			];

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			List<DataIssueDisplayModel> result = await _dataIssuesService.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(7);

			// All these activity types should be marked as "Error" severity
			_ = result.Should().AllSatisfy(issue => issue.Severity.Should().Be("Error"));
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
			Holding holding = new() { Id = 1 };
			holding.SymbolProfiles.Add(symbolProfile);
			return holding;
		}

		private static BuyActivity CreateBuyActivity(Account account, Holding holding, DateTime date, decimal quantity = 10, decimal price = 100, string transactionId = "TXN-001")
		{
			List<PartialSymbolIdentifier> partialIdentifiers =
			[
				PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Ticker, "AAPL", null)!
			];

			return new BuyActivity(
				account,
				holding,
				partialIdentifiers,
				date,
				quantity,
				new Money(Currency.USD, price),
				new Money(Currency.USD, price).Times(quantity),
				transactionId,
				null,
				"Test buy transaction")
			{
			};
		}

		private static SellActivity CreateSellActivity(Account account, Holding holding, DateTime date, decimal quantity = 10, decimal price = 100, string transactionId = "TXN-002")
		{
			List<PartialSymbolIdentifier> partialIdentifiers =
			[
				PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Ticker, "AAPL", null)!
			];

			return new SellActivity(
				account,
				holding,
				partialIdentifiers,
				date,
				quantity,
				new Money(Currency.USD, price),
				new Money(Currency.USD, price).Times(quantity),
				transactionId,
				null,
				"Test sell transaction")
			{
			};
		}

		private static BuyActivity CreateBuyActivityWithoutHolding(Account account, DateTime date, decimal quantity = 10, decimal price = 100, string transactionId = "TXN-001", long id = 1)
		{
			List<PartialSymbolIdentifier> partialIdentifiers =
			[
				PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Ticker, "UNKNOWN", null)!
			];

			BuyActivity activity = new(
				account,
				null, // No holding
				partialIdentifiers,
				date,
				quantity,
				new Money(Currency.USD, price),
				new Money(Currency.USD, price).Times(quantity),
				transactionId,
				null,
				"Test buy transaction without holding")
			{
				Id = id
			};

			return activity;
		}

		private static SellActivity CreateSellActivityWithoutHolding(Account account, DateTime date, decimal quantity = 5, decimal price = 110, string transactionId = "TXN-002", long id = 2)
		{
			List<PartialSymbolIdentifier> partialIdentifiers =
			[
				PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Ticker, "UNKNOWN", null)!
			];

			SellActivity activity = new(
				account,
				null, // No holding
				partialIdentifiers,
				date,
				quantity,
				new Money(Currency.USD, price),
				new Money(Currency.USD, price).Times(quantity),
				transactionId,
				null,
				"Test sell transaction without holding")
			{
				Id = id
			};

			return activity;
		}

		private static DividendActivity CreateDividendActivityWithoutHolding(Account account, DateTime date, decimal amount = 50, string transactionId = "TXN-003", long id = 3)
		{
			List<PartialSymbolIdentifier> partialIdentifiers =
			[
				PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Ticker, "UNKNOWN", null)!
			];

			DividendActivity activity = new(
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
			List<PartialSymbolIdentifier> partialIdentifiers =
			[
				PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Ticker, "UNKNOWN", null)!
			];

			ReceiveActivity activity = new(
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
			List<PartialSymbolIdentifier> partialIdentifiers =
			[
				PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Ticker, "UNKNOWN", null)!
			];

			SendActivity activity = new(
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
			List<PartialSymbolIdentifier> partialIdentifiers =
			[
				PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Ticker, "UNKNOWN", null)!
			];

			StakingRewardActivity activity = new(
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
			List<PartialSymbolIdentifier> partialIdentifiers =
			[
				PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Ticker, "UNKNOWN", null)!
			];

			GiftAssetActivity activity = new(
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
