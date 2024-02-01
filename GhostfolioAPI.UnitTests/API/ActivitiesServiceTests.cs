using FluentAssertions;
using GhostfolioSidekick.GhostfolioAPI.API;
using Microsoft.Extensions.Logging;
using Moq;
using RestSharp;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API
{
	public class ActivitiesServiceTests : BaseAPITests
	{
		private const string orderUrl = "api/v1/order";

		private readonly ActivitiesService activitiesService;

		public ActivitiesServiceTests()
		{
			var loggerMock = new Mock<ILogger<ActivitiesService>>();
			var exchange = new Mock<IExchangeRateService>();
			var accountService = new Mock<IAccountService>();

			activitiesService = new ActivitiesService(
				exchange.Object,
				accountService.Object,
				restCall,
				loggerMock.Object);
		}

		[Fact]
		public async Task GetAllActivities_Success()
		{
			// Arrange
			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl)), default))
				.ReturnsAsync(CreateResponse(
					true,
					"{\"activities\":[{\"accountId\":\"ddbcf8de-0e49-438c-b0e2-9e3b9de0629c\",\"accountUserId\":\"b403a187-b727-4b57-a96c-493582f8184f\",\"comment\":\"Transaction Reference: [Buy_DE0001102333_2023-10-06_1_EUR_1] (Details: asset DE0001102333)\",\"createdAt\":\"2024-01-31T12:43:49.366Z\",\"date\":\"2023-10-06T00:00:00.000Z\",\"fee\":1,\"id\":\"24891883-8d32-4653-8732-ce7b322c87d1\",\"isDraft\":false,\"quantity\":1,\"symbolProfileId\":\"c4380744-c63d-48cd-a759-cbca706d0e4c\",\"type\":\"BUY\",\"unitPrice\":100,\"updatedAt\":\"2024-01-31T12:43:49.366Z\",\"userId\":\"b403a187-b727-4b57-a96c-493582f8184f\",\"Account\":{\"balance\":9384.89770507,\"comment\":null,\"createdAt\":\"2023-11-30T12:18:48.892Z\",\"currency\":\"EUR\",\"id\":\"ddbcf8de-0e49-438c-b0e2-9e3b9de0629c\",\"isDefault\":false,\"isExcluded\":false,\"name\":\"Trade Republic\",\"platformId\":\"be0e5f74-af04-44c5-95ea-1dd2ba2473b8\",\"updatedAt\":\"2024-01-31T12:38:44.245Z\",\"userId\":\"b403a187-b727-4b57-a96c-493582f8184f\",\"Platform\":{\"id\":\"be0e5f74-af04-44c5-95ea-1dd2ba2473b8\",\"name\":\"Trade Republic\",\"url\":\"https://traderepublic.com/en-nl\"}},\"SymbolProfile\":{\"assetClass\":\"EQUITY\",\"assetSubClass\":\"BOND\",\"comment\":\"Known Identifiers: [DE0001102333]\",\"countries\":null,\"createdAt\":\"2024-01-11T08:45:28.872Z\",\"currency\":\"EUR\",\"dataSource\":\"MANUAL\",\"figi\":null,\"figiComposite\":null,\"figiShareClass\":null,\"id\":\"c4380744-c63d-48cd-a759-cbca706d0e4c\",\"isin\":null,\"name\":\"Bond Germany Feb 2024\",\"updatedAt\":\"2024-01-11T08:45:29.308Z\",\"scraperConfiguration\":{},\"sectors\":null,\"symbol\":\"DE0001102333\",\"symbolMapping\":{},\"url\":null},\"tags\":[],\"value\":100,\"feeInBaseCurrency\":1,\"valueInBaseCurrency\":100}],\"count\":7}"));

			// Act
			var activities = await activitiesService.GetAllActivities();

			// Assert
			restClient.Verify(
				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl)), default),
				Times.Once);
			activities.Should().HaveCount(1);
		}

	}
}
