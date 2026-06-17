using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests
{
	public class GhostfolioSyncTests
	{
		private readonly Mock<IApiWrapper> _apiWrapperMock;
		private readonly Mock<ILogger<GhostfolioSync>> _loggerMock;
		private readonly GhostfolioSync _ghostfolioSync;

		public GhostfolioSyncTests()
		{
			_apiWrapperMock = new Mock<IApiWrapper>();
			_loggerMock = new Mock<ILogger<GhostfolioSync>>();
			_ghostfolioSync = new GhostfolioSync(_apiWrapperMock.Object, _loggerMock.Object);
		}

		[Fact]
		public void Constructor_ThrowsArgumentNullException_WhenApiWrapperIsNull()
		{
			// Arrange & Act & Assert
			_ = Assert.Throws<ArgumentNullException>(() => new GhostfolioSync(null!, _loggerMock.Object));
		}

		[Fact]
		public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
		{
			// Arrange & Act & Assert
			_ = Assert.Throws<ArgumentNullException>(() => new GhostfolioSync(_apiWrapperMock.Object, null!));
		}

		[Fact]
		public async Task SyncAccount_ShouldCreatePlatformAndAccount_WhenBothDoNotExist()
		{
			// Arrange
			Platform platform = new() { Name = "TestPlatform" };
			Account account = new() { Name = "TestAccount", Platform = platform, SyncBalance = true };
			_ = _apiWrapperMock.Setup(x => x.GetPlatformByName("TestPlatform")).ReturnsAsync((Platform?)null);
			_ = _apiWrapperMock.Setup(x => x.GetAccountByName("TestAccount")).ReturnsAsync((Account?)null);

			// Act
			await _ghostfolioSync.SyncAccount(account);

			// Assert
			_apiWrapperMock.Verify(x => x.CreatePlatform(platform), Times.Once);
			_apiWrapperMock.Verify(x => x.CreateAccount(account), Times.Once);
			_apiWrapperMock.Verify(x => x.UpdateAccount(account), Times.Once);
		}

		[Fact]
		public async Task SyncAccount_ShouldNotCreatePlatform_WhenPlatformExists()
		{
			// Arrange
			Platform platform = new() { Name = "TestPlatform" };
			Account account = new() { Name = "TestAccount", Platform = platform, SyncBalance = true };
			_ = _apiWrapperMock.Setup(x => x.GetPlatformByName("TestPlatform")).ReturnsAsync(platform);
			_ = _apiWrapperMock.Setup(x => x.GetAccountByName("TestAccount")).ReturnsAsync((Account?)null);

			// Act
			await _ghostfolioSync.SyncAccount(account);

			// Assert
			_apiWrapperMock.Verify(x => x.CreatePlatform(It.IsAny<Platform>()), Times.Never);
			_apiWrapperMock.Verify(x => x.CreateAccount(account), Times.Once);
			_apiWrapperMock.Verify(x => x.UpdateAccount(account), Times.Once);
		}

		[Fact]
		public async Task SyncAccount_ShouldNotCreateAccount_WhenAccountExists()
		{
			// Arrange
			Platform platform = new() { Name = "TestPlatform" };
			Account account = new() { Name = "TestAccount", Platform = platform, SyncBalance = true };
			_ = _apiWrapperMock.Setup(x => x.GetPlatformByName("TestPlatform")).ReturnsAsync(platform);
			_ = _apiWrapperMock.Setup(x => x.GetAccountByName("TestAccount")).ReturnsAsync(account);

			// Act
			await _ghostfolioSync.SyncAccount(account);

			// Assert
			_apiWrapperMock.Verify(x => x.CreatePlatform(It.IsAny<Platform>()), Times.Never);
			_apiWrapperMock.Verify(x => x.CreateAccount(It.IsAny<Account>()), Times.Never);
			_apiWrapperMock.Verify(x => x.UpdateAccount(account), Times.Once);
		}

		[Fact]
		public async Task SyncAccount_ShouldNotUpdateAccount_WhenSyncBalanceIsDisabled()
		{
			// Arrange
			Platform platform = new() { Name = "TestPlatform" };
			Account account = new() { Name = "TestAccount", Platform = platform, SyncBalance = false };
			_ = _apiWrapperMock.Setup(x => x.GetPlatformByName("TestPlatform")).ReturnsAsync(platform);
			_ = _apiWrapperMock.Setup(x => x.GetAccountByName("TestAccount")).ReturnsAsync(account);

			// Act
			await _ghostfolioSync.SyncAccount(account);

			// Assert
			_apiWrapperMock.Verify(x => x.UpdateAccount(It.IsAny<Account>()), Times.Never);
		}

		[Fact]
		public async Task SyncAccount_ShouldHandleAccountWithoutPlatform()
		{
			// Arrange
			Account account = new() { Name = "TestAccount", Platform = null, SyncBalance = true };
			_ = _apiWrapperMock.Setup(x => x.GetAccountByName("TestAccount")).ReturnsAsync((Account?)null);

			// Act
			await _ghostfolioSync.SyncAccount(account);

			// Assert
			_apiWrapperMock.Verify(x => x.GetPlatformByName(It.IsAny<string>()), Times.Never);
			_apiWrapperMock.Verify(x => x.CreatePlatform(It.IsAny<Platform>()), Times.Never);
			_apiWrapperMock.Verify(x => x.CreateAccount(account), Times.Once);
			_apiWrapperMock.Verify(x => x.UpdateAccount(account), Times.Once);
		}

		[Fact]
		public async Task SyncAllActivities_ShouldConvertAndSyncActivities()
		{
			// Arrange
			Account account = new() { Name = "TestAccount", SyncActivities = true };
			List<Activity> activities = [new BuyActivity(account, null, [], DateTime.Now, 10, new Money(Currency.USD, 100), new Money(Currency.USD, 100), "tx1", null, null)];
			_ = _apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>())).Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			_apiWrapperMock.Verify(x => x.SyncAllActivities(It.IsAny<List<Activity>>()), Times.Once);
		}

		[Fact]
		public async Task SyncAllActivities_ShouldFilterActivitiesBasedOnSyncActivitiesFlag()
		{
			// Arrange
			Account syncAccount = new() { Name = "SyncAccount", SyncActivities = true };
			Account noSyncAccount = new() { Name = "NoSyncAccount", SyncActivities = false };
			List<Activity> activities =
			[
				new BuyActivity(syncAccount, null, [], DateTime.Now, 10, new Money(Currency.USD, 100), new Money(Currency.USD, 100), "tx1", null, null),
				new BuyActivity(noSyncAccount, null, [], DateTime.Now, 5, new Money(Currency.USD, 50), new Money(Currency.USD, 50), "tx2", null, null)
			];

			List<Activity> capturedActivities = [];
			_ = _apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			_ = Assert.Single(capturedActivities);
			Assert.Equal("SyncAccount", capturedActivities[0].Account.Name);
		}

		[Fact]
		public async Task SyncAllActivities_ShouldConvertSendAndReceiveActivities()
		{
			// Arrange
			Account account = new() { Name = "TestAccount", SyncActivities = true };
			ReceiveActivity sendReceiveActivity = new(account, null, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST", null)!], DateTime.Now, 10, "tx1", null, null)
			{
				UnitPrice = new Money(Currency.USD, 100)
			};
			List<Activity> activities = [sendReceiveActivity];

			List<Activity> capturedActivities = [];
			_ = _apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			_ = Assert.Single(capturedActivities);
			_ = Assert.IsType<BuyActivity>(capturedActivities[0]);
			BuyActivity buySellActivity = (BuyActivity)capturedActivities[0];
			Assert.Equal(10, buySellActivity.Quantity);
			Assert.Equal(100, buySellActivity.UnitPrice.Amount);
		}

		[Fact]
		public async Task SyncAllActivities_ShouldConvertGiftFiatToInterest()
		{
			// Arrange
			Account account = new() { Name = "TestAccount", SyncActivities = true };
			GiftFiatActivity giftFiatActivity = new(account, null, DateTime.Now, new Money(Currency.USD, 100), "tx1", null, null);
			List<Activity> activities = [giftFiatActivity];

			List<Activity> capturedActivities = [];
			_ = _apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			_ = Assert.Single(capturedActivities);
			_ = Assert.IsType<InterestActivity>(capturedActivities[0]);
			InterestActivity interestActivity = (InterestActivity)capturedActivities[0];
			Assert.Equal(100, interestActivity.Amount.Amount);
		}

		[Fact]
		public async Task SyncAllActivities_ShouldConvertGiftAssetToBuy()
		{
			// Arrange
			Account account = new() { Name = "TestAccount", SyncActivities = true };
			GiftAssetActivity giftAssetActivity = new(account, null, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST", null)!], DateTime.Now, 10, "tx1", null, null)
			{
				UnitPrice = new Money(Currency.USD, 100)
			};
			List<Activity> activities = [giftAssetActivity];

			List<Activity> capturedActivities = [];
			_ = _apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			_ = Assert.Single(capturedActivities);
			_ = Assert.IsType<BuyActivity>(capturedActivities[0]);
			BuyActivity buySellActivity = (BuyActivity)capturedActivities[0];
			Assert.Equal(10, buySellActivity.Quantity);
		}

		[Fact]
		public async Task SyncAllActivities_ShouldConvertRepayBondActivity()
		{
			// Arrange
			Account account = new() { Name = "TestAccount", SyncActivities = true };
			Holding holding = new();
			BuyActivity buyActivity = new(account, holding, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST", null)!], DateTime.Now.AddDays(-1), 100, new Money(Currency.USD, 10), new Money(Currency.USD, 10), "tx0", null, null);
			holding.Activities = [buyActivity];

			RepayBondActivity repayBondActivity = new(account, holding, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST", null)!], DateTime.Now, new Money(Currency.USD, 1200), "tx1", null, null);
			List<Activity> activities = [repayBondActivity];

			List<Activity> capturedActivities = [];
			_ = _apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			_ = Assert.Single(capturedActivities);
			_ = Assert.IsType<SellActivity>(capturedActivities[0]);
			SellActivity buySellActivity = (SellActivity)capturedActivities[0];
			Assert.Equal(100, buySellActivity.Quantity);
			Assert.Equal(12, buySellActivity.UnitPrice.Amount); // 1200 / 100
		}

		[Fact]
		public async Task SyncAllActivities_ShouldSkipRepayBondActivityWithoutHolding()
		{
			// Arrange
			Account account = new() { Name = "TestAccount", SyncActivities = true };
			RepayBondActivity repayBondActivity = new(account, null, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST", null)!], DateTime.Now, new Money(Currency.USD, 1200), "tx1", null, null);
			List<Activity> activities = [repayBondActivity];

			List<Activity> capturedActivities = [];
			_ = _apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			Assert.Empty(capturedActivities);
		}

		[Fact]
		public async Task SyncAllActivities_ShouldConvertNegativeDividendToFee()
		{
			// Arrange
			Account account = new() { Name = "TestAccount", SyncActivities = true };
			DividendActivity dividendActivity = new(account, null, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST", null)!], DateTime.Now, new Money(Currency.USD, -50), "tx1", null, null);
			List<Activity> activities = [dividendActivity];

			List<Activity> capturedActivities = [];
			_ = _apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			_ = Assert.Single(capturedActivities);
			_ = Assert.IsType<FeeActivity>(capturedActivities[0]);
			FeeActivity feeActivity = (FeeActivity)capturedActivities[0];
			Assert.Equal(50, feeActivity.Amount.Amount); // Should be positive
		}

		[Fact]
		public async Task SyncAllActivities_ShouldNotConvertPositiveDividend()
		{
			// Arrange
			Account account = new() { Name = "TestAccount", SyncActivities = true };
			DividendActivity dividendActivity = new(account, null, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST", null)!], DateTime.Now, new Money(Currency.USD, 50), "tx1", null, null);
			List<Activity> activities = [dividendActivity];

			List<Activity> capturedActivities = [];
			_ = _apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			_ = Assert.Single(capturedActivities);
			_ = Assert.IsType<DividendActivity>(capturedActivities[0]);
			DividendActivity resultDividendActivity = (DividendActivity)capturedActivities[0];
			Assert.Equal(50, resultDividendActivity.Amount.Amount);
		}

		[Fact]
		public async Task SyncAllActivities_ShouldHandleAdjustedQuantityAndPrice()
		{
			// Arrange
			Account account = new() { Name = "TestAccount", SyncActivities = true };
			ReceiveActivity sendReceiveActivity = new(account, null, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST", null)!], DateTime.Now, 10, "tx1", null, null)
			{
				UnitPrice = new Money(Currency.USD, 100),
				AdjustedQuantity = 20,
				AdjustedUnitPrice = new Money(Currency.USD, 200)
			};
			List<Activity> activities = [sendReceiveActivity];

			List<Activity> capturedActivities = [];
			_ = _apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			_ = Assert.Single(capturedActivities);
			_ = Assert.IsType<BuyActivity>(capturedActivities[0]);
			BuyActivity buySellActivity = (BuyActivity)capturedActivities[0];
			Assert.Equal(20, buySellActivity.Quantity); // Uses adjusted quantity
			Assert.Equal(200, buySellActivity.UnitPrice.Amount); // Uses adjusted unit price
		}

		[Fact]
		public async Task SyncSymbolProfiles_ShouldSyncSymbolProfiles()
		{
			// Arrange
			List<SymbolProfile> symbolProfiles = [new()];
			_ = _apiWrapperMock.Setup(x => x.SyncSymbolProfiles(It.IsAny<IEnumerable<SymbolProfile>>())).Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncSymbolProfiles(symbolProfiles);

			// Assert
			_apiWrapperMock.Verify(x => x.SyncSymbolProfiles(symbolProfiles), Times.Once);
		}

		[Fact]
		public async Task SyncMarketData_ShouldSyncMarketData()
		{
			// Arrange
			SymbolProfile profile = new();
			List<MarketData> marketDataList = [new()];
			_ = _apiWrapperMock.Setup(x => x.SyncMarketData(It.IsAny<SymbolProfile>(), It.IsAny<ICollection<MarketData>>())).Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncMarketData(profile, marketDataList);

			// Assert
			_apiWrapperMock.Verify(x => x.SyncMarketData(profile, marketDataList), Times.Once);
		}
	}
}