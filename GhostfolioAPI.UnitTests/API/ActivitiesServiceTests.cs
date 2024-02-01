using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
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
		private readonly DateTime now = DateTime.UtcNow;
		private readonly Mock<IAccountService> accountService;
		private readonly ActivitiesService activitiesService;

		private int c = 0;

		public ActivitiesServiceTests()
		{
			var loggerMock = new Mock<ILogger<ActivitiesService>>();
			var exchange = new Mock<IExchangeRateService>();
			accountService = new Mock<IAccountService>();

			activitiesService = new ActivitiesService(
				exchange.Object,
				accountService.Object,
				restCall,
				loggerMock.Object);

			fixture.Customize(new ContractConventions());
		}

		[Fact]
		public async Task GetAllActivities_Success()
		{
			// Arrange
			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl)), default))
				.ReturnsAsync(CreateResponse(
					true,
					JsonConvert.SerializeObject(new Contract.ActivityList() { Activities = fixture.CreateMany<Contract.Activity>(10).ToArray() })));

			// Act
			var activities = await activitiesService.GetAllActivities();

			// Assert
			restClient.Verify(
				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl)), default),
				Times.Once);
			activities.Should().HaveCount(10);
		}

		[Fact]
		public async Task UpdateActivities_AllNew_Success()
		{
			// Arrange
			var account = new Fixture().Create<Account>();
			var symbolProfileStock = new Fixture()
				.Build<SymbolProfile>()
				.With(x => x.AssetClass, AssetClass.Equity)
				.With(x => x.AssetSubClass, AssetSubClass.Etf)
				.Create();
			var holding = new Holding(symbolProfileStock)
			{
				Activities = [
					CreateDummyActivity(account, ActivityType.Buy, 200),
					CreateDummyActivity(account, ActivityType.Sell, 100),
					CreateDummyActivity(account, ActivityType.Buy, 50),
					CreateDummyActivity(account, ActivityType.Buy, 50),
					CreateDummyActivity(account, ActivityType.Sell, 150),
				]
			};
			accountService.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl)), default))
				.ReturnsAsync(CreateResponse(true, JsonConvert.SerializeObject(new Contract.ActivityList() { Activities = [] })));

			// Act
			await activitiesService.UpdateActivities([account.Name], [holding]);

			// Assert
			restClient.Verify(
				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Method == Method.Post && x.Resource.Contains(orderUrl)), default),
				Times.Exactly(5));
		}

		[Fact]
		public async Task UpdateActivities_AllChanged_Success()
		{
			// Arrange
			var account = new Fixture().Create<Account>();
			var symbolProfileStock = new Fixture()
				.Build<SymbolProfile>()
				.With(x => x.AssetClass, AssetClass.Equity)
				.With(x => x.AssetSubClass, AssetSubClass.Etf)
				.Create();
			var holding = new Holding(symbolProfileStock)
			{
				Activities = [
					CreateDummyActivity(account, ActivityType.Buy, 200),
					CreateDummyActivity(account, ActivityType.Sell, 100),
					CreateDummyActivity(account, ActivityType.Buy, 50),
					CreateDummyActivity(account, ActivityType.Buy, 50),
					CreateDummyActivity(account, ActivityType.Sell, 150),
				]
			};
			accountService.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(orderUrl)), default))
				.ReturnsAsync(CreateResponse(true, JsonConvert.SerializeObject(new Contract.ActivityList()
				{
					Activities = fixture.CreateMany<Contract.Activity>(3).ToArray()
				})));

			// Act
			await activitiesService.UpdateActivities([account.Name], [holding]);

			// Assert
			restClient.Verify(
				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Method == Method.Delete && x.Resource.Contains(orderUrl)), default),
				Times.Exactly(3));
			restClient.Verify(
				x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Method == Method.Post && x.Resource.Contains(orderUrl)), default),
				Times.Exactly(5));
		}

		private Activity CreateDummyActivity(Account account, ActivityType type, decimal amount)
		{
			return new Activity(account, type, now.AddMinutes(c++), amount, new Money(Currency.EUR, 1), "A");
		}
	}

	internal class ContractConventions : ICustomization
	{
		public void Customize(IFixture fixture)
		{
			fixture.Customize<Contract.Activity>(composer =>
			composer
				.With(p => p.Type, Contract.ActivityType.BUY));
			fixture.Customize<Contract.SymbolProfile>(composer =>
			composer
				.With(p => p.AssetClass, AssetClass.Equity.ToString().ToUpperInvariant())
				.With(p => p.AssetSubClass, AssetSubClass.Etf.ToString().ToUpperInvariant()));
		}
	}
}
