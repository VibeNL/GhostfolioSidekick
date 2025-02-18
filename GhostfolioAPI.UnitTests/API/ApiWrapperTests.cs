using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Shouldly;
using GhostfolioSidekick.Database.Repository;
using RestSharp;
using Microsoft.Extensions.Caching.Memory;
using GhostfolioSidekick.GhostfolioAPI.Contract;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API
{
	public class ApiWrapperTests
	{
		private readonly Mock<IRestClient> _mockRestCall;
		private readonly Mock<ILogger<ApiWrapper>> _mockLogger;
		private readonly Mock<ICurrencyExchange> _mockCurrencyExchange;
		private readonly ApiWrapper _apiWrapper;

		public ApiWrapperTests()
		{
			_mockRestCall = new Mock<IRestClient>();
			_mockLogger = new Mock<ILogger<ApiWrapper>>();
			_mockCurrencyExchange = new Mock<ICurrencyExchange>();
			_apiWrapper = new ApiWrapper(new RestCall(_mockRestCall.Object, new MemoryCache(new MemoryCacheOptions()), Mock.Of<ILogger<RestCall>>(), "a", "a", new RestCallOptions()), _mockLogger.Object, _mockCurrencyExchange.Object);

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>())).ReturnsAsync(new Money { Amount = 100, Currency = Currency.EUR });

			_mockRestCall
					.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(y => y.Resource.Contains("auth")), default))
					.ReturnsAsync(new RestResponse
					{
						StatusCode = System.Net.HttpStatusCode.OK,
						IsSuccessStatusCode = true,
						ResponseStatus = ResponseStatus.Completed,
						Content = JsonConvert.SerializeObject(new Token
						{
							AuthToken = "a",
						})
					});
		}

		[Fact]
		public async Task CreateAccount_ShouldCreateAccount()
		{
			// Arrange
			var account = new Model.Accounts.Account { Name = "TestAccount", Balance = new List<Model.Accounts.Balance>(), Comment = "TestComment" };
			SetupRestCall("api/v1/account/", string.Empty);

			// Act
			Func<Task> act = async () => await _apiWrapper.CreateAccount(account);

			// Assert
			await act.ShouldNotThrowAsync();
			_mockRestCall.Verify(x => x.ExecuteAsync(It.Is<RestRequest>(y => y.Resource.Contains("api/v1/account/")), default), Times.Once);
			_mockLogger.VerifyLog(x => x.LogDebug("Created account {Name}", account.Name), Times.Once);
		}

		[Fact]
		public async Task CreatePlatform_ShouldCreatePlatform()
		{
			// Arrange
			var platform = new Model.Accounts.Platform { Name = "TestPlatform", Url = "http://test.com" };
			SetupRestCall("api/v1/platform/", string.Empty);

			// Act
			Func<Task> act = async () => await _apiWrapper.CreatePlatform(platform);

			// Assert
			await act.ShouldNotThrowAsync();
			_mockRestCall.Verify(x => x.ExecuteAsync(It.Is<RestRequest>(y => y.Resource.Contains("api/v1/platform/")), default), Times.Once);
			_mockLogger.VerifyLog(x => x.LogDebug("Created platform {Name}", platform.Name), Times.Once);
		}

		[Fact]
		public async Task GetAccountByName_ShouldReturnAccount()
		{
			// Arrange
			var accountName = "TestAccount";
			var accounts = new List<Contract.Account> { new() { Name = accountName, Currency = "EUR", Id = "a" } };
			SetupRestCall("api/v1/account", JsonConvert.SerializeObject(new { Accounts = accounts }));
			SetupRestCall("api/v1/platform", JsonConvert.SerializeObject(new List<Contract.Platform> { }));

			// Act
			var result = await _apiWrapper.GetAccountByName(accountName);

			// Assert
			result.ShouldNotBeNull();
			result!.Name.ShouldBe(accountName);
		}

		[Fact]
		public async Task GetPlatformByName_ShouldReturnPlatform()
		{
			// Arrange
			var platformName = "TestPlatform";
			var platforms = new List<Contract.Platform> { new Contract.Platform { Name = platformName, Id = "a" } };
			SetupRestCall("api/v1/platform", JsonConvert.SerializeObject(platforms));

			// Act
			var result = await _apiWrapper.GetPlatformByName(platformName);

			// Assert
			result.ShouldNotBeNull();
			result!.Name.ShouldBe(platformName);
		}

		[Fact]
		public async Task GetSymbolProfile_ShouldReturnSymbolProfiles()
		{
			// Arrange
			var identifier = "TestSymbol";
			var symbolProfiles = new List<Contract.SymbolProfile> { new Contract.SymbolProfile { Symbol = identifier, AssetClass = "", Countries = Array.Empty<Country>(), Currency = "EUR", DataSource = "DUMMY", Name = identifier, Sectors = Array.Empty<Sector>() } };
			SetupRestCall("api/v1/symbol", JsonConvert.SerializeObject(new { Items = symbolProfiles }));

			// Act
			var result = await _apiWrapper.GetSymbolProfile(identifier, true);

			// Assert
			result.ShouldNotBeNull();
			result.ShouldHaveSingleItem();
			result.First().Symbol.ShouldBe(identifier);
		}

		[Fact]
		public async Task SyncAllActivities_ShouldSyncActivities()
		{
			// Arrange
			var identifier = "TestSymbol";
			var accountName = "TestAccount";
			var symbol = new Contract.SymbolProfile { Symbol = identifier, AssetClass = "", Countries = Array.Empty<Country>(), Currency = "EUR", DataSource = "DUMMY", Name = identifier, Sectors = Array.Empty<Sector>() };
			var contractActivities = new ActivityList { Activities = [new Contract.Activity { SymbolProfile = symbol }] };
			SetupRestCall("api/v1/order", JsonConvert.SerializeObject(contractActivities));
			SetupRestCall("api/v1/admin/market-data/", string.Empty);
			var accounts = new List<Contract.Account> { new() { Name = accountName, Currency = "EUR", Id = "a" } };
			SetupRestCall("api/v1/account", JsonConvert.SerializeObject(new { Accounts = accounts }));

			var modelActivities = new List<Model.Activities.Activity> {
				new Model.Activities.Types.BuySellActivity {
					Holding = new Model.Holding {
						SymbolProfiles = [new Model.Symbols.SymbolProfile { 
							Symbol = identifier, 
							Currency = Currency.EUR,
							DataSource = Datasource.GHOSTFOLIO + "_DUMMY" }]
					},
					Account = new Model.Accounts.Account(){
						Name = accountName, 
						Balance = [],
						Comment = accountName
					} 
				}
			};

			// Act
			Func<Task> act = async () => await _apiWrapper.SyncAllActivities(modelActivities);

			// Assert
			await act.ShouldNotThrowAsync();
			_mockRestCall.Verify(x => x.ExecuteAsync(It.Is<RestRequest>(y => y.Resource.Contains("api/v1/order") && y.Method == Method.Post), default), Times.Once);
			_mockLogger.VerifyLog(x => x.LogDebug("Applying changes"), Times.Once);
		}

		[Fact]
		public async Task UpdateAccount_ShouldUpdateAccount()
		{
			// Arrange
			var account = new Model.Accounts.Account { Name = "TestAccount", Balance = new List<Model.Accounts.Balance> { new Model.Accounts.Balance(DateOnly.FromDateTime(DateTime.Now), new Money { Amount = 100 }) } };
			var accounts = new List<Contract.Account> { new Contract.Account { Name = account.Name, Id = "1", Currency = "EUR" } };
			SetupRestCall("api/v1/account", JsonConvert.SerializeObject(new { Accounts = accounts }));

			// Act
			Func<Task> act = async () => await _apiWrapper.UpdateAccount(account);

			// Assert
			await act.ShouldNotThrowAsync();
			_mockRestCall.Verify(x => x.ExecuteAsync(It.Is<RestRequest>(y => y.Resource.Contains("api/v1/account/1/balances")), default), Times.Once);
			_mockRestCall.Verify(x => x.ExecuteAsync(It.Is<RestRequest>(y => y.Resource.Contains("api/v1/account-balance/")), default), Times.Once);
		}

		private void SetupRestCall(string suffix, string content)
		{
			_mockRestCall
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(y => y.Resource.Contains(suffix)), default))
				.ReturnsAsync(new RestResponse
				{
					StatusCode = System.Net.HttpStatusCode.OK,
					IsSuccessStatusCode = true,
					ResponseStatus = ResponseStatus.Completed,
					Content = content
				});
		}
	}
}
