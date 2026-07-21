using System.Net;
using GhostfolioSidekick.ExternalDataProvider.TipRanks;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.ExternalDataProvider.UnitTests.TipRanks;

public class TipRanksMatcherTests
{
	[Fact]
	public void DataSource_ShouldReturnTipRanks()
	{
		// Arrange
		var (matcher, _) = CreateMatcher();

		// Act
		var result = matcher.DataSource;

		// Assert
		Assert.Equal(Datasource.TIPRANKS, result);
	}

	[Fact]
	public void AllowedForDeterminingHolding_ShouldReturnTrue()
	{
		// Arrange
		var (matcher, _) = CreateMatcher();

		// Act
		var result = matcher.AllowedForDeterminingHolding;

		// Assert
		Assert.True(result);
	}

	[Fact]
	public async Task MatchSymbol_WhenNoIdentifiers_ShouldReturnNull()
	{
		// Arrange
		var (matcher, _) = CreateMatcher();
		var identifiers = Array.Empty<PartialSymbolIdentifier>();

		// Act
		var result = await matcher.MatchSymbol(identifiers);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task MatchSymbol_WhenNoEquityIdentifiers_ShouldReturnNull()
	{
		// Arrange
		var (matcher, _) = CreateMatcher();
		var identifiers = new[]
		{
			new PartialSymbolIdentifier(IdentifierType.Ticker, "AAPL", null, [AssetClass.Liquidity], [])
		};

		// Act
		var result = await matcher.MatchSymbol(identifiers);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task MatchSymbol_WhenApiReturnsEmpty_ShouldReturnNull()
	{
		// Arrange
		var (matcher, _) = CreateMatcher("[]");
		var identifiers = StockIdentifiers("AAPL");

		// Act
		var result = await matcher.MatchSymbol(identifiers);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task MatchSymbol_WhenAllResultsDelisted_ShouldReturnNull()
	{
		// Arrange
		var (matcher, _) = CreateMatcher("[{\"label\":\"AAPL (delisted)\",\"value\":\"AAPL\",\"category\":\"ticker\"}]");
		var identifiers = StockIdentifiers("AAPL");

		// Act
		var result = await matcher.MatchSymbol(identifiers);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task MatchSymbol_WhenNoTickerCategoryResults_ShouldReturnNull()
	{
		// Arrange
		var (matcher, _) = CreateMatcher("[{\"label\":\"AAPL\",\"value\":\"AAPL\",\"category\":\"news\"}]");
		var identifiers = StockIdentifiers("AAPL");

		// Act
		var result = await matcher.MatchSymbol(identifiers);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task MatchSymbol_WhenValidMatchFound_ShouldReturnSymbolProfile()
	{
		// Arrange
		var (matcher, _) = CreateMatcher("[{\"label\":\"Apple Inc.\",\"value\":\"AAPL\",\"category\":\"ticker\"}]");
		var identifiers = StockIdentifiers("AAPL");

		// Act
		var result = await matcher.MatchSymbol(identifiers);

		// Assert
		Assert.NotNull(result);
		Assert.Equal("AAPL", result!.Symbol);
		Assert.Equal("Apple Inc.", result.Name);
		Assert.Equal(Datasource.TIPRANKS, result.DataSource);
		Assert.Equal(AssetClass.Equity, result.AssetClass);
		Assert.Equal("https://www.tipranks.com/stocks/AAPL/forecast", result.WebsiteUrl);
		Assert.Single(result.Identifiers);
		Assert.Equal("AAPL", result.Identifiers[0].Identifier);
		Assert.Equal(IdentifierType.Ticker, result.Identifiers[0].IdentifierType);
	}

	[Fact]
	public async Task MatchSymbol_WhenMultipleResults_ShouldReturnBestMatch()
	{
		// Arrange
		var (matcher, _) = CreateMatcher("[{\"label\":\"Apple Inc.\",\"value\":\"AAPL\",\"category\":\"ticker\"},{\"label\":\"Apple Corps\",\"value\":\"APC\",\"category\":\"ticker\"}]");
		var identifiers = StockIdentifiers("AAPL");

		// Act
		var result = await matcher.MatchSymbol(identifiers);

		// Assert
		Assert.NotNull(result);
		Assert.Equal("AAPL", result!.Symbol);
	}

	[Fact]
	public async Task MatchSymbol_WhenScoreBelowThreshold_ShouldReturnNull()
	{
		// Arrange
		var (matcher, _) = CreateMatcher("[{\"label\":\"Completely Different Company\",\"value\":\"XYZ\",\"category\":\"ticker\"}]");
		var identifiers = StockIdentifiers("AAPL");

		// Act
		var result = await matcher.MatchSymbol(identifiers);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task MatchSymbol_WhenAllowedAssetClassesIsNull_ShouldIncludeEquity()
	{
		// Arrange
		var (matcher, _) = CreateMatcher("[{\"label\":\"Test\",\"value\":\"TEST\",\"category\":\"ticker\"}]");
		var identifiers = GenericIdentifiers("TEST");

		// Act
		var result = await matcher.MatchSymbol(identifiers);

		// Assert
		Assert.NotNull(result);
	}

	[Fact]
	public async Task MatchSymbol_WhenAllowedAssetClassesIsEmpty_ShouldIncludeEquity()
	{
		// Arrange
		var (matcher, _) = CreateMatcher("[{\"label\":\"Test\",\"value\":\"TEST\",\"category\":\"ticker\"}]");
		var identifiers = GenericIdentifiers("TEST");

		// Act
		var result = await matcher.MatchSymbol(identifiers);

		// Assert
		Assert.NotNull(result);
	}

	[Fact]
	public async Task MatchSymbol_WhenDelistedFilteredFromAllResults_ShouldReturnNull()
	{
		// Arrange
		var (matcher, _) = CreateMatcher("[{\"label\":\"AAPL (delisted)\",\"value\":\"AAPL\",\"category\":\"ticker\"},{\"label\":\"AAPL (delisted)\",\"value\":\"AAPL\",\"category\":\"ticker\"}]");
		var identifiers = StockIdentifiers("AAPL");

		// Act
		var result = await matcher.MatchSymbol(identifiers);

		// Assert
		Assert.Null(result);
	}

	private static PartialSymbolIdentifier[] StockIdentifiers(string symbol)
	{
		return [PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Ticker, symbol, null)!];
	}

	private static PartialSymbolIdentifier[] GenericIdentifiers(string symbol)
	{
		return [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Ticker, symbol, null)!];
	}

	private static (TipRanksMatcher Matcher, Mock<ILogger<TipRanksMatcher>> LoggerMock) CreateMatcher(string? apiResponse = "[{\"label\":\"AAPL\",\"value\":\"AAPL\",\"category\":\"ticker\"}]")
	{
		var loggerMock = new Mock<ILogger<TipRanksMatcher>>();

		var handler = new TestHttpMessageHandler(apiResponse);
		var httpClient = new HttpClient(handler);
		var factoryMock = new Mock<IHttpClientFactory>();
		factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

		var matcher = new TipRanksMatcher(factoryMock.Object);
		return (matcher, loggerMock);
	}

	private sealed class TestHttpMessageHandler(string? response) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			if (response == null)
			{
				return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway });
			}

			return Task.FromResult(new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(response)
			});
		}
	}
}
