using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model.Accounts;
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

		private readonly Fixture fixture = new Fixture();
		private readonly Mock<IAccountService> accountService;
		private readonly ActivitiesService activitiesService;

		public ActivitiesServiceTests()
		{
			var loggerMock = new Mock<ILogger<ActivitiesService>>();
			var exchange = new Mock<IExchangeRateService>();
			accountService = new Mock<IAccountService>();

			activitiesService = new ActivitiesService(
				exchange.Object,
				accountService.Object,
				restCall: restCall,
				logger: loggerMock.Object);

			fixture.Customize(new ContractConventions());
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
	}
}
