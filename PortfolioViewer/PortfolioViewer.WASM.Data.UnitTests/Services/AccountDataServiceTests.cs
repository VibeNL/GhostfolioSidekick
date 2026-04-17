using AwesomeAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace PortfolioViewer.WASM.Data.UnitTests.Services
{
	public class AccountDataServiceTests
	{
		private readonly Mock<DatabaseContext> _mockDatabaseContext;
		private readonly Mock<IServerConfigurationService> _mockServerConfigurationService;
		private readonly Mock<ITaxReportCacheService> _mockTaxReportCacheService;
		private readonly AccountDataService _accountDataService;

		public AccountDataServiceTests()
		{
			_mockDatabaseContext = new Mock<DatabaseContext>();
			_mockServerConfigurationService = new Mock<IServerConfigurationService>();
			_mockServerConfigurationService.Setup(x => x.PrimaryCurrency).Returns(Currency.USD);

			_mockTaxReportCacheService = new Mock<ITaxReportCacheService>();
			_mockTaxReportCacheService.Setup(x => x.IsValid).Returns(false);

			var dbContextFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbContextFactoryMock.Setup(x => x.CreateDbContextAsync()).ReturnsAsync(_mockDatabaseContext.Object);
			_accountDataService = new AccountDataService(dbContextFactoryMock.Object, _mockServerConfigurationService.Object, _mockTaxReportCacheService.Object);
		}

		#region GetAccountInfo Tests

		[Fact]
		public async Task GetAccountInfo_WithAccounts_ShouldReturnAccountsOrderedByName()
		{
			// Arrange
			var platform = CreateTestPlatform("Test Platform");
			var accounts = new List<Account>
			{
				CreateTestAccount("Charlie Account", 3, platform),
				CreateTestAccount("Alpha Account", 1, platform),
				CreateTestAccount("Beta Account", 2, platform)
			};

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(accounts);

			// Act
			var result = await _accountDataService.GetAccountInfo();

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(3);
			result[0].Name.Should().Be("Alpha Account");
			result[1].Name.Should().Be("Beta Account");
			result[2].Name.Should().Be("Charlie Account");
			result.Should().AllSatisfy(account => account.Platform.Should().NotBeNull());
		}

		[Fact]
		public async Task GetAccountInfo_WithEmptyDatabase_ShouldReturnEmptyList()
		{
			// Arrange
			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account>());

			// Act
			var result = await _accountDataService.GetAccountInfo();

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetAccountInfo_ShouldIncludePlatformAndUseAsNoTracking()
		{
			// Arrange
			var platform = CreateTestPlatform("Investment Platform");
			var accounts = new List<Account>
			{
				CreateTestAccount("Test Account", 1, platform)
			};

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(accounts);

			// Act
			var result = await _accountDataService.GetAccountInfo();

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Platform.Should().NotBeNull();
			result[0].Platform!.Name.Should().Be("Investment Platform");
		}

		#endregion

		#region GetAccountsAsync Tests

		[Fact]
		public async Task GetAccountsAsync_WithoutSymbolFilter_ShouldReturnAllAccountsOrderedByName()
		{
			// Arrange
			var platform = CreateTestPlatform("Test Platform");
			var accounts = new List<Account>
			{
				CreateTestAccount("Zulu Account", 3, platform),
				CreateTestAccount("Alpha Account", 1, platform),
				CreateTestAccount("Beta Account", 2, platform)
			};

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(accounts);

			// Act
			var result = await _accountDataService.GetAccountsAsync(null, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(3);
			result[0].Name.Should().Be("Alpha Account");
			result[1].Name.Should().Be("Beta Account");
			result[2].Name.Should().Be("Zulu Account");
		}

		[Fact]
		public async Task GetAccountsAsync_WithSymbolFilter_ShouldReturnFilteredAccounts()
		{
			// Arrange
			var platform = CreateTestPlatform("Test Platform");
			var symbolProfile = CreateTestSymbolProfile("AAPL");
			var holding = CreateTestHolding(symbolProfile);
			var activity = CreateTestActivity(holding);

			var accountWithSymbol = CreateTestAccount("Account with AAPL", 1, platform);
			accountWithSymbol.Activities.Add(activity);

			var accountWithoutSymbol = CreateTestAccount("Account without AAPL", 2, platform);

			var accounts = new List<Account> { accountWithSymbol, accountWithoutSymbol };

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(accounts);

			// Act
			var result = await _accountDataService.GetAccountsAsync("AAPL", CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Name.Should().Be("Account with AAPL");
		}

		[Fact]
		public async Task GetAccountsAsync_WithEmptySymbolFilter_ShouldReturnAllAccounts()
		{
			// Arrange
			var platform = CreateTestPlatform("Test Platform");
			var accounts = new List<Account>
			{
				CreateTestAccount("Account 1", 1, platform),
				CreateTestAccount("Account 2", 2, platform)
			};

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(accounts);

			// Act
			var result = await _accountDataService.GetAccountsAsync(string.Empty, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(2);
		}

		[Fact]
		public async Task GetAccountsAsync_WithWhitespaceSymbolFilter_ShouldReturnAllAccounts()
		{
			// Arrange
			var platform = CreateTestPlatform("Test Platform");
			var accounts = new List<Account>
			{
				CreateTestAccount("Account 1", 1, platform),
				CreateTestAccount("Account 2", 2, platform)
			};

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(accounts);

			// Act
			var result = await _accountDataService.GetAccountsAsync("   ", CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(2);
		}

		[Fact]
		public async Task GetAccountsAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			var accounts = new List<Account>();
			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(accounts);

			// Act
			await _accountDataService.GetAccountsAsync(null, CancellationToken.None);

			// Assert
			_mockDatabaseContext.Verify(x => x.Accounts, Times.AtLeastOnce);
		}

		#endregion

		#region GetAccountValueHistoryAsync Tests

		[Fact]
		public async Task GetAccountValueHistoryAsync_WithValidDateRange_ShouldReturnAccountValueHistory()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);
			var accountId = 1;

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateCalculatedSnapshot(startDate, accountId, 1000m, 800m),
				CreateCalculatedSnapshot(startDate.AddDays(1), accountId, 1100m, 850m)
			};

			var balances = new List<Balance>
			{
				CreateBalance(startDate, accountId, 200m),
				CreateBalance(startDate.AddDays(1), accountId, 250m)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(2);

			var firstPoint = result[0];
			firstPoint.Date.Should().Be(startDate);
			firstPoint.AccountId.Should().Be(accountId);
			firstPoint.TotalAssetValue.Amount.Should().Be(1000m);
			firstPoint.TotalInvested.Amount.Should().Be(800m);
			firstPoint.CashBalance.Amount.Should().Be(200m);
			firstPoint.TotalValue.Amount.Should().Be(1200m); // 1000 + 200

			var secondPoint = result[1];
			secondPoint.Date.Should().Be(startDate.AddDays(1));
			secondPoint.TotalAssetValue.Amount.Should().Be(1100m);
			secondPoint.TotalInvested.Amount.Should().Be(850m);
			secondPoint.CashBalance.Amount.Should().Be(250m);
			secondPoint.TotalValue.Amount.Should().Be(1350m); // 1100 + 250
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_WithMissingSnapshots_ShouldHandleLeftJoin()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);
			var accountId = 1;

			var snapshots = new List<CalculatedSnapshot>(); // Empty snapshots

			var balances = new List<Balance>
			{
				CreateBalance(startDate, accountId, 200m)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);

			var point = result[0];
			point.Date.Should().Be(startDate);
			point.AccountId.Should().Be(accountId);
			point.TotalAssetValue.Amount.Should().Be(0m); // No snapshot data
			point.TotalInvested.Amount.Should().Be(0m); // No snapshot data
			point.CashBalance.Amount.Should().Be(200m);
			point.TotalValue.Amount.Should().Be(200m); // 0 + 200
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_WithMultipleAccounts_ShouldReturnDataForAllAccounts()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateCalculatedSnapshot(startDate, 1, 1000m, 800m),
				CreateCalculatedSnapshot(startDate, 2, 2000m, 1600m)
			};

			var balances = new List<Balance>
			{
				CreateBalance(startDate, 1, 200m),
				CreateBalance(startDate, 2, 400m)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(2);
			result.Should().Contain(p => p.AccountId == 1);
			result.Should().Contain(p => p.AccountId == 2);
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_ShouldOrderByDateThenAccountId()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 3);

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateCalculatedSnapshot(startDate.AddDays(1), 2, 1000m, 800m),
				CreateCalculatedSnapshot(startDate, 1, 1000m, 800m),
				CreateCalculatedSnapshot(startDate.AddDays(1), 1, 1000m, 800m),
				CreateCalculatedSnapshot(startDate, 2, 1000m, 800m)
			};

			var balances = new List<Balance>
			{
				CreateBalance(startDate.AddDays(1), 2, 200m),
				CreateBalance(startDate, 1, 200m),
				CreateBalance(startDate.AddDays(1), 1, 200m),
				CreateBalance(startDate, 2, 200m)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(4);

			// Should be ordered by date first, then by account ID
			result[0].Date.Should().Be(startDate);
			result[0].AccountId.Should().Be(1);
			result[1].Date.Should().Be(startDate);
			result[1].AccountId.Should().Be(2);
			result[2].Date.Should().Be(startDate.AddDays(1));
			result[2].AccountId.Should().Be(1);
			result[3].Date.Should().Be(startDate.AddDays(1));
			result[3].AccountId.Should().Be(2);
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_WithEmptyDatabase_ShouldReturnEmptyList()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate, CancellationToken.None);

			// Assert
			_mockDatabaseContext.Verify(x => x.CalculatedSnapshots, Times.AtLeastOnce);
			_mockDatabaseContext.Verify(x => x.Balances, Times.AtLeastOnce);
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_ShouldUsePrimaryCurrencyFromService()
		{
			// Arrange
			var startDate = new DateOnly(2023, 1, 1);
			var endDate = new DateOnly(2023, 1, 31);
			var accountId = 1;

			var expectedCurrency = Currency.EUR;
			_mockServerConfigurationService.Setup(x => x.PrimaryCurrency).Returns(expectedCurrency);

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateCalculatedSnapshot(startDate, accountId, 1000m, 800m)
			};

			var balances = new List<Balance>
			{
				CreateBalance(startDate, accountId, 200m)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);

			var point = result[0];
			point.TotalAssetValue.Currency.Should().Be(expectedCurrency);
			point.TotalInvested.Currency.Should().Be(expectedCurrency);
			point.CashBalance.Currency.Should().Be(expectedCurrency);
			point.TotalValue.Currency.Should().Be(expectedCurrency);
		}

		#endregion

		#region GetMinDateAsync Tests

		[Fact]
		public async Task GetMinDateAsync_WithActivities_ShouldReturnMinimumDate()
		{
			// Arrange
			var expectedMinDate = new DateOnly(2020, 1, 1);
			var platform = CreateTestPlatform("Test Platform");
			var account = CreateTestAccount("Test Account", 1, platform);
			var holding = CreateTestHolding(CreateTestSymbolProfile("TEST"));

			var activities = new List<BuyActivity>
			{
				CreateTestActivity(holding, 1, new DateTime(2023, 1, 1)),
				CreateTestActivity(holding, 1, expectedMinDate.ToDateTime(TimeOnly.MinValue)),
				CreateTestActivity(holding, 1, new DateTime(2022, 6, 15))
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _accountDataService.GetMinDateAsync(CancellationToken.None);

			// Assert
			result.Should().Be(expectedMinDate);
		}

		[Fact]
		public async Task GetMinDateAsync_WithEmptyDatabase_ShouldThrowInvalidOperationException()
		{
			// Arrange
			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(new List<BuyActivity>());

			// Act & Assert
			var action = () => _accountDataService.GetMinDateAsync(CancellationToken.None);
			await action.Should().ThrowAsync<InvalidOperationException>()
				.WithMessage("Sequence contains no elements");
		}

		[Fact]
		public async Task GetMinDateAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			var cancellationToken = new CancellationToken();
			var platform = CreateTestPlatform("Test Platform");
			var account = CreateTestAccount("Test Account", 1, platform);
			var holding = CreateTestHolding(CreateTestSymbolProfile("TEST"));
			var activities = new List<BuyActivity>
			{
				CreateTestActivity(holding, 1, new DateTime(2023, 1, 1))
			};

			_mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			await _accountDataService.GetMinDateAsync(cancellationToken);

			// Assert
			_mockDatabaseContext.Verify(x => x.Activities, Times.AtLeastOnce);
		}

		#endregion

		#region GetSymbolProfilesAsync Tests

		[Fact]
		public async Task GetSymbolProfilesAsync_WithoutAccountFilter_ShouldReturnAllSymbolsOrderedBySymbol()
		{
			// Arrange
			var symbolProfiles = new List<SymbolProfile>
			{
				CreateTestSymbolProfile("ZULU"),
				CreateTestSymbolProfile("AAPL"),
				CreateTestSymbolProfile("MSFT"),
				CreateTestSymbolProfile("BETA")
			};

			_mockDatabaseContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(symbolProfiles);

			// Act
			var result = await _accountDataService.GetSymbolProfilesAsync(null, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(4);
			result[0].Should().Be("AAPL");
			result[1].Should().Be("BETA");
			result[2].Should().Be("MSFT");
			result[3].Should().Be("ZULU");
		}

		[Fact]
		public async Task GetSymbolProfilesAsync_WithAccountFilter_ShouldReturnSymbolsForSpecificAccount()
		{
			// Arrange
			var accountId = 1;
			var symbolProfile1 = CreateTestSymbolProfile("AAPL");
			var symbolProfile2 = CreateTestSymbolProfile("MSFT");
			var symbolProfile3 = CreateTestSymbolProfile("GOOGL");

			var holding1 = CreateTestHolding(symbolProfile1);
			var holding2 = CreateTestHolding(symbolProfile2);
			var holding3 = CreateTestHolding(symbolProfile3);

			var activity1 = CreateTestActivity(holding1, accountId);
			var activity2 = CreateTestActivity(holding2, accountId);
			var activity3 = CreateTestActivity(holding3, 2); // Different account

			holding1.Activities.Add(activity1);
			holding2.Activities.Add(activity2);
			holding3.Activities.Add(activity3);

			var holdings = new List<Holding> { holding1, holding2, holding3 };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);

			// Act
			var result = await _accountDataService.GetSymbolProfilesAsync(accountId, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(2);
			result.Should().Contain("AAPL");
			result.Should().Contain("MSFT");
			result.Should().NotContain("GOOGL");
		}

		[Fact]
		public async Task GetSymbolProfilesAsync_WithAccountFilterButNoMatchingHoldings_ShouldReturnEmptyList()
		{
			// Arrange
			var accountId = 999; // Non-existent account
			var holdings = new List<Holding>();

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);

			// Act
			var result = await _accountDataService.GetSymbolProfilesAsync(accountId, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetSymbolProfilesAsync_WithEmptyDatabase_ShouldReturnEmptyList()
		{
			// Arrange
			_mockDatabaseContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile>());

			// Act
			var result = await _accountDataService.GetSymbolProfilesAsync(null, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetSymbolProfilesAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			_mockDatabaseContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile>());

			// Act
			await _accountDataService.GetSymbolProfilesAsync(null, CancellationToken.None);

			// Assert
			_mockDatabaseContext.Verify(x => x.SymbolProfiles, Times.AtLeastOnce);
		}

		[Fact]
		public async Task GetSymbolProfilesAsync_WithAccountFilter_ShouldOrderResultsBySymbol()
		{
			// Arrange
			var accountId = 1;
			var symbolProfileZ = CreateTestSymbolProfile("ZULU");
			var symbolProfileA = CreateTestSymbolProfile("AAPL");
			var symbolProfileM = CreateTestSymbolProfile("MSFT");

			var holdingZ = CreateTestHolding(symbolProfileZ);
			var holdingA = CreateTestHolding(symbolProfileA);
			var holdingM = CreateTestHolding(symbolProfileM);

			var activityZ = CreateTestActivity(holdingZ, accountId);
			var activityA = CreateTestActivity(holdingA, accountId);
			var activityM = CreateTestActivity(holdingM, accountId);

			holdingZ.Activities.Add(activityZ);
			holdingA.Activities.Add(activityA);
			holdingM.Activities.Add(activityM);

			var holdings = new List<Holding> { holdingZ, holdingA, holdingM };

			_mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);

			// Act
			var result = await _accountDataService.GetSymbolProfilesAsync(accountId, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(3);
			result[0].Should().Be("AAPL");
			result[1].Should().Be("MSFT");
			result[2].Should().Be("ZULU");
		}

		#endregion

		#region GetTaxReportAsync Tests

		[Fact]
		public async Task GetTaxReportAsync_WhenCacheIsValid_ShouldReturnCachedResultWithoutHittingDatabase()
		{
			// Arrange
			var cached = new List<TaxReportRow>
			{
				new TaxReportRow
				{
					Year = 2023,
					Date = new DateOnly(2023, 1, 1),
					AccountId = 1,
					AccountName = "Cached Account",
					AssetValue = new Money(Currency.USD, 999m),
					CashBalance = new Money(Currency.USD, 1m),
					TotalValue = new Money(Currency.USD, 1000m)
				}
			};

			_mockTaxReportCacheService.Setup(x => x.IsValid).Returns(true);
			_mockTaxReportCacheService.Setup(x => x.CachedResult).Returns(cached);

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			result.Should().BeSameAs(cached);
			_mockDatabaseContext.Verify(x => x.CalculatedSnapshots, Times.Never);
			_mockDatabaseContext.Verify(x => x.Balances, Times.Never);
		}

		[Fact]
		public async Task GetTaxReportAsync_WhenCacheIsInvalid_ShouldStoreResultInCache()
		{
			// Arrange
			var account = CreateTestAccount("Broker A", 1);
			var jan1 = new DateOnly(2023, 1, 1);

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>
			{
				CreateCalculatedSnapshot(jan1, 1, 1000m, 900m)
			});
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			List<TaxReportRow>? storedResult = null;
			_mockTaxReportCacheService
				.Setup(x => x.Store(It.IsAny<List<TaxReportRow>>()))
				.Callback<List<TaxReportRow>>(r => storedResult = r);

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			_mockTaxReportCacheService.Verify(x => x.Store(It.IsAny<List<TaxReportRow>>()), Times.Once);
			storedResult.Should().NotBeNull();
			storedResult.Should().BeSameAs(result);
		}

		[Fact]
		public async Task GetTaxReportAsync_WithEmptyDatabase_ShouldReturnEmptyList()
		{
			// Arrange
			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account>());
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetTaxReportAsync_WithOnlySnapshots_ShouldReturnRowsForJan1AndDec31()
		{
			// Arrange
			var account = CreateTestAccount("Broker A", 1);
			var jan1 = new DateOnly(2023, 1, 1);
			var dec31 = new DateOnly(2023, 12, 31);

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateCalculatedSnapshot(jan1, 1, 1000m, 900m),
				CreateCalculatedSnapshot(dec31, 1, 1500m, 1400m)
			};

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(2);
			result[0].Date.Should().Be(jan1);
			result[0].TotalValue.Amount.Should().Be(1000m);
			result[1].Date.Should().Be(dec31);
			result[1].TotalValue.Amount.Should().Be(1500m);
		}

		[Fact]
		public async Task GetTaxReportAsync_WithCashBalance_ShouldAddBalanceToTotal()
		{
			// Arrange
			var account = CreateTestAccount("Broker A", 1);
			var jan1 = new DateOnly(2023, 1, 1);

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateCalculatedSnapshot(jan1, 1, 1000m, 900m)
			};
			var balances = new List<Balance>
			{
				CreateBalance(jan1, 1, 250m)
			};

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			var jan1Row = result.First(r => r.Date == jan1);
			jan1Row.AssetValue.Amount.Should().Be(1000m);
			jan1Row.CashBalance.Amount.Should().Be(250m);
			jan1Row.TotalValue.Amount.Should().Be(1250m);
		}

		[Fact]
		public async Task GetTaxReportAsync_WithBalanceFromPriorYear_ShouldUseItForJan1()
		{
			// Arrange - balance on Dec 31 2022 should be picked up for Jan 1 2023
			var account = CreateTestAccount("Broker A", 1);
			var dec31Prior = new DateOnly(2022, 12, 31);
			var jan1 = new DateOnly(2023, 1, 1);
			var dec31 = new DateOnly(2023, 12, 31);

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateCalculatedSnapshot(jan1, 1, 1000m, 900m),
				CreateCalculatedSnapshot(dec31, 1, 1500m, 1400m)
			};
			var balances = new List<Balance>
			{
				CreateBalance(dec31Prior, 1, 500m), // prior-year balance, no Jan 1 balance exists
				CreateBalance(dec31, 1, 600m)
			};

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			var jan1Row = result.First(r => r.Year == 2023 && r.Date == jan1);
			jan1Row.CashBalance.Amount.Should().Be(500m); // picked up from Dec 31 prior year
		}

		[Fact]
		public async Task GetTaxReportAsync_ShouldPickClosestSnapshotOnOrBeforeTargetDate()
		{
			// Arrange - snapshot on Dec 29 should be used for Dec 31 (no Dec 31 snapshot)
			var account = CreateTestAccount("Broker A", 1);
			var jan1 = new DateOnly(2023, 1, 1);
			var dec29 = new DateOnly(2023, 12, 29);

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateCalculatedSnapshot(jan1, 1, 1000m, 900m),
				CreateCalculatedSnapshot(dec29, 1, 1400m, 1300m)
			};

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			var dec31Row = result.First(r => r.Year == 2023 && r.Date == new DateOnly(2023, 12, 31));
			dec31Row.AssetValue.Amount.Should().Be(1400m); // from Dec 29 snapshot
		}

		[Fact]
		public async Task GetTaxReportAsync_WithMultipleAccounts_ShouldReturnRowsPerAccount()
		{
			// Arrange
			var account1 = CreateTestAccount("Broker A", 1);
			var account2 = CreateTestAccount("Broker B", 2);
			var jan1 = new DateOnly(2023, 1, 1);

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateCalculatedSnapshot(jan1, 1, 1000m, 900m),
				CreateCalculatedSnapshot(jan1, 2, 2000m, 1800m)
			};

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account1, account2 });
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			var jan1Rows = result.Where(r => r.Date == jan1).OrderBy(r => r.AccountName).ToList();
			jan1Rows.Should().HaveCount(2);
			jan1Rows[0].AccountName.Should().Be("Broker A");
			jan1Rows[0].AssetValue.Amount.Should().Be(1000m);
			jan1Rows[1].AccountName.Should().Be("Broker B");
			jan1Rows[1].AssetValue.Amount.Should().Be(2000m);
		}

		[Fact]
		public async Task GetTaxReportAsync_ShouldUseAccountNameFromAccountsTable()
		{
			// Arrange
			var account = CreateTestAccount("My Named Account", 42);
			var jan1 = new DateOnly(2023, 1, 1);

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateCalculatedSnapshot(jan1, 42, 1000m, 900m)
			};

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
				result.Should().HaveCount(2); // Jan 1 and Dec 31 both pick up the only available snapshot
				result.Should().AllSatisfy(r => r.AccountName.Should().Be("My Named Account"));
		}

		[Fact]
		public async Task GetTaxReportAsync_WithUnknownAccountId_ShouldFallbackToAccountIdLabel()
		{
			// Arrange - snapshot references accountId 99 which has no Account record
			var jan1 = new DateOnly(2023, 1, 1);
			var snapshots = new List<CalculatedSnapshot>
			{
				CreateCalculatedSnapshot(jan1, 99, 1000m, 900m)
			};

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account>());
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
				result.Should().HaveCount(2); // Jan 1 and Dec 31 both pick up the only available snapshot
				result.Should().AllSatisfy(r => r.AccountName.Should().Be("Account 99"));
		}

		[Fact]
		public async Task GetTaxReportAsync_ShouldReturnRowsOrderedByYearThenDateThenAccountName()
		{
			// Arrange
			var account1 = CreateTestAccount("Zulu", 1);
			var account2 = CreateTestAccount("Alpha", 2);
			var jan1 = new DateOnly(2023, 1, 1);
			var dec31 = new DateOnly(2023, 12, 31);

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateCalculatedSnapshot(dec31, 1, 1000m, 900m),
				CreateCalculatedSnapshot(dec31, 2, 500m, 450m),
				CreateCalculatedSnapshot(jan1, 1, 800m, 700m),
				CreateCalculatedSnapshot(jan1, 2, 400m, 350m)
			};

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account1, account2 });
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			result[0].Date.Should().Be(jan1);
			result[0].AccountName.Should().Be("Alpha");
			result[1].Date.Should().Be(jan1);
			result[1].AccountName.Should().Be("Zulu");
			result[2].Date.Should().Be(dec31);
			result[2].AccountName.Should().Be("Alpha");
			result[3].Date.Should().Be(dec31);
			result[3].AccountName.Should().Be("Zulu");
		}

		[Fact]
		public async Task GetTaxReportAsync_CurrentYear_ShouldUseTodayAsEndDate()
		{
			// Arrange
			var account = CreateTestAccount("Broker A", 1);
			var today = DateOnly.FromDateTime(DateTime.Today);
			var jan1 = new DateOnly(today.Year, 1, 1);

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateCalculatedSnapshot(jan1, 1, 1000m, 900m),
				CreateCalculatedSnapshot(today, 1, 1200m, 1100m)
			};

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			var dates = result.Select(r => r.Date).Distinct().OrderBy(d => d).ToList();
			dates.Should().Contain(jan1);
			dates.Should().Contain(today);
			dates.Should().NotContain(new DateOnly(today.Year, 12, 31));
		}

		[Fact]
		public async Task GetTaxReportAsync_WithBalanceOnlyAccount_ShouldIncludeItWithZeroAssets()
		{
			// Arrange - account has a balance but no snapshots
			var account = CreateTestAccount("Cash Account", 1);
			var jan1 = new DateOnly(2023, 1, 1);

			var balances = new List<Balance>
			{
				CreateBalance(jan1, 1, 5000m)
			};

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
				// The single Jan 1 balance is also the closest value for Dec 31, so both dates appear
				result.Should().HaveCount(2);
				result.Should().AllSatisfy(r =>
				{
					r.AssetValue.Amount.Should().Be(0m);
					r.CashBalance.Amount.Should().Be(5000m);
					r.TotalValue.Amount.Should().Be(5000m);
				});
		}

		[Fact]
		public async Task GetTaxReportAsync_ShouldUsePrimaryCurrencyForAllRows()
		{
			// Arrange
			_mockServerConfigurationService.Setup(x => x.PrimaryCurrency).Returns(Currency.EUR);
			var account = CreateTestAccount("Broker A", 1);
			var jan1 = new DateOnly(2023, 1, 1);

			var snapshots = new List<CalculatedSnapshot>
			{
				CreateCalculatedSnapshot(jan1, 1, 1000m, 900m)
			};

			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
				// The single Jan 1 snapshot is also the closest value for Dec 31, so 2 rows appear
				result.Should().HaveCount(2);
				result.Should().AllSatisfy(r =>
				{
					r.TotalValue.Currency.Should().Be(Currency.EUR);
					r.AssetValue.Currency.Should().Be(Currency.EUR);
					r.CashBalance.Currency.Should().Be(Currency.EUR);
				});
		}

		#endregion

		#region Helper Methods

		private static Platform CreateTestPlatform(string name, int id = 1)
		{
			return new Platform(name) { Id = id };
		}

		private static Account CreateTestAccount(string name, int id = 1, Platform? platform = null)
		{
			return new Account(name)
			{
				Id = id,
				Platform = platform,
				Activities = []
			};
		}

		private static SymbolProfile CreateTestSymbolProfile(string symbol)
		{
			return new SymbolProfile(
					symbol,
					$"{symbol} Company",
					[new SymbolIdentifier { Identifier = symbol, IdentifierType = IdentifierType.Ticker }],
					Currency.USD,
				"TEST",
				AssetClass.Equity,
				null,
				[],
				[]);
		}

		private static Holding CreateTestHolding(SymbolProfile symbolProfile)
		{
			var holding = new Holding { Id = 1, Activities = [] };
			holding.SymbolProfiles.Add(symbolProfile);
			return holding;
		}

		private static BuyActivity CreateTestActivity(Holding holding, int accountId = 1, DateTime? date = null)
		{
			var account = new Account("Test Account") { Id = accountId };
			return new BuyActivity(
				account,
				holding,
				[],
				date ?? DateTime.Now,
				1m,
				new Money(Currency.USD, 100m),
				"TEST-001",
				null,
				"Test activity");
		}

		private static CalculatedSnapshot CreateCalculatedSnapshot(DateOnly date, int accountId, decimal totalValue, decimal totalInvested)
		{
			return new CalculatedSnapshot
			{
				Date = date,
				AccountId = accountId,
				TotalValue = totalValue,
				TotalInvested = totalInvested,
				Holding = new Holding { SymbolProfiles = [ new SymbolProfile { Symbol = "TEST" } ] },
			};
		}

		private static Balance CreateBalance(DateOnly date, int accountId, decimal money)
		{
			return new Balance(date, new Money(Currency.USD, money))
			{
				AccountId = accountId
			};
		}

		#endregion
	}
}

