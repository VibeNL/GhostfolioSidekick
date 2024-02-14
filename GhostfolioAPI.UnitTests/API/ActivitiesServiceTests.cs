using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Compare;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using RestSharp;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API
{
	public class ActivitiesServiceTests : BaseAPITests
	{
		private const string orderUrl = "api/v1/order";


		private readonly Fixture fixture = DefaultFixture.Create();
		private readonly Mock<IExchangeRateService> exchangeRateService;
		private readonly Mock<IAccountService> accountService;
		private readonly ActivitiesService activitiesService;

		public ActivitiesServiceTests()
		{
			var loggerMock = new Mock<ILogger<ActivitiesService>>();
			exchangeRateService = new Mock<IExchangeRateService>();
			accountService = new Mock<IAccountService>();

			activitiesService = new ActivitiesService(
				exchangeRateService.Object,
				accountService.Object,
				restCall: restCall,
				logger: loggerMock.Object);
		}

		[Fact]
		public async Task GetAllActivities_Success()
		{
			// Arrange
			var account = fixture.Create<Account>();
			var activitiesFromService = fixture
									.CreateMany<Contract.Activity>(10)
									.ToArray();
			activitiesFromService.ToList().ForEach(x => x.AccountId = account.Id);
			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl)), default))
				.ReturnsAsync(CreateResponse(
					true,
					JsonConvert.SerializeObject(new Contract.ActivityList()
					{
						Activities = activitiesFromService
					})));
			accountService.Setup(x => x.GetAllAccounts()).ReturnsAsync([account]);

			// Act
			var activities = await activitiesService.GetAllActivities();

			// Assert
			restClient.Verify(
				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl)), default),
				Times.Once);
			activities.Should().HaveCount(10);
		}

		[Fact]
		public async Task InsertActivity_Success()
		{
			// Arrange
			var symbolProfile = fixture.Create<Model.Symbols.SymbolProfile>();
			var newActivity = DefaultFixture.Create().Create<Activity>();

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl) && x.Method == Method.Post), default))
				.ReturnsAsync(CreateResponse(true, string.Empty));

			// Act
			await activitiesService.InsertActivity(symbolProfile, newActivity);

			// Assert
			restClient.Verify(
				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl) && x.Method == Method.Post), default),
				Times.Once);
		}

		[Fact]
		public async Task UpdateActivity_Success()
		{
			// Arrange
			var symbolProfile = fixture.Create<Model.Symbols.SymbolProfile>();
			var oldActivity = DefaultFixture.Create().Create<Activity>();
			var newActivity = DefaultFixture.Create().Create<Activity>();

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl) && x.Method == Method.Delete), default))
				.ReturnsAsync(CreateResponse(true, string.Empty));

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl) && x.Method == Method.Post), default))
				.ReturnsAsync(CreateResponse(true, string.Empty));

			// Act
			await activitiesService.UpdateActivity(symbolProfile, oldActivity, newActivity);

			// Assert
			restClient.Verify(
				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl) && x.Method == Method.Delete), default),
				Times.Once);

			restClient.Verify(
				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl) && x.Method == Method.Post), default),
				Times.Once);
		}

		[Fact]
		public async Task DeleteActivity_Success()
		{
			// Arrange
			var symbolProfile = fixture.Create<Model.Symbols.SymbolProfile>();
			var oldActivity = DefaultFixture.Create().Create<Activity>();

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl) && x.Method == Method.Delete), default))
				.ReturnsAsync(CreateResponse(true, string.Empty));

			// Act
			await activitiesService.DeleteActivity(symbolProfile, oldActivity);

			// Assert
			restClient.Verify(
				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl) && x.Method == Method.Delete), default),
				Times.Once);
		}
	}
}
