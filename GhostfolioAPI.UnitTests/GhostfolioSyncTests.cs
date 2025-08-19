using GhostfolioSidekick.GhostfolioAPI.API;
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
		public async Task SyncAccount_ShouldCreateAndSyncAccount()
		{
			// Arrange
			var account = new Account { Name = "TestAccount", Platform = new Platform { Name = "TestPlatform" } };
			_apiWrapperMock.Setup(x => x.GetPlatformByName(It.IsAny<string>())).ReturnsAsync((Platform?)null);
			_apiWrapperMock.Setup(x => x.GetAccountByName(It.IsAny<string>())).ReturnsAsync((Account?)null);

			// Act
			await _ghostfolioSync.SyncAccount(account);

			// Assert
			_apiWrapperMock.Verify(x => x.CreatePlatform(It.IsAny<Platform>()), Times.Once);
			_apiWrapperMock.Verify(x => x.CreateAccount(It.IsAny<Account>()), Times.Once);
			_apiWrapperMock.Verify(x => x.UpdateAccount(It.IsAny<Account>()), Times.Once);
		}

		[Fact]
		public async Task SyncAllActivities_ShouldSyncAllActivities()
		{
			// Arrange
			var activities = new List<Activity> { new BuySellActivity() { Account = new Account() } };
			_apiWrapperMock.Setup(x => x.SyncAllActivities(It.IsAny<List<Activity>>())).Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncAllActivities(activities);

			// Assert
			_apiWrapperMock.Verify(x => x.SyncAllActivities(It.IsAny<List<Activity>>()), Times.Once);
		}

		[Fact]
		public async Task SyncSymbolProfiles_ShouldSyncSymbolProfiles()
		{
			// Arrange
			var symbolProfiles = new List<SymbolProfile> { new SymbolProfile() };
			_apiWrapperMock.Setup(x => x.SyncSymbolProfiles(It.IsAny<IEnumerable<SymbolProfile>>())).Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncSymbolProfiles(symbolProfiles);

			// Assert
			_apiWrapperMock.Verify(x => x.SyncSymbolProfiles(It.IsAny<IEnumerable<SymbolProfile>>()), Times.Once);
		}

		[Fact]
		public async Task SyncMarketData_ShouldSyncMarketData()
		{
			// Arrange
			var profile = new SymbolProfile();
			var marketDataList = new List<MarketData> { new MarketData() };
			_apiWrapperMock.Setup(x => x.SyncMarketData(It.IsAny<SymbolProfile>(), It.IsAny<ICollection<MarketData>>())).Returns(Task.CompletedTask);

			// Act
			await _ghostfolioSync.SyncMarketData(profile, marketDataList);

			// Assert
			_apiWrapperMock.Verify(x => x.SyncMarketData(It.IsAny<SymbolProfile>(), It.IsAny<ICollection<MarketData>>()), Times.Once);
		}
	}
}