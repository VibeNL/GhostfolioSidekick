using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using RestSharp;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API
{
	public class MarketDataServiceTests : BaseAPITests
	{
		private const string marketDataUrl = "api/v1/market-data";
		private const string marketDataAdminUrl = $"api/v1/admin/market-data/";
		private const string findSymbolUrl = "api/v1/symbol/lookup";

		private readonly MemoryCache memoryCache;
		private readonly Mock<ILogger<MarketDataService>> loggerMock;
		private readonly MarketDataService marketDataService;

		public MarketDataServiceTests()
		{
			memoryCache = new MemoryCache(new MemoryCacheOptions());
			loggerMock = new Mock<ILogger<MarketDataService>>();

			marketDataService = new MarketDataService(
				new ApplicationSettings(),
				memoryCache,
				restCall,
				loggerMock.Object);
		}

		[Fact]
		public async Task FindSymbolByIdentifier_Success()
		{
			// Arrange
			var content = DefaultFixture.Create().Create<SymbolProfileList>();
			var serialized = JsonConvert.SerializeObject(content);
			var asymbol = content.Items[0];

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(marketDataAdminUrl)), default))
				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK));
			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(findSymbolUrl)), default))
				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK, serialized));

			// Act
			var result = await marketDataService.FindSymbolByIdentifier(
								[asymbol.ISIN], 
								new Currency(asymbol.Currency),
								[AssetClass.Equity],
								[AssetSubClass.Etf],
								true, 
								false);

			// Assert
			result.Should().NotBeNull();
			result.ISIN.Should().Be(asymbol.ISIN);
		}

		[Fact]
		public async Task FindSymbolByIdentifier_DoesNotExists_NothingFound()
		{
			// Arrange
			var content = DefaultFixture.Create().Create<SymbolProfileList>();
			var serialized = JsonConvert.SerializeObject(content);
			var asymbol = content.Items[0];

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(marketDataAdminUrl)), default))
				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK));
			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(findSymbolUrl)), default))
				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK, serialized));

			// Act
			var result = await marketDataService.FindSymbolByIdentifier(
								[asymbol.ISIN],
								new Currency(asymbol.Currency),
								[AssetClass.Cash],
								[AssetSubClass.CryptoCurrency],
								true,
								false);

			// Assert
			result.Should().BeNull();
		}
	}
}
