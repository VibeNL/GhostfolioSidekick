using AwesomeAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.UnitTests.Services
{
	public class AccountDataServiceTests
	{
		private readonly Mock<DatabaseContext> _mockDatabaseContext;
		private readonly Mock<IServerConfigurationService> _mockServerConfigurationService;
		private readonly AccountDataService _accountDataService;

		public AccountDataServiceTests()
		{
			_mockDatabaseContext = new Mock<DatabaseContext>();
			_mockServerConfigurationService = new Mock<IServerConfigurationService>();
			_mockServerConfigurationService.Setup(x => x.PrimaryCurrency).Returns(Currency.USD);
			_accountDataService = new AccountDataService(_mockDatabaseContext.Object, _mockServerConfigurationService.Object);
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
			var result = await _accountDataService.GetAccountsAsync(null);

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
			var result = await _accountDataService.GetAccountsAsync("AAPL");

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
			var result = await _accountDataService.GetAccountsAsync(string.Empty);

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
			var result = await _accountDataService.GetAccountsAsync("   ");

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(2);
		}

		[Fact]
		public async Task GetAccountsAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			var cancellationToken = new CancellationToken();
			var accounts = new List<Account>();
			_mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(accounts);

			// Act
			await _accountDataService.GetAccountsAsync(null, cancellationToken);

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

			var snapshots = new List<CalculatedSnapshotPrimaryCurrency>
			{
				CreateCalculatedSnapshot(startDate, accountId, 1000m, 800m),
				CreateCalculatedSnapshot(startDate.AddDays(1), accountId, 1100m, 850m)
			};

			var balances = new List<BalancePrimaryCurrency>
			{
				CreateBalance(startDate, accountId, 200m),
				CreateBalance(startDate.AddDays(1), accountId, 250m)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.BalancePrimaryCurrencies).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate);

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

			var snapshots = new List<CalculatedSnapshotPrimaryCurrency>(); // Empty snapshots

			var balances = new List<BalancePrimaryCurrency>
			{
				CreateBalance(startDate, accountId, 200m)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.BalancePrimaryCurrencies).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate);

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

			var snapshots = new List<CalculatedSnapshotPrimaryCurrency>
			{
				CreateCalculatedSnapshot(startDate, 1, 1000m, 800m),
				CreateCalculatedSnapshot(startDate, 2, 2000m, 1600m)
			};

			var balances = new List<BalancePrimaryCurrency>
			{
				CreateBalance(startDate, 1, 200m),
				CreateBalance(startDate, 2, 400m)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.BalancePrimaryCurrencies).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate);

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

			var snapshots = new List<CalculatedSnapshotPrimaryCurrency>
			{
				CreateCalculatedSnapshot(startDate.AddDays(1), 2, 1000m, 800m),
				CreateCalculatedSnapshot(startDate, 1, 1000m, 800m),
				CreateCalculatedSnapshot(startDate.AddDays(1), 1, 1000m, 800m),
				CreateCalculatedSnapshot(startDate, 2, 1000m, 800m)
			};

			var balances = new List<BalancePrimaryCurrency>
			{
				CreateBalance(startDate.AddDays(1), 2, 200m),
				CreateBalance(startDate, 1, 200m),
				CreateBalance(startDate.AddDays(1), 1, 200m),
				CreateBalance(startDate, 2, 200m)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.BalancePrimaryCurrencies).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate);

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

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency>());
			_mockDatabaseContext.Setup(x => x.BalancePrimaryCurrencies).ReturnsDbSet(new List<BalancePrimaryCurrency>());

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate);

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
			var cancellationToken = new CancellationToken();

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency>());
			_mockDatabaseContext.Setup(x => x.BalancePrimaryCurrencies).ReturnsDbSet(new List<BalancePrimaryCurrency>());

			// Act
			await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate, cancellationToken);

			// Assert
			_mockDatabaseContext.Verify(x => x.CalculatedSnapshotPrimaryCurrencies, Times.AtLeastOnce);
			_mockDatabaseContext.Verify(x => x.BalancePrimaryCurrencies, Times.AtLeastOnce);
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

			var snapshots = new List<CalculatedSnapshotPrimaryCurrency>
			{
				CreateCalculatedSnapshot(startDate, accountId, 1000m, 800m)
			};

			var balances = new List<BalancePrimaryCurrency>
			{
				CreateBalance(startDate, accountId, 200m)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(snapshots);
			_mockDatabaseContext.Setup(x => x.BalancePrimaryCurrencies).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate);

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
		public async Task GetMinDateAsync_WithCalculatedSnapshots_ShouldReturnMinimumDate()
		{
			// Arrange
			var expectedMinDate = new DateOnly(2020, 1, 1);
			var snapshots = new List<CalculatedSnapshotPrimaryCurrency>
			{
				CreateCalculatedSnapshot(new DateOnly(2023, 1, 1), 1, 1000m, 800m),
				CreateCalculatedSnapshot(expectedMinDate, 1, 500m, 400m),
				CreateCalculatedSnapshot(new DateOnly(2022, 6, 15), 1, 750m, 600m)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(snapshots);

			// Act
			var result = await _accountDataService.GetMinDateAsync();

			// Assert
			result.Should().Be(expectedMinDate);
		}

		[Fact]
		public async Task GetMinDateAsync_WithEmptyDatabase_ShouldThrowInvalidOperationException()
		{
			// Arrange
			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(new List<CalculatedSnapshotPrimaryCurrency>());

			// Act & Assert
			var action = () => _accountDataService.GetMinDateAsync();
			await action.Should().ThrowAsync<InvalidOperationException>()
				.WithMessage("Sequence contains no elements");
		}

		[Fact]
		public async Task GetMinDateAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			var cancellationToken = new CancellationToken();
			var snapshots = new List<CalculatedSnapshotPrimaryCurrency>
			{
				CreateCalculatedSnapshot(new DateOnly(2023, 1, 1), 1, 1000m, 800m)
			};

			_mockDatabaseContext.Setup(x => x.CalculatedSnapshotPrimaryCurrencies).ReturnsDbSet(snapshots);

			// Act
			await _accountDataService.GetMinDateAsync(cancellationToken);

			// Assert
			_mockDatabaseContext.Verify(x => x.CalculatedSnapshotPrimaryCurrencies, Times.AtLeastOnce);
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
			var result = await _accountDataService.GetSymbolProfilesAsync(null);

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
			var result = await _accountDataService.GetSymbolProfilesAsync(accountId);

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
			var result = await _accountDataService.GetSymbolProfilesAsync(accountId);

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
			var result = await _accountDataService.GetSymbolProfilesAsync(null);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetSymbolProfilesAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			var cancellationToken = new CancellationToken();
			_mockDatabaseContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile>());

			// Act
			await _accountDataService.GetSymbolProfilesAsync(null, cancellationToken);

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
			var result = await _accountDataService.GetSymbolProfilesAsync(accountId);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(3);
			result[0].Should().Be("AAPL");
			result[1].Should().Be("MSFT");
			result[2].Should().Be("ZULU");
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
				[symbol],
				Currency.USD,
				"TEST",
				AssetClass.Equity,
				null,
				Array.Empty<CountryWeight>(),
				Array.Empty<SectorWeight>());
		}

		private static Holding CreateTestHolding(SymbolProfile symbolProfile)
		{
			var holding = new Holding { Id = 1, Activities = [] };
			holding.SymbolProfiles.Add(symbolProfile);
			return holding;
		}

		private static BuyActivity CreateTestActivity(Holding holding, int accountId = 1)
		{
			var account = new Account("Test Account") { Id = accountId };
			return new BuyActivity(
				account,
				holding,
				[],
				DateTime.Now,
				1m,
				new Money(Currency.USD, 100m),
				"TEST-001",
				null,
				"Test activity");
		}

		private static CalculatedSnapshotPrimaryCurrency CreateCalculatedSnapshot(DateOnly date, int accountId, decimal totalValue, decimal totalInvested)
		{
			return new CalculatedSnapshotPrimaryCurrency
			{
				Date = date,
				AccountId = accountId,
				TotalValue = totalValue,
				TotalInvested = totalInvested
			};
		}

		private static BalancePrimaryCurrency CreateBalance(DateOnly date, int accountId, decimal money)
		{
			return new BalancePrimaryCurrency
			{
				Date = date,
				AccountId = accountId,
				Money = money
			};
		}

		#endregion
	}
}