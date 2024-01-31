using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI.API;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using RestSharp;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API
{
	public class AccountServiceTests
	{
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
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains("api/v1/platform/")), default))
				.ReturnsAsync(CreateResponse(true));

			var platform = new Model.Accounts.Platform("TestPlatform");

			// Act
			await _accountService.CreatePlatform(platform);

			// Assert
			restClient.Verify(
				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains("api/v1/platform/")), default),
				Times.Once);
		}

		private RestResponse CreateResponse(bool succesfull, string? response = null)
		{
			return new RestResponse { IsSuccessStatusCode = succesfull, ResponseStatus = ResponseStatus.Completed, Content = response };
		}

	}

}
