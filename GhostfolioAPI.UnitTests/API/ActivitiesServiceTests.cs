using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;
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
		private readonly Mock<ILogger<ActivitiesService>> loggerMock;
		private readonly Mock<IExchangeRateService> exchangeRateServiceMock;
		private readonly Mock<IAccountService> accountServiceMock;
		private readonly ActivitiesService activitiesService;

		public ActivitiesServiceTests()
		{
			loggerMock = new Mock<ILogger<ActivitiesService>>();
			exchangeRateServiceMock = new Mock<IExchangeRateService>();
			accountServiceMock = new Mock<IAccountService>();

			exchangeRateServiceMock
				.Setup(x => x.GetConversionRate(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTime>()))
				.ReturnsAsync(1);

			activitiesService = new ActivitiesService(
				exchangeRateServiceMock.Object,
				accountServiceMock.Object,
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
					System.Net.HttpStatusCode.OK,
					JsonConvert.SerializeObject(new Contract.ActivityList()
					{
						Activities = activitiesFromService
					})));
			accountServiceMock.Setup(x => x.GetAllAccounts()).ReturnsAsync([account]);

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
			var newActivity = DefaultFixture.Create().Create<BuySellActivity>();

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl) && x.Method == Method.Post), default))
				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK, string.Empty));

			// Act
			await activitiesService.InsertActivity(symbolProfile, newActivity);

			// Assert
			restClient.Verify(
				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl) && x.Method == Method.Post), default),
				Times.Once);
		}

		[Fact]
		public async Task InsertActivity_IgnoreType_Ignored()
		{
			// Arrange
			var symbolProfile = fixture.Create<Model.Symbols.SymbolProfile>();
			var newActivity = DefaultFixture.Create().Create<CashDepositWithdrawalActivity>();

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl) && x.Method == Method.Post), default))
				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK, string.Empty));

			// Act
			await activitiesService.InsertActivity(symbolProfile, newActivity);

			// Assert
			restClient.Verify(x => x.ExecuteAsync(It.IsAny<RestRequest>(), default), Times.Never);
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Trace,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.StartsWith("Skipping ignore transaction")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
		}

		[Fact]
		public async Task InsertActivity_NullQuantity_Written()
		{
			// Arrange
			var symbolProfile = fixture.Create<Model.Symbols.SymbolProfile>();
			var newActivity = DefaultFixture.Create().Create<BuySellActivity>();
			newActivity.Quantity = 0;

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl) && x.Method == Method.Post), default))
				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK, string.Empty));

			// Act
			await activitiesService.InsertActivity(symbolProfile, newActivity);

			// Assert
			restClient.Verify(x => x.ExecuteAsync(It.IsAny<RestRequest>(), default), Times.Exactly(2));
		}

		[Fact]
		public async Task UpdateActivity_Success()
		{
			// Arrange
			var symbolProfile = fixture.Create<Model.Symbols.SymbolProfile>();
			var oldActivity = DefaultFixture.Create().Create<BuySellActivity>();
			var newActivity = DefaultFixture.Create().Create<BuySellActivity>();

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl) && x.Method == Method.Delete), default))
				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK, string.Empty));

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl) && x.Method == Method.Post), default))
				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK, string.Empty));

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
			var oldActivity = DefaultFixture.Create().Create<BuySellActivity>();

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl) && x.Method == Method.Delete), default))
				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK, string.Empty));

			// Act
			await activitiesService.DeleteActivity(symbolProfile, oldActivity);

			// Assert
			restClient.Verify(
				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl) && x.Method == Method.Delete), default),
				Times.Once);
		}
	}
}
