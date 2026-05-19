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
			_ = _mockServerConfigurationService.Setup(x => x.PrimaryCurrency).Returns(Currency.USD);

			_mockTaxReportCacheService = new Mock<ITaxReportCacheService>();
			_ = _mockTaxReportCacheService.Setup(x => x.IsValid).Returns(false);

			Mock<IDbContextFactory<DatabaseContext>> dbContextFactoryMock = new();
			_ = dbContextFactoryMock.Setup(x => x.CreateDbContextAsync()).ReturnsAsync(_mockDatabaseContext.Object);
			_accountDataService = new AccountDataService(dbContextFactoryMock.Object, _mockServerConfigurationService.Object, _mockTaxReportCacheService.Object);
		}

		#region GetAccountInfo Tests

		[Fact]
		public async Task GetAccountInfo_WithAccounts_ShouldReturnAccountsOrderedByName()
		{
			// Arrange
			var platform = CreateTestPlatform("Test Platform");
			List<Account> accounts = new()
			{
				CreateTestAccount("Charlie Account", 3, platform),
				CreateTestAccount("Alpha Account", 1, platform),
				CreateTestAccount("Beta Account", 2, platform)
			};

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(accounts);

			// Act
			var result = await _accountDataService.GetAccountInfo();

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(3);
			_ = result[0].Name.Should().Be("Alpha Account");
			_ = result[1].Name.Should().Be("Beta Account");
			_ = result[2].Name.Should().Be("Charlie Account");
			_ = result.Should().AllSatisfy(account => account.Platform.Should().NotBeNull());
		}

		[Fact]
		public async Task GetAccountInfo_WithEmptyDatabase_ShouldReturnEmptyList()
		{
			// Arrange
			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account>());

			// Act
			var result = await _accountDataService.GetAccountInfo();

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetAccountInfo_ShouldIncludePlatformAndUseAsNoTracking()
		{
			// Arrange
			var platform = CreateTestPlatform("Investment Platform");
			List<Account> accounts = new()
			{
				CreateTestAccount("Test Account", 1, platform)
			};

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(accounts);

			// Act
			var result = await _accountDataService.GetAccountInfo();

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(1);
			_ = result[0].Platform.Should().NotBeNull();
			_ = result[0].Platform!.Name.Should().Be("Investment Platform");
		}

		#endregion

		#region GetAccountByIdAsync Tests

		[Fact]
		public async Task GetAccountByIdAsync_WithExistingAccount_ShouldReturnAccount()
		{
			// Arrange
			var platform = CreateTestPlatform("Test Platform");
			var account = CreateTestAccount("Test Account", 42, platform);
			List<Account> accounts = new()
			{ account };
			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(accounts);

			// Act
			var result = await _accountDataService.GetAccountByIdAsync(42);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result!.Id.Should().Be(42);
			_ = result.Name.Should().Be("Test Account");
			_ = result.Platform.Should().NotBeNull();
			_ = result.Platform!.Name.Should().Be("Test Platform");
		}

		[Fact]
		public async Task GetAccountByIdAsync_WithNonExistingAccount_ShouldReturnNull()
		{
			// Arrange
			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account>());

			// Act
			var result = await _accountDataService.GetAccountByIdAsync(999);

			// Assert
			_ = result.Should().BeNull();
		}

		#endregion

		#region GetAccountsAsync Tests

		[Fact]
		public async Task GetAccountsAsync_WithoutSymbolFilter_ShouldReturnAllAccountsOrderedByName()
		{
			// Arrange
			var platform = CreateTestPlatform("Test Platform");
			List<Account> accounts = new()
			{
				CreateTestAccount("Zulu Account", 3, platform),
				CreateTestAccount("Alpha Account", 1, platform),
				CreateTestAccount("Beta Account", 2, platform)
			};

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(accounts);

			// Act
			var result = await _accountDataService.GetAccountsAsync(null, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(3);
			_ = result[0].Name.Should().Be("Alpha Account");
			_ = result[1].Name.Should().Be("Beta Account");
			_ = result[2].Name.Should().Be("Zulu Account");
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

			List<Account> accounts = new()
			{ accountWithSymbol, accountWithoutSymbol };

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(accounts);

			// Act
			var result = await _accountDataService.GetAccountsAsync("AAPL", CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(1);
			_ = result[0].Name.Should().Be("Account with AAPL");
		}

		[Fact]
		public async Task GetAccountsAsync_WithEmptySymbolFilter_ShouldReturnAllAccounts()
		{
			// Arrange
			var platform = CreateTestPlatform("Test Platform");
			List<Account> accounts = new()
			{
				CreateTestAccount("Account 1", 1, platform),
				CreateTestAccount("Account 2", 2, platform)
			};

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(accounts);

			// Act
			var result = await _accountDataService.GetAccountsAsync(string.Empty, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(2);
		}

		[Fact]
		public async Task GetAccountsAsync_WithWhitespaceSymbolFilter_ShouldReturnAllAccounts()
		{
			// Arrange
			var platform = CreateTestPlatform("Test Platform");
			List<Account> accounts = new()
			{
				CreateTestAccount("Account 1", 1, platform),
				CreateTestAccount("Account 2", 2, platform)
			};

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(accounts);

			// Act
			var result = await _accountDataService.GetAccountsAsync("   ", CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(2);
		}

		[Fact]
		public async Task GetAccountsAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			List<Account> accounts = new();
			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(accounts);

			// Act
			_ = await _accountDataService.GetAccountsAsync(null, CancellationToken.None);

			// Assert
			_mockDatabaseContext.Verify(x => x.Accounts, Times.AtLeastOnce);
		}

		#endregion

		#region GetAccountValueHistoryAsync Tests

		[Fact]
		public async Task GetAccountValueHistoryAsync_WithValidDateRange_ShouldReturnAccountValueHistory()
		{
			// Arrange
			DateOnly startDate = new(2023, 1, 1);
			DateOnly endDate = new(2023, 1, 31);
			var accountId = 1;

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(startDate, accountId, 1000m, 800m),
				CreateCalculatedSnapshot(startDate.AddDays(1), accountId, 1100m, 850m)
			};

			List<Balance> balances = new()
			{
				CreateBalance(startDate, accountId, 200m),
				CreateBalance(startDate.AddDays(1), accountId, 250m)
			};

			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate, CancellationToken.None);

			// Assert – forward-fill produces one point per day for the whole range (31 days)
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(31);

			var jan1 = result.Single(p => p.Date == startDate);
			_ = jan1.AccountId.Should().Be(accountId);
			_ = jan1.TotalAssetValue.Amount.Should().Be(1000m);
			_ = jan1.TotalInvested.Amount.Should().Be(800m);
			_ = jan1.CashBalance.Amount.Should().Be(200m);
			_ = jan1.TotalValue.Amount.Should().Be(1200m); // 1000 + 200

			var jan2 = result.Single(p => p.Date == startDate.AddDays(1));
			_ = jan2.TotalAssetValue.Amount.Should().Be(1100m);
			_ = jan2.TotalInvested.Amount.Should().Be(850m);
			_ = jan2.CashBalance.Amount.Should().Be(250m);
			_ = jan2.TotalValue.Amount.Should().Be(1350m); // 1100 + 250

			// Remaining days forward-fill Jan 2 values
			var jan31 = result.Single(p => p.Date == endDate);
			_ = jan31.TotalAssetValue.Amount.Should().Be(1100m);
			_ = jan31.CashBalance.Amount.Should().Be(250m);
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_WithMissingSnapshots_ShouldForwardFillBalanceAcrossRange()
		{
			// Arrange
			DateOnly startDate = new(2023, 1, 1);
			DateOnly endDate = new(2023, 1, 31);
			var accountId = 1;

			List<CalculatedSnapshot> snapshots = new(); // No snapshot data

			List<Balance> balances = new()
			{
				CreateBalance(startDate, accountId, 200m)
			};

			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate, CancellationToken.None);

			// Assert – the single balance on Jan 1 is forward-filled for every day through Jan 31
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(31);
			_ = result.Should().AllSatisfy(p =>
			{
				_ = p.AccountId.Should().Be(accountId);
				_ = p.TotalAssetValue.Amount.Should().Be(0m);
				_ = p.TotalInvested.Amount.Should().Be(0m);
				_ = p.CashBalance.Amount.Should().Be(200m);
				_ = p.TotalValue.Amount.Should().Be(200m);
			});
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_WithMultipleAccounts_ShouldReturnDataForAllAccounts()
		{
			// Arrange
			DateOnly startDate = new(2023, 1, 1);
			DateOnly endDate = new(2023, 1, 31);

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(startDate, 1, 1000m, 800m),
				CreateCalculatedSnapshot(startDate, 2, 2000m, 1600m)
			};

			List<Balance> balances = new()
			{
				CreateBalance(startDate, 1, 200m),
				CreateBalance(startDate, 2, 400m)
			};

			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate, CancellationToken.None);

			// Assert – 31 days × 2 accounts = 62 points
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(62);
			_ = result.Should().Contain(p => p.AccountId == 1);
			_ = result.Should().Contain(p => p.AccountId == 2);
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_ShouldOrderByDateThenAccountId()
		{
			// Arrange
			DateOnly startDate = new(2023, 1, 1);
			DateOnly endDate = new(2023, 1, 3);

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(startDate.AddDays(1), 2, 1000m, 800m),
				CreateCalculatedSnapshot(startDate, 1, 1000m, 800m),
				CreateCalculatedSnapshot(startDate.AddDays(1), 1, 1000m, 800m),
				CreateCalculatedSnapshot(startDate, 2, 1000m, 800m)
			};

			List<Balance> balances = new()
			{
				CreateBalance(startDate.AddDays(1), 2, 200m),
				CreateBalance(startDate, 1, 200m),
				CreateBalance(startDate.AddDays(1), 1, 200m),
				CreateBalance(startDate, 2, 200m)
			};

			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate, CancellationToken.None);

			// Assert – 3 days × 2 accounts = 6 points, ordered by date then account ID
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(6);

			_ = result[0].Date.Should().Be(startDate);
			_ = result[0].AccountId.Should().Be(1);
			_ = result[1].Date.Should().Be(startDate);
			_ = result[1].AccountId.Should().Be(2);
			_ = result[2].Date.Should().Be(startDate.AddDays(1));
			_ = result[2].AccountId.Should().Be(1);
			_ = result[3].Date.Should().Be(startDate.AddDays(1));
			_ = result[3].AccountId.Should().Be(2);
			// Jan 3 is forward-filled from Jan 2 for both accounts
			_ = result[4].Date.Should().Be(startDate.AddDays(2));
			_ = result[4].AccountId.Should().Be(1);
			_ = result[5].Date.Should().Be(startDate.AddDays(2));
			_ = result[5].AccountId.Should().Be(2);
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_WithEmptyDatabase_ShouldReturnEmptyList()
		{
			// Arrange
			DateOnly startDate = new(2023, 1, 1);
			DateOnly endDate = new(2023, 1, 31);

			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			DateOnly startDate = new(2023, 1, 1);
			DateOnly endDate = new(2023, 1, 31);

			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			_ = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate, CancellationToken.None);

			// Assert
			_mockDatabaseContext.Verify(x => x.CalculatedSnapshots, Times.AtLeastOnce);
			_mockDatabaseContext.Verify(x => x.Balances, Times.AtLeastOnce);
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_ShouldUsePrimaryCurrencyFromService()
		{
			// Arrange
			DateOnly startDate = new(2023, 1, 1);
			DateOnly endDate = new(2023, 1, 31);
			var accountId = 1;

			var expectedCurrency = Currency.EUR;
			_ = _mockServerConfigurationService.Setup(x => x.PrimaryCurrency).Returns(expectedCurrency);

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(startDate, accountId, 1000m, 800m)
			};

			List<Balance> balances = new()
			{
				CreateBalance(startDate, accountId, 200m)
			};

			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate, CancellationToken.None);

			// Assert – 31 forward-filled points, all using the primary currency
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(31);
			_ = result.Should().AllSatisfy(p =>
			{
				_ = p.TotalAssetValue.Currency.Should().Be(expectedCurrency);
				_ = p.TotalInvested.Currency.Should().Be(expectedCurrency);
				_ = p.CashBalance.Currency.Should().Be(expectedCurrency);
				_ = p.TotalValue.Currency.Should().Be(expectedCurrency);
			});
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_WhenAccountDataStartsAfterRangeStart_ShouldBeginFromFirstDataDate()
		{
			// Arrange – simulates "Nexo" whose first transaction is Feb 21 while the
			// selected range starts Jan 1.  No data should be fabricated before Feb 21.
			DateOnly rangeStart = new(2023, 1, 1);
			DateOnly rangeEnd = new(2023, 3, 31);
			DateOnly firstDataDate = new(2023, 2, 21);
			var accountId = 1;

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(firstDataDate, accountId, 500m, 400m)
			};
			List<Balance> balances = new()
			{
				CreateBalance(firstDataDate, accountId, 100m)
			};

			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(rangeStart, rangeEnd, CancellationToken.None);

			// Assert – points only from Feb 21 to Mar 31 (39 days); nothing before Feb 21
			var expectedDays = rangeEnd.DayNumber - firstDataDate.DayNumber + 1;
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(expectedDays);
			_ = result.Min(p => p.Date).Should().Be(firstDataDate);
			_ = result.Max(p => p.Date).Should().Be(rangeEnd);
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_WhenAccountDataEndsBeforeRangeEnd_ShouldForwardFillToRangeEnd()
		{
			// Arrange – simulates "Nexo" whose last transaction is Feb 27 while the
			// selected range ends today.  The last known value must be carried forward.
			DateOnly rangeStart = new(2023, 2, 21);
			DateOnly rangeEnd = new(2023, 3, 31);
			DateOnly lastDataDate = new(2023, 2, 27);
			var accountId = 1;

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(rangeStart, accountId, 400m, 350m),
				CreateCalculatedSnapshot(lastDataDate, accountId, 600m, 500m)
			};
			List<Balance> balances = new()
			{
				CreateBalance(rangeStart, accountId, 50m),
				CreateBalance(lastDataDate, accountId, 80m)
			};

			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(rangeStart, rangeEnd, CancellationToken.None);

			// Assert – every day in the range is present
			var expectedDays = rangeEnd.DayNumber - rangeStart.DayNumber + 1;
			_ = result.Should().HaveCount(expectedDays);

			// Days after the last data point carry the Feb 27 values forward
			DateOnly march1 = new(2023, 3, 1);
			var march1Point = result.Single(p => p.Date == march1);
			_ = march1Point.TotalAssetValue.Amount.Should().Be(600m);
			_ = march1Point.CashBalance.Amount.Should().Be(80m);

			var lastPoint = result.Single(p => p.Date == rangeEnd);
			_ = lastPoint.TotalAssetValue.Amount.Should().Be(600m);
			_ = lastPoint.CashBalance.Amount.Should().Be(80m);
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_WhenLastKnownDataIsBeforeRangeStart_ShouldForwardFillFromRangeStart()
		{
			// Arrange – account had its last transaction in Dec 2022; the filter starts
			// Jan 1 2023.  The Dec value must seed the forward-fill from Jan 1.
			DateOnly lastActivityDate = new(2022, 12, 15);
			DateOnly rangeStart = new(2023, 1, 1);
			DateOnly rangeEnd = new(2023, 1, 5);
			var accountId = 1;

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(lastActivityDate, accountId, 750m, 600m)
			};
			List<Balance> balances = new()
			{
				CreateBalance(lastActivityDate, accountId, 120m)
			};

			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(rangeStart, rangeEnd, CancellationToken.None);

			// Assert – 5 days in range, all carrying forward the Dec 15 values
			_ = result.Should().HaveCount(5);
			_ = result.Min(p => p.Date).Should().Be(rangeStart);
			_ = result.Should().AllSatisfy(p =>
			{
				_ = p.TotalAssetValue.Amount.Should().Be(750m);
				_ = p.TotalInvested.Amount.Should().Be(600m);
				_ = p.CashBalance.Amount.Should().Be(120m);
				_ = p.TotalValue.Amount.Should().Be(870m); // 750 + 120
			});
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_WithSparseTransactionDates_ShouldForwardFillBetweenDataPoints()
		{
			// Arrange – account has transactions on Jan 1 and Jan 5 only.
			// Jan 2, 3, 4 must forward-fill the Jan 1 values; Jan 5+ use the new value.
			DateOnly startDate = new(2023, 1, 1);
			DateOnly endDate = new(2023, 1, 7);
			var accountId = 1;

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(startDate, accountId, 1000m, 900m),
				CreateCalculatedSnapshot(startDate.AddDays(4), accountId, 1200m, 1100m)
			};
			List<Balance> balances = new()
			{
				CreateBalance(startDate, accountId, 100m),
				CreateBalance(startDate.AddDays(4), accountId, 150m)
			};

			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetAccountValueHistoryAsync(startDate, endDate, CancellationToken.None);

			// Assert
			_ = result.Should().HaveCount(7);

			// Jan 1 – actual data
			_ = result.Single(p => p.Date == startDate).TotalAssetValue.Amount.Should().Be(1000m);
			_ = result.Single(p => p.Date == startDate).CashBalance.Amount.Should().Be(100m);

			// Jan 2-4 – forward-filled from Jan 1
			foreach (var day in new[] { 1, 2, 3 })
			{
				var point = result.Single(p => p.Date == startDate.AddDays(day));
				_ = point.TotalAssetValue.Amount.Should().Be(1000m);
				_ = point.CashBalance.Amount.Should().Be(100m);
			}

			// Jan 5 – new transaction values
			_ = result.Single(p => p.Date == startDate.AddDays(4)).TotalAssetValue.Amount.Should().Be(1200m);
			_ = result.Single(p => p.Date == startDate.AddDays(4)).CashBalance.Amount.Should().Be(150m);

			// Jan 6-7 – forward-filled from Jan 5
			foreach (var day in new[] { 5, 6 })
			{
				var point = result.Single(p => p.Date == startDate.AddDays(day));
				_ = point.TotalAssetValue.Amount.Should().Be(1200m);
				_ = point.CashBalance.Amount.Should().Be(150m);
			}
		}

		#endregion

		#region GetMinDateAsync Tests

		[Fact]
		public async Task GetMinDateAsync_WithActivities_ShouldReturnMinimumDate()
		{
			// Arrange
			DateOnly expectedMinDate = new(2020, 1, 1);
			var platform = CreateTestPlatform("Test Platform");
			var account = CreateTestAccount("Test Account", 1, platform);
			var holding = CreateTestHolding(CreateTestSymbolProfile("TEST"));

			List<BuyActivity> activities = new()
			{
				CreateTestActivity(holding, 1, new DateTime(2023, 1, 1)),
				CreateTestActivity(holding, 1, expectedMinDate.ToDateTime(TimeOnly.MinValue)),
				CreateTestActivity(holding, 1, new DateTime(2022, 6, 15))
			};

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			var result = await _accountDataService.GetMinDateAsync(CancellationToken.None);

			// Assert
			_ = result.Should().Be(expectedMinDate);
		}

		[Fact]
		public async Task GetMinDateAsync_WithEmptyDatabase_ShouldThrowInvalidOperationException()
		{
			// Arrange
			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(new List<BuyActivity>());

			// Act & Assert
			var action = () => _accountDataService.GetMinDateAsync(CancellationToken.None);
			_ = await action.Should().ThrowAsync<InvalidOperationException>()
				.WithMessage("Sequence contains no elements");
		}

		[Fact]
		public async Task GetMinDateAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			CancellationToken cancellationToken = new();
			var platform = CreateTestPlatform("Test Platform");
			var account = CreateTestAccount("Test Account", 1, platform);
			var holding = CreateTestHolding(CreateTestSymbolProfile("TEST"));
			List<BuyActivity> activities = new()
			{
				CreateTestActivity(holding, 1, new DateTime(2023, 1, 1))
			};

			_ = _mockDatabaseContext.Setup(x => x.Activities).ReturnsDbSet(activities);

			// Act
			_ = await _accountDataService.GetMinDateAsync(cancellationToken);

			// Assert
			_mockDatabaseContext.Verify(x => x.Activities, Times.AtLeastOnce);
		}

		#endregion

		#region GetSymbolProfilesAsync Tests

		[Fact]
		public async Task GetSymbolProfilesAsync_WithoutAccountFilter_ShouldReturnAllSymbolsOrderedBySymbol()
		{
			// Arrange
			List<SymbolProfile> symbolProfiles = new()
			{
				CreateTestSymbolProfile("ZULU"),
				CreateTestSymbolProfile("AAPL"),
				CreateTestSymbolProfile("MSFT"),
				CreateTestSymbolProfile("BETA")
			};

			_ = _mockDatabaseContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(symbolProfiles);

			// Act
			var result = await _accountDataService.GetSymbolProfilesAsync(null, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(4);
			_ = result[0].Should().Be("AAPL");
			_ = result[1].Should().Be("BETA");
			_ = result[2].Should().Be("MSFT");
			_ = result[3].Should().Be("ZULU");
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

			List<Holding> holdings = new()
			{ holding1, holding2, holding3 };

			_ = _mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);

			// Act
			var result = await _accountDataService.GetSymbolProfilesAsync(accountId, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(2);
			_ = result.Should().Contain("AAPL");
			_ = result.Should().Contain("MSFT");
			_ = result.Should().NotContain("GOOGL");
		}

		[Fact]
		public async Task GetSymbolProfilesAsync_WithAccountFilterButNoMatchingHoldings_ShouldReturnEmptyList()
		{
			// Arrange
			var accountId = 999; // Non-existent account
			List<Holding> holdings = new();

			_ = _mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);

			// Act
			var result = await _accountDataService.GetSymbolProfilesAsync(accountId, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetSymbolProfilesAsync_WithEmptyDatabase_ShouldReturnEmptyList()
		{
			// Arrange
			_ = _mockDatabaseContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile>());

			// Act
			var result = await _accountDataService.GetSymbolProfilesAsync(null, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetSymbolProfilesAsync_WithCancellationToken_ShouldPassTokenToDatabase()
		{
			// Arrange
			_ = _mockDatabaseContext.Setup(x => x.SymbolProfiles).ReturnsDbSet(new List<SymbolProfile>());

			// Act
			_ = await _accountDataService.GetSymbolProfilesAsync(null, CancellationToken.None);

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

			List<Holding> holdings = new()
			{ holdingZ, holdingA, holdingM };

			_ = _mockDatabaseContext.Setup(x => x.Holdings).ReturnsDbSet(holdings);

			// Act
			var result = await _accountDataService.GetSymbolProfilesAsync(accountId, CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(3);
			_ = result[0].Should().Be("AAPL");
			_ = result[1].Should().Be("MSFT");
			_ = result[2].Should().Be("ZULU");
		}

		#endregion

		#region GetTaxReportAsync Tests

		[Fact]
		public async Task GetTaxReportAsync_WhenCacheIsValid_ShouldReturnCachedResultWithoutHittingDatabase()
		{
			// Arrange
			List<TaxReportRow> cached = new()
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

			_ = _mockTaxReportCacheService.Setup(x => x.IsValid).Returns(true);
			_ = _mockTaxReportCacheService.Setup(x => x.CachedResult).Returns(cached);

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			_ = result.Should().BeSameAs(cached);
			_mockDatabaseContext.Verify(x => x.CalculatedSnapshots, Times.Never);
			_mockDatabaseContext.Verify(x => x.Balances, Times.Never);
		}

		[Fact]
		public async Task GetTaxReportAsync_WhenCacheIsInvalid_ShouldStoreResultInCache()
		{
			// Arrange
			var account = CreateTestAccount("Broker A", 1);
			DateOnly jan1 = new(2023, 1, 1);

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>
			{
				CreateCalculatedSnapshot(jan1, 1, 1000m, 900m)
			});
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			List<TaxReportRow>? storedResult = null;
			_ = _mockTaxReportCacheService
				.Setup(x => x.Store(It.IsAny<List<TaxReportRow>>()))
				.Callback<List<TaxReportRow>>(r => storedResult = r);

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			_mockTaxReportCacheService.Verify(x => x.Store(It.IsAny<List<TaxReportRow>>()), Times.Once);
			_ = storedResult.Should().NotBeNull();
			_ = storedResult.Should().BeSameAs(result);
		}

		[Fact]
		public async Task GetTaxReportAsync_WithEmptyDatabase_ShouldReturnEmptyList()
		{
			// Arrange
			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account>());
			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetTaxReportAsync_WithOnlySnapshots_ShouldReturnRowsForJan1AndDec31()
		{
			// Arrange
			var account = CreateTestAccount("Broker A", 1);
			DateOnly jan1 = new(2023, 1, 1);
			DateOnly dec31 = new(2023, 12, 31);

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(jan1, 1, 1000m, 900m),
				CreateCalculatedSnapshot(dec31, 1, 1500m, 1400m)
			};

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().HaveCount(2);
			_ = result[0].Date.Should().Be(jan1);
			_ = result[0].TotalValue.Amount.Should().Be(1000m);
			_ = result[1].Date.Should().Be(dec31);
			_ = result[1].TotalValue.Amount.Should().Be(1500m);
		}

		[Fact]
		public async Task GetTaxReportAsync_WithCashBalance_ShouldAddBalanceToTotal()
		{
			// Arrange
			var account = CreateTestAccount("Broker A", 1);
			DateOnly jan1 = new(2023, 1, 1);

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(jan1, 1, 1000m, 900m)
			};
			List<Balance> balances = new()
			{
				CreateBalance(jan1, 1, 250m)
			};

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			_ = result.Should().NotBeNull();
			var jan1Row = result.First(r => r.Date == jan1);
			_ = jan1Row.AssetValue.Amount.Should().Be(1000m);
			_ = jan1Row.CashBalance.Amount.Should().Be(250m);
			_ = jan1Row.TotalValue.Amount.Should().Be(1250m);
		}

		[Fact]
		public async Task GetTaxReportAsync_WithBalanceFromPriorYear_ShouldUseItForJan1()
		{
			// Arrange - balance on Dec 31 2022 should be picked up for Jan 1 2023
			var account = CreateTestAccount("Broker A", 1);
			DateOnly dec31Prior = new(2022, 12, 31);
			DateOnly jan1 = new(2023, 1, 1);
			DateOnly dec31 = new(2023, 12, 31);

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(jan1, 1, 1000m, 900m),
				CreateCalculatedSnapshot(dec31, 1, 1500m, 1400m)
			};
			List<Balance> balances = new()
			{
				CreateBalance(dec31Prior, 1, 500m), // prior-year balance, no Jan 1 balance exists
				CreateBalance(dec31, 1, 600m)
			};

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			var jan1Row = result.First(r => r.Year == 2023 && r.Date == jan1);
			_ = jan1Row.CashBalance.Amount.Should().Be(500m); // picked up from Dec 31 prior year
		}

		[Fact]
		public async Task GetTaxReportAsync_ShouldPickClosestSnapshotOnOrBeforeTargetDate()
		{
			// Arrange - snapshot on Dec 29 should be used for Dec 31 (no Dec 31 snapshot)
			var account = CreateTestAccount("Broker A", 1);
			DateOnly jan1 = new(2023, 1, 1);
			DateOnly dec29 = new(2023, 12, 29);

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(jan1, 1, 1000m, 900m),
				CreateCalculatedSnapshot(dec29, 1, 1400m, 1300m)
			};

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			var dec31Row = result.First(r => r.Year == 2023 && r.Date == new DateOnly(2023, 12, 31));
			_ = dec31Row.AssetValue.Amount.Should().Be(1400m); // from Dec 29 snapshot
		}

		[Fact]
		public async Task GetTaxReportAsync_WithMultipleAccounts_ShouldReturnRowsPerAccount()
		{
			// Arrange
			var account1 = CreateTestAccount("Broker A", 1);
			var account2 = CreateTestAccount("Broker B", 2);
			DateOnly jan1 = new(2023, 1, 1);

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(jan1, 1, 1000m, 900m),
				CreateCalculatedSnapshot(jan1, 2, 2000m, 1800m)
			};

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account1, account2 });
			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			List<TaxReportRow> jan1Rows = result.Where(r => r.Date == jan1).OrderBy(r => r.AccountName).ToList();
			_ = jan1Rows.Should().HaveCount(2);
			_ = jan1Rows[0].AccountName.Should().Be("Broker A");
			_ = jan1Rows[0].AssetValue.Amount.Should().Be(1000m);
			_ = jan1Rows[1].AccountName.Should().Be("Broker B");
			_ = jan1Rows[1].AssetValue.Amount.Should().Be(2000m);
		}

		[Fact]
		public async Task GetTaxReportAsync_ShouldUseAccountNameFromAccountsTable()
		{
			// Arrange
			var account = CreateTestAccount("My Named Account", 42);
			DateOnly jan1 = new(2023, 1, 1);

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(jan1, 42, 1000m, 900m)
			};

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			_ = result.Should().HaveCount(2); // Jan 1 and Dec 31 both pick up the only available snapshot
			_ = result.Should().AllSatisfy(r => r.AccountName.Should().Be("My Named Account"));
		}

		[Fact]
		public async Task GetTaxReportAsync_WithUnknownAccountId_ShouldFallbackToAccountIdLabel()
		{
			// Arrange - snapshot references accountId 99 which has no Account record
			DateOnly jan1 = new(2023, 1, 1);
			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(jan1, 99, 1000m, 900m)
			};

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account>());
			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			_ = result.Should().HaveCount(2); // Jan 1 and Dec 31 both pick up the only available snapshot
			_ = result.Should().AllSatisfy(r => r.AccountName.Should().Be("Account 99"));
		}

		[Fact]
		public async Task GetTaxReportAsync_ShouldReturnRowsOrderedByYearThenDateThenAccountName()
		{
			// Arrange
			var account1 = CreateTestAccount("Zulu", 1);
			var account2 = CreateTestAccount("Alpha", 2);
			DateOnly jan1 = new(2023, 1, 1);
			DateOnly dec31 = new(2023, 12, 31);

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(dec31, 1, 1000m, 900m),
				CreateCalculatedSnapshot(dec31, 2, 500m, 450m),
				CreateCalculatedSnapshot(jan1, 1, 800m, 700m),
				CreateCalculatedSnapshot(jan1, 2, 400m, 350m)
			};

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account1, account2 });
			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			_ = result[0].Date.Should().Be(jan1);
			_ = result[0].AccountName.Should().Be("Alpha");
			_ = result[1].Date.Should().Be(jan1);
			_ = result[1].AccountName.Should().Be("Zulu");
			_ = result[2].Date.Should().Be(dec31);
			_ = result[2].AccountName.Should().Be("Alpha");
			_ = result[3].Date.Should().Be(dec31);
			_ = result[3].AccountName.Should().Be("Zulu");
		}

		[Fact]
		public async Task GetTaxReportAsync_CurrentYear_ShouldUseTodayAsEndDate()
		{
			// Arrange
			var account = CreateTestAccount("Broker A", 1);
			DateOnly today = DateOnly.FromDateTime(DateTime.Today);
			DateOnly jan1 = new(today.Year, 1, 1);

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(jan1, 1, 1000m, 900m),
				CreateCalculatedSnapshot(today, 1, 1200m, 1100m)
			};

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			List<DateOnly> dates = result.Select(r => r.Date).Distinct().OrderBy(d => d).ToList();
			_ = dates.Should().Contain(jan1);
			_ = dates.Should().Contain(today);
			_ = dates.Should().NotContain(new DateOnly(today.Year, 12, 31));
		}

		[Fact]
		public async Task GetTaxReportAsync_WithBalanceOnlyAccount_ShouldIncludeItWithZeroAssets()
		{
			// Arrange - account has a balance but no snapshots
			var account = CreateTestAccount("Cash Account", 1);
			DateOnly jan1 = new(2023, 1, 1);

			List<Balance> balances = new()
			{
				CreateBalance(jan1, 1, 5000m)
			};

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(new List<CalculatedSnapshot>());
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(balances);

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			// The single Jan 1 balance is also the closest value for Dec 31, so both dates appear
			_ = result.Should().HaveCount(2);
			_ = result.Should().AllSatisfy(r =>
			{
				_ = r.AssetValue.Amount.Should().Be(0m);
				_ = r.CashBalance.Amount.Should().Be(5000m);
				_ = r.TotalValue.Amount.Should().Be(5000m);
			});
		}

		[Fact]
		public async Task GetTaxReportAsync_ShouldUsePrimaryCurrencyForAllRows()
		{
			// Arrange
			_ = _mockServerConfigurationService.Setup(x => x.PrimaryCurrency).Returns(Currency.EUR);
			var account = CreateTestAccount("Broker A", 1);
			DateOnly jan1 = new(2023, 1, 1);

			List<CalculatedSnapshot> snapshots = new()
			{
				CreateCalculatedSnapshot(jan1, 1, 1000m, 900m)
			};

			_ = _mockDatabaseContext.Setup(x => x.Accounts).ReturnsDbSet(new List<Account> { account });
			_ = _mockDatabaseContext.Setup(x => x.CalculatedSnapshots).ReturnsDbSet(snapshots);
			_ = _mockDatabaseContext.Setup(x => x.Balances).ReturnsDbSet(new List<Balance>());

			// Act
			var result = await _accountDataService.GetTaxReportAsync(CancellationToken.None);

			// Assert
			// The single Jan 1 snapshot is also the closest value for Dec 31, so 2 rows appear
			_ = result.Should().HaveCount(2);
			_ = result.Should().AllSatisfy(r =>
			{
				_ = r.TotalValue.Currency.Should().Be(Currency.EUR);
				_ = r.AssetValue.Currency.Should().Be(Currency.EUR);
				_ = r.CashBalance.Currency.Should().Be(Currency.EUR);
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
			Holding holding = new() { Id = 1, Activities = [] };
			holding.SymbolProfiles.Add(symbolProfile);
			return holding;
		}

		private static BuyActivity CreateTestActivity(Holding holding, int accountId = 1, DateTime? date = null)
		{
			Account account = new("Test Account") { Id = accountId };
			return new BuyActivity(
				account,
				holding,
				[],
				date ?? DateTime.Now,
				1m,
				new Money(Currency.USD, 100m),
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
				Holding = new Holding { SymbolProfiles = [new SymbolProfile { Symbol = "TEST" }] },
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

