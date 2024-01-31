using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model.Accounts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using RestSharp;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API
{
	public class AccountServiceTests
	{
		private const string platformUrl = "api/v1/platform";
		private const string accountUrl = "api/v1/account";

		private readonly Mock<IApplicationSettings> applicationSettingsMock;
		private readonly Mock<IRestClient> restClient;
		private readonly AccountService _accountService;

		public AccountServiceTests()
		{
			applicationSettingsMock = new Mock<IApplicationSettings>();
			restClient = new Mock<IRestClient>();
			var restCall = new RestCall(restClient.Object, new MemoryCache(new MemoryCacheOptions()), new Mock<ILogger<RestCall>>().Object, "https://www.google.com", "wow");
			var loggerMock = new Mock<ILogger<AccountService>>();

			_accountService = new AccountService(
				applicationSettingsMock.Object,
				restCall,
				loggerMock.Object);

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains("api/v1/auth/anonymous")), default))
				.ReturnsAsync(CreateResponse(true, "{\"authToken\":\"abcd\"}"));
		}

		[Fact]
		public async Task CreatePlatform_Success()
		{
			// Arrange
			applicationSettingsMock.Setup(x => x.AllowAdminCalls).Returns(true);

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(platformUrl)), default))
				.ReturnsAsync(CreateResponse(true));

			var platform = new Fixture().Create<Platform>();

			// Act
			await _accountService.CreatePlatform(platform);

			// Assert
			restClient.Verify(
				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(platformUrl)), default),
				Times.Once);
		}

		[Fact]
		public async Task CreatePlatform_Failed()
		{
			// Arrange
			applicationSettingsMock.Setup(x => x.AllowAdminCalls).Returns(true);

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(platformUrl)), default))
				.ReturnsAsync(CreateResponse(false));

			var platform = new Fixture().Create<Platform>();

			// Act
			var test = async () => await _accountService.CreatePlatform(platform);

			// Assert
			await test.Should().ThrowAsync<NotSupportedException>();
		}

		[Fact]
		public async Task CreateAccount_Success()
		{
			// Arrange
			applicationSettingsMock.Setup(x => x.AllowAdminCalls).Returns(true);
			var account = new Fixture()
				.Build<Account>()
				.With(x => x.Platform, new Platform("Platform1"))
				.Create();

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(accountUrl)), default))
				.ReturnsAsync(CreateResponse(true));
			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(platformUrl)), default))
				.ReturnsAsync(CreateResponse(true, "[{\"id\":\"11d9f728-12ee-4868-b7cc-0ec06ef9a1c4\",\"name\":\"Platform1\",\"url\":\"https://www.google.nl\",\"accountCount\":1}]"));

			// Act
			await _accountService.CreateAccount(account);

			// Assert
			restClient.Verify(
				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(accountUrl)), default),
				Times.Once);
		}

		[Fact]
		public async Task CreateAccount_Failed()
		{
			// Arrange
			applicationSettingsMock.Setup(x => x.AllowAdminCalls).Returns(true);
			var account = new Fixture()
				.Build<Account>()
				.With(x => x.Platform, new Platform("Platform1"))
				.Create();

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(accountUrl)), default))
				.ReturnsAsync(CreateResponse(false));
			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(platformUrl)), default))
				.ReturnsAsync(CreateResponse(true, "[{\"id\":\"11d9f728-12ee-4868-b7cc-0ec06ef9a1c4\",\"name\":\"Platform1\",\"url\":\"https://www.google.nl\",\"accountCount\":1}]"));


			// Act
			var test = async () => await _accountService.CreateAccount(account);

			// Assert
			await test.Should().ThrowAsync<NotSupportedException>();
		}

		private RestResponse CreateResponse(bool succesfull, string? response = null)
		{
			return new RestResponse
			{
				IsSuccessStatusCode = succesfull,
				StatusCode = succesfull ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.BadRequest,
				ResponseStatus = ResponseStatus.Completed,
				Content = response
			};
		}

	}

}
