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
			Assert.Throws<ArgumentNullException>(() => new GhostfolioSync(null!, _loggerMock.Object));
		}

		[Fact]
		public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
		{
			// Arrange & Act & Assert
			Assert.Throws<ArgumentNullException>(() => new GhostfolioSync(_apiWrapperMock.Object, null!));
		}

		[Fact]
		public async Task SyncAccount_ShouldCreatePlatformAndAccount_WhenBothDoNotExist()
		{
			// Arrange
			var platform = new Platform { Name = "TestPlatform" };
			var account = new Account { Name = "TestAccount", Platform = platform, SyncBalance = true };
			_apiWrapperMock.Setup(x => x.GetPlatformByName("TestPlatform")).ReturnsAsync((Platform?)null);
			_apiWrapperMock.Setup(x => x.GetAccountByName("TestAccount")).ReturnsAsync((Account?)null);

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
			var platform = new Platform { Name = "TestPlatform" };
			var account = new Account { Name = "TestAccount", Platform = platform, SyncBalance = true };
			_apiWrapperMock.Setup(x => x.GetPlatformByName("TestPlatform")).ReturnsAsync(platform);
			_apiWrapperMock.Setup(x => x.GetAccountByName("TestAccount")).ReturnsAsync((Account?)null);

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
			var platform = new Platform { Name = "TestPlatform" };
			var account = new Account { Name = "TestAccount", Platform = platform, SyncBalance = true };
			_apiWrapperMock.Setup(x => x.GetPlatformByName("TestPlatform")).ReturnsAsync(platform);
			_apiWrapperMock.Setup(x => x.GetAccountByName("TestAccount")).ReturnsAsync(account);

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
			var platform = new Platform { Name = "TestPlatform" };
			var account = new Account { Name = "TestAccount", Platform = platform, SyncBalance = false };
			_apiWrapperMock.Setup(x => x.GetPlatformByName("TestPlatform")).ReturnsAsync(platform);
			_apiWrapperMock.Setup(x => x.GetAccountByName("TestAccount")).ReturnsAsync(account);

			// Act
			await _ghostfolioSync.SyncAccount(account);

			// Assert
			_apiWrapperMock.Verify(x => x.UpdateAccount(It.IsAny<Account>()), Times.Never);
		}

		[Fact]
		public async Task SyncAccount_ShouldHandleAccountWithoutPlatform()
		{
			// Arrange
			var account = new Account { Name = "TestAccount", Platform = null, SyncBalance = true };
			_apiWrapperMock.Setup(x => x.GetAccountByName("TestAccount")).ReturnsAsync((Account?)null);

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
			var account = new Account { Name = "TestAccount", SyncActivities = true };
			var activities = new List<Activity> { new BuyActivity(account, null, [], DateTime.Now, 10, new Money(Currency.USD, 100), "tx1", null, null) };
			_apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>())).Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			_apiWrapperMock.Verify(x => x.SyncAllActivities(It.IsAny<List<Activity>>()), Times.Once);
		}

		[Fact]
		public async Task SyncAllActivities_ShouldFilterActivitiesBasedOnSyncActivitiesFlag()
		{
			// Arrange
			var syncAccount = new Account { Name = "SyncAccount", SyncActivities = true };
			var noSyncAccount = new Account { Name = "NoSyncAccount", SyncActivities = false };
			var activities = new List<Activity>
			{
				new BuyActivity(syncAccount, null, [], DateTime.Now, 10, new Money(Currency.USD, 100), "tx1", null, null),
				new BuyActivity(noSyncAccount, null, [], DateTime.Now, 5, new Money(Currency.USD, 50), "tx2", null, null)
			};

			List<Activity> capturedActivities = [];
			_apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			Assert.Single(capturedActivities);
			Assert.Equal("SyncAccount", capturedActivities[0].Account.Name);
		}

		[Fact]
		public async Task SyncAllActivities_ShouldConvertSendAndReceiveActivities()
		{
			// Arrange
			var account = new Account { Name = "TestAccount", SyncActivities = true };
			var sendReceiveActivity = new ReceiveActivity(account, null, [PartialSymbolIdentifier.CreateGeneric("TEST")], DateTime.Now, 10, "tx1", null, null);
			sendReceiveActivity.UnitPrice = new Money(Currency.USD, 100);
			var activities = new List<Activity> { sendReceiveActivity };

			List<Activity> capturedActivities = [];
			_apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			Assert.Single(capturedActivities);
			Assert.IsType<BuyActivity>(capturedActivities[0]);
			var buySellActivity = (BuyActivity)capturedActivities[0];
			Assert.Equal(10, buySellActivity.Quantity);
			Assert.Equal(100, buySellActivity.UnitPrice.Amount);
		}

		[Fact]
		public async Task SyncAllActivities_ShouldConvertGiftFiatToInterest()
		{
			// Arrange
			var account = new Account { Name = "TestAccount", SyncActivities = true };
			var giftFiatActivity = new GiftFiatActivity(account, null, DateTime.Now, new Money(Currency.USD, 100), "tx1", null, null);
			var activities = new List<Activity> { giftFiatActivity };

			List<Activity> capturedActivities = [];
			_apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			Assert.Single(capturedActivities);
			Assert.IsType<InterestActivity>(capturedActivities[0]);
			var interestActivity = (InterestActivity)capturedActivities[0];
			Assert.Equal(100, interestActivity.Amount.Amount);
		}

		[Fact]
		public async Task SyncAllActivities_ShouldConvertGiftAssetToBuy()
		{
			// Arrange
			var account = new Account { Name = "TestAccount", SyncActivities = true };
			var giftAssetActivity = new GiftAssetActivity(account, null, [PartialSymbolIdentifier.CreateGeneric("TEST")], DateTime.Now, 10, "tx1", null, null);
			giftAssetActivity.UnitPrice = new Money(Currency.USD, 100);
			var activities = new List<Activity> { giftAssetActivity };

			List<Activity> capturedActivities = [];
			_apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			Assert.Single(capturedActivities);
			Assert.IsType<BuyActivity>(capturedActivities[0]);
			var buySellActivity = (BuyActivity)capturedActivities[0];
			Assert.Equal(10, buySellActivity.Quantity);
		}

		[Fact]
		public async Task SyncAllActivities_ShouldConvertRepayBondActivity()
		{
			// Arrange
			var account = new Account { Name = "TestAccount", SyncActivities = true };
			var holding = new Holding();
			var buyActivity = new BuyActivity(account, holding, [PartialSymbolIdentifier.CreateGeneric("TEST")], DateTime.Now.AddDays(-1), 100, new Money(Currency.USD, 10), "tx0", null, null);
			holding.Activities = [buyActivity];

			var repayBondActivity = new RepayBondActivity(account, holding, [PartialSymbolIdentifier.CreateGeneric("TEST")], DateTime.Now, new Money(Currency.USD, 1200), "tx1", null, null);
			var activities = new List<Activity> { repayBondActivity };

			List<Activity> capturedActivities = [];
			_apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			Assert.Single(capturedActivities);
			Assert.IsType<SellActivity>(capturedActivities[0]);
			var buySellActivity = (SellActivity)capturedActivities[0];
			Assert.Equal(100, buySellActivity.Quantity);
			Assert.Equal(12, buySellActivity.UnitPrice.Amount); // 1200 / 100
		}

		[Fact]
		public async Task SyncAllActivities_ShouldSkipRepayBondActivityWithoutHolding()
		{
			// Arrange
			var account = new Account { Name = "TestAccount", SyncActivities = true };
			var repayBondActivity = new RepayBondActivity(account, null, [PartialSymbolIdentifier.CreateGeneric("TEST")], DateTime.Now, new Money(Currency.USD, 1200), "tx1", null, null);
			var activities = new List<Activity> { repayBondActivity };

			List<Activity> capturedActivities = [];
			_apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
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
			var account = new Account { Name = "TestAccount", SyncActivities = true };
			var dividendActivity = new DividendActivity(account, null, [PartialSymbolIdentifier.CreateGeneric("TEST")], DateTime.Now, new Money(Currency.USD, -50), "tx1", null, null);
			var activities = new List<Activity> { dividendActivity };

			List<Activity> capturedActivities = [];
			_apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			Assert.Single(capturedActivities);
			Assert.IsType<FeeActivity>(capturedActivities[0]);
			var feeActivity = (FeeActivity)capturedActivities[0];
			Assert.Equal(50, feeActivity.Amount.Amount); // Should be positive
		}

		[Fact]
		public async Task SyncAllActivities_ShouldNotConvertPositiveDividend()
		{
			// Arrange
			var account = new Account { Name = "TestAccount", SyncActivities = true };
			var dividendActivity = new DividendActivity(account, null, [PartialSymbolIdentifier.CreateGeneric("TEST")], DateTime.Now, new Money(Currency.USD, 50), "tx1", null, null);
			var activities = new List<Activity> { dividendActivity };

			List<Activity> capturedActivities = [];
			_apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			Assert.Single(capturedActivities);
			Assert.IsType<DividendActivity>(capturedActivities[0]);
			var resultDividendActivity = (DividendActivity)capturedActivities[0];
			Assert.Equal(50, resultDividendActivity.Amount.Amount);
		}

		[Fact]
		public async Task SyncAllActivities_ShouldHandleAdjustedQuantityAndPrice()
		{
			// Arrange
			var account = new Account { Name = "TestAccount", SyncActivities = true };
			var sendReceiveActivity = new ReceiveActivity(account, null, [PartialSymbolIdentifier.CreateGeneric("TEST")], DateTime.Now, 10, "tx1", null, null);
			sendReceiveActivity.UnitPrice = new Money(Currency.USD, 100);
			sendReceiveActivity.AdjustedQuantity = 20;
			sendReceiveActivity.AdjustedUnitPrice = new Money(Currency.USD, 200);
			var activities = new List<Activity> { sendReceiveActivity };

			List<Activity> capturedActivities = [];
			_apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>()))
				.Callback<List<Activity>>(acts => capturedActivities = acts)
				.Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			Assert.Single(capturedActivities);
			Assert.IsType<BuyActivity>(capturedActivities[0]);
			var buySellActivity = (BuyActivity)capturedActivities[0];
			Assert.Equal(20, buySellActivity.Quantity); // Uses adjusted quantity
			Assert.Equal(200, buySellActivity.UnitPrice.Amount); // Uses adjusted unit price
		}

		[Fact]
		public async Task SyncSymbolProfiles_ShouldSyncSymbolProfiles()
		{
			// Arrange
			var symbolProfiles = new List<SymbolProfile> { new() };
			_apiWrapperMock.Setup(x => x.SyncSymbolProfiles(It.IsAny<IEnumerable<SymbolProfile>>())).Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncSymbolProfiles(symbolProfiles);

			// Assert
			_apiWrapperMock.Verify(x => x.SyncSymbolProfiles(symbolProfiles), Times.Once);
		}

		[Fact]
		public async Task SyncMarketData_ShouldSyncMarketData()
		{
			// Arrange
			var profile = new SymbolProfile();
			var marketDataList = new List<MarketData> { new() };
			_apiWrapperMock.Setup(x => x.SyncMarketData(It.IsAny<SymbolProfile>(), It.IsAny<ICollection<MarketData>>())).Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncMarketData(profile, marketDataList);

			// Assert
			_apiWrapperMock.Verify(x => x.SyncMarketData(profile, marketDataList), Times.Once);
		}
	}
}