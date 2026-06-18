using AwesomeAssertions;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using RestSharp;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests;

public class GhostfolioMarketDataTests
{
	[Fact]
	public async Task GetAllSymbolProfiles_WithValidData_ShouldReturnProfiles()
	{
		// Arrange
		var mockRestCall = new Mock<RestCall>();
		var mockLogger = new Mock<ILogger<GhostfolioMarketData>>();

		var marketDataContent = JsonConvert.SerializeObject(new MarketDataList
		{
			MarketData = [new() { DataSource = "YAHOO", Symbol = "AAPL" }],
			AssetProfile = new SymbolProfile { DataSource = "YAHOO", Symbol = "AAPL", Name = "Apple Inc.", Currency = "USD", AssetClass = "EQUITY", Countries = [], Sectors = [] }
		});

		var profileContent = JsonConvert.SerializeObject(new MarketDataList
		{
			MarketData = [],
			AssetProfile = new SymbolProfile
			{
				DataSource = "YAHOO",
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = "USD",
				AssetClass = "EQUITY",
				Countries = [],
				Sectors = []
			}
		});

		mockRestCall
			.Setup(x => x.DoRestGet(It.Is<string>(s => s.Contains("api/v1/admin/market-data/"))))
			.ReturnsAsync(marketDataContent);

		mockRestCall
			.Setup(x => x.DoRestGet(It.Is<string>(s => s.Contains("api/v1/market-data/"))))
			.ReturnsAsync(profileContent);

		var marketData = new GhostfolioMarketData(mockRestCall.Object, mockLogger.Object);

		// Act
		var result = await marketData.GetAllSymbolProfiles();

		// Assert
		result.Should().ContainSingle();
		result.First().Symbol.Should().Be("AAPL");
	}

	[Fact]
	public async Task GetAllSymbolProfiles_WithEmptyData_ShouldReturnEmptyList()
	{
		// Arrange
		var mockRestCall = new Mock<RestCall>();
		var mockLogger = new Mock<ILogger<GhostfolioMarketData>>();

		var marketDataContent = JsonConvert.SerializeObject(new MarketDataList
		{
			MarketData = [],
			AssetProfile = new SymbolProfile { DataSource = "YAHOO", Symbol = "AAPL", Name = "Apple Inc.", Currency = "USD", AssetClass = "EQUITY", Countries = [], Sectors = [] }
		});

		mockRestCall
			.Setup(x => x.DoRestGet(It.Is<string>(s => s.Contains("api/v1/admin/market-data/"))))
			.ReturnsAsync(marketDataContent);

		var marketData = new GhostfolioMarketData(mockRestCall.Object, mockLogger.Object);

		// Act
		var result = await marketData.GetAllSymbolProfiles();

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAllSymbolProfiles_WithNullContent_ShouldReturnEmptyList()
	{
		// Arrange
		var mockRestCall = new Mock<RestCall>();
		var mockLogger = new Mock<ILogger<GhostfolioMarketData>>();

		mockRestCall
			.Setup(x => x.DoRestGet(It.Is<string>(s => s.Contains("api/v1/admin/market-data/"))))
			.ReturnsAsync((string?)null);

		var marketData = new GhostfolioMarketData(mockRestCall.Object, mockLogger.Object);

		// Act
		var result = await marketData.GetAllSymbolProfiles();

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAllSymbolProfiles_WithNotAuthorizedException_ShouldReturnEmptyList()
	{
		// Arrange
		var mockRestCall = new Mock<RestCall>();
		var mockLogger = new Mock<ILogger<GhostfolioMarketData>>();

		mockRestCall
			.Setup(x => x.DoRestGet(It.Is<string>(s => s.Contains("api/v1/admin/market-data/"))))
			.ThrowsAsync(new NotAuthorizedException("Forbidden"));

		var marketData = new GhostfolioMarketData(mockRestCall.Object, mockLogger.Object);

		// Act
		var result = await marketData.GetAllSymbolProfiles();

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task DeleteSymbol_WithSuccess_ShouldNotThrow()
	{
		// Arrange
		var mockRestCall = new Mock<RestCall>();
		var mockLogger = new Mock<ILogger<GhostfolioMarketData>>();

		mockRestCall
			.Setup(x => x.DoRestDelete(It.IsAny<string>()))
			.ReturnsAsync(new RestResponse { IsSuccessStatusCode = true });

		var symbolProfile = new SymbolProfile
		{
			DataSource = "YAHOO",
			Symbol = "AAPL",
			Name = "Apple Inc.",
			Currency = "USD",
			AssetClass = "EQUITY",
			Countries = [],
			Sectors = []
		};
		var marketData = new GhostfolioMarketData(mockRestCall.Object, mockLogger.Object);

		// Act
		await marketData.DeleteSymbol(symbolProfile);

		// Assert
		mockRestCall.Verify(x => x.DoRestDelete(It.IsAny<string>()), Times.Once);
	}

	[Fact]
	public async Task DeleteSymbol_WithNotAuthorizedException_ShouldNotThrow()
	{
		// Arrange
		var mockRestCall = new Mock<RestCall>();
		var mockLogger = new Mock<ILogger<GhostfolioMarketData>>();

		mockRestCall
			.Setup(x => x.DoRestDelete(It.IsAny<string>()))
			.ThrowsAsync(new NotAuthorizedException("Forbidden"));

		var symbolProfile = new SymbolProfile
		{
			DataSource = "YAHOO",
			Symbol = "AAPL",
			Name = "Apple Inc.",
			Currency = "USD",
			AssetClass = "EQUITY",
			Countries = [],
			Sectors = []
		};
		var marketData = new GhostfolioMarketData(mockRestCall.Object, mockLogger.Object);

		// Act
		await marketData.DeleteSymbol(symbolProfile);

		// Assert
		mockRestCall.Verify(x => x.DoRestDelete(It.IsAny<string>()), Times.Once);
	}
}
