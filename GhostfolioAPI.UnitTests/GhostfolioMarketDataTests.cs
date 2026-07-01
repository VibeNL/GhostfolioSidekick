using AwesomeAssertions;
using GhostfolioSidekick.Configuration;
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
	private static Mock<IApplicationSettings> CreateSettings(bool allowAdminCalls)
	{
		var mock = new Mock<IApplicationSettings>();
		mock.Setup(x => x.AllowAdminCalls).Returns(allowAdminCalls);
		return mock;
	}

	[Fact]
	public async Task GetAllSymbolProfiles_WithValidData_ShouldReturnProfiles()
	{
		// Arrange
		var profilesContent = JsonConvert.SerializeObject(new AssetProfileList
		{
			AssetProfiles = new[]
			{
				new SymbolProfile
				{
					DataSource = "YAHOO", Symbol = "AAPL", Name = "Apple", Currency = "USD",
					AssetClass = "EQUITY", Countries = [], Sectors = []
				}
			}
		});

		var mock = new Mock<IRestCall>();
		mock.Setup(x => x.DoRestGet("api/v1/asset-profiles"))
			.ReturnsAsync(profilesContent);

		var settings = CreateSettings(allowAdminCalls: true);
		var marketData = new GhostfolioMarketData(mock.Object, Mock.Of<ILogger<GhostfolioMarketData>>(), settings.Object);

		// Act
		var result = await marketData.GetAllSymbolProfiles();

		// Assert
		result.Should().ContainSingle().Which.Symbol.Should().Be("AAPL");
	}

	[Fact]
	public async Task GetAllSymbolProfiles_WithEmptyData_ShouldReturnEmptyList()
	{
		// Arrange
		var mock = new Mock<IRestCall>();
		mock.Setup(x => x.DoRestGet("api/v1/asset-profiles"))
			.ReturnsAsync(JsonConvert.SerializeObject(new AssetProfileList { AssetProfiles = Array.Empty<SymbolProfile>() }));

		var settings = CreateSettings(allowAdminCalls: true);
		var marketData = new GhostfolioMarketData(mock.Object, Mock.Of<ILogger<GhostfolioMarketData>>(), settings.Object);

		// Act
		var result = await marketData.GetAllSymbolProfiles();

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAllSymbolProfiles_WithNullContent_ShouldReturnEmptyList()
	{
		// Arrange
		var mock = new Mock<IRestCall>();
		mock.Setup(x => x.DoRestGet("api/v1/asset-profiles"))
			.ReturnsAsync((string?)null);

		var settings = CreateSettings(allowAdminCalls: true);
		var marketData = new GhostfolioMarketData(mock.Object, Mock.Of<ILogger<GhostfolioMarketData>>(), settings.Object);

		// Act
		var result = await marketData.GetAllSymbolProfiles();

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAllSymbolProfiles_WithNotAuthorizedException_ShouldReturnEmptyList()
	{
		// Arrange
		var mock = new Mock<IRestCall>();
		mock.Setup(x => x.DoRestGet("api/v1/asset-profiles"))
			.ThrowsAsync(new NotAuthorizedException("Forbidden"));

		var settings = CreateSettings(allowAdminCalls: true);
		var marketData = new GhostfolioMarketData(mock.Object, Mock.Of<ILogger<GhostfolioMarketData>>(), settings.Object);

		// Act
		var result = await marketData.GetAllSymbolProfiles();

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAllSymbolProfiles_WhenAllowAdminCallsFalse_ShouldReturnEmptyList()
	{
		// Arrange
		var mock = new Mock<IRestCall>();
		var settings = CreateSettings(allowAdminCalls: false);
		var marketData = new GhostfolioMarketData(mock.Object, Mock.Of<ILogger<GhostfolioMarketData>>(), settings.Object);

		// Act
		var result = await marketData.GetAllSymbolProfiles();

		// Assert
		result.Should().BeEmpty();
		mock.Verify(x => x.DoRestGet(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task DeleteSymbol_WithSuccess_ShouldNotThrow()
	{
		// Arrange
		var mock = new Mock<IRestCall>();
		mock.Setup(x => x.DoRestDelete(It.IsAny<string>()))
			.ReturnsAsync(new RestResponse { IsSuccessStatusCode = true });

		var settings = CreateSettings(allowAdminCalls: true);
		var marketData = new GhostfolioMarketData(mock.Object, Mock.Of<ILogger<GhostfolioMarketData>>(), settings.Object);
		var symbolProfile = new SymbolProfile
		{
			DataSource = "YAHOO", Symbol = "AAPL", Name = "Apple Inc.", Currency = "USD",
			AssetClass = "EQUITY", Countries = [], Sectors = []
		};

		// Act
		await marketData.DeleteSymbol(symbolProfile);

		// Assert
		mock.Verify(x => x.DoRestDelete(It.IsAny<string>()), Times.Once);
	}

	[Fact]
	public async Task DeleteSymbol_WithNotAuthorizedException_ShouldNotThrow()
	{
		// Arrange
		var mock = new Mock<IRestCall>();
		mock.Setup(x => x.DoRestDelete(It.IsAny<string>()))
			.ThrowsAsync(new NotAuthorizedException("Forbidden"));

		var settings = CreateSettings(allowAdminCalls: true);
		var marketData = new GhostfolioMarketData(mock.Object, Mock.Of<ILogger<GhostfolioMarketData>>(), settings.Object);
		var symbolProfile = new SymbolProfile
		{
			DataSource = "YAHOO", Symbol = "AAPL", Name = "Apple Inc.", Currency = "USD",
			AssetClass = "EQUITY", Countries = [], Sectors = []
		};

		// Act
		await marketData.DeleteSymbol(symbolProfile);

		// Assert
		mock.Verify(x => x.DoRestDelete(It.IsAny<string>()), Times.Once);
	}

	[Fact]
	public async Task DeleteSymbol_WhenAllowAdminCallsFalse_ShouldNotCallApi()
	{
		// Arrange
		var mock = new Mock<IRestCall>();
		var settings = CreateSettings(allowAdminCalls: false);
		var marketData = new GhostfolioMarketData(mock.Object, Mock.Of<ILogger<GhostfolioMarketData>>(), settings.Object);
		var symbolProfile = new SymbolProfile
		{
			DataSource = "YAHOO", Symbol = "AAPL", Name = "Apple Inc.", Currency = "USD",
			AssetClass = "EQUITY", Countries = [], Sectors = []
		};

		// Act
		await marketData.DeleteSymbol(symbolProfile);

		// Assert
		mock.Verify(x => x.DoRestDelete(It.IsAny<string>()), Times.Never);
	}
}
