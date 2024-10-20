//using AutoFixture;
//using FluentAssertions;
//using GhostfolioSidekick.Configuration;
//using GhostfolioSidekick.GhostfolioAPI.API;
//using GhostfolioSidekick.Model.Accounts;
//using Microsoft.Extensions.Logging;
//using Moq;
//using RestSharp;

//namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API
//{
//	public class AccountServiceTests : BaseAPITests
//	{
//		private const string platformUrl = "api/v1/platform";
//		private const string accountUrl = "api/v1/account";

//		private readonly Mock<IApplicationSettings> applicationSettingsMock;
//		private readonly AccountService accountService;

//		public AccountServiceTests()
//		{
//			var loggerMock = new Mock<ILogger<AccountService>>();

//			applicationSettingsMock = new Mock<IApplicationSettings>();
//			accountService = new AccountService(
//				applicationSettingsMock.Object,
//				restCall,
//				loggerMock.Object);
//		}

//		[Fact]
//		public async Task CreatePlatform_NoAdmin_Success()
//		{
//			// Arrange
//			applicationSettingsMock.Setup(x => x.AllowAdminCalls).Returns(false);

//			restClient
//				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(platformUrl)), default))
//				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK));

//			var platform = new Fixture().Create<Platform>();

//			// Act
//			await accountService.CreatePlatform(platform);

//			// Assert
//			restClient.Verify(
//				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(platformUrl)), default),
//				Times.Never);
//		}

//		[Fact]
//		public async Task CreatePlatform_Success()
//		{
//			// Arrange
//			applicationSettingsMock.Setup(x => x.AllowAdminCalls).Returns(true);

//			restClient
//				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(platformUrl)), default))
//				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK));

//			var platform = new Fixture().Create<Platform>();

//			// Act
//			await accountService.CreatePlatform(platform);

//			// Assert
//			restClient.Verify(
//				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(platformUrl)), default),
//				Times.Once);
//		}

//		[Fact]
//		public async Task CreatePlatform_Failed()
//		{
//			// Arrange
//			applicationSettingsMock.Setup(x => x.AllowAdminCalls).Returns(true);

//			restClient
//				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(platformUrl)), default))
//				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.BadRequest));

//			var platform = new Fixture().Create<Platform>();

//			// Act
//			var test = async () => await accountService.CreatePlatform(platform);

//			// Assert
//			await test.Should().ThrowAsync<NotSupportedException>();
//		}

//		[Fact]
//		public async Task CreateAccount_Success()
//		{
//			// Arrange
//			applicationSettingsMock.Setup(x => x.AllowAdminCalls).Returns(true);
//			var account = new Fixture()
//				.Build<Account>()
//				.With(x => x.Platform, new Platform("Platform1"))
//				.Create();

//			restClient
//				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(accountUrl)), default))
//				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK));
//			restClient
//				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(platformUrl)), default))
//				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK, "[{\"id\":\"11d9f728-12ee-4868-b7cc-0ec06ef9a1c4\",\"name\":\"Platform1\",\"url\":\"https://www.google.nl\",\"accountCount\":1}]"));

//			// Act
//			await accountService.CreateAccount(account);

//			// Assert
//			restClient.Verify(
//				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(accountUrl)), default),
//				Times.Once);
//		}

//		[Fact]
//		public async Task CreateAccount_Failed()
//		{
//			// Arrange
//			applicationSettingsMock.Setup(x => x.AllowAdminCalls).Returns(true);
//			var account = new Fixture()
//				.Build<Account>()
//				.With(x => x.Platform, new Platform("Platform1"))
//				.Create();

//			restClient
//				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(accountUrl)), default))
//				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.BadRequest));
//			restClient
//				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(platformUrl)), default))
//				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK, "[{\"id\":\"11d9f728-12ee-4868-b7cc-0ec06ef9a1c4\",\"name\":\"Platform1\",\"url\":\"https://www.google.nl\",\"accountCount\":1}]"));

//			// Act
//			var test = async () => await accountService.CreateAccount(account);

//			// Assert
//			await test.Should().ThrowAsync<NotSupportedException>();
//		}
//	}
//}
