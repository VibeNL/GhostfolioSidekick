using System.Net;
using GhostfolioSidekick.ExternalDataProvider.TipRanks;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace GhostfolioSidekick.ExternalDataProvider.UnitTests.TipRanks;

public class TipRanksScraperTests
{
	[Fact]
	public async Task GetPriceTarget_WhenSymbolIsNull_ShouldReturnNull()
	{
		// Arrange
		var (scraper, _) = CreateScraper();

		// Act
		var result = await scraper.GetPriceTarget(null!);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task GetPriceTarget_WhenWebsiteUrlIsNull_ShouldReturnNull()
	{
		// Arrange
		var (scraper, _) = CreateScraper();
		var symbol = BuildSymbolProfile("AAPL", Datasource.YAHOO);

		// Act
		var result = await scraper.GetPriceTarget(symbol);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task GetPriceTarget_WhenDataSourceNotTipRanks_ShouldReturnNull()
	{
		// Arrange
		var (scraper, _) = CreateScraper();
		var symbol = BuildSymbolProfileWithUrl("AAPL", Datasource.YAHOO, "https://www.tipranks.com/stocks/nl:asrnl/forecast");

		// Act
		var result = await scraper.GetPriceTarget(symbol);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task GetPriceTarget_WhenUrlHasTooFewSegments_ShouldReturnNull()
	{
		// Arrange
		var (scraper, loggerMock) = CreateScraper();
		var symbol = BuildSymbolProfileWithUrl("AAPL", Datasource.TIPRANKS, "https://www.tipranks.com/stocks");

		// Act
		var result = await scraper.GetPriceTarget(symbol);

		// Assert
		Assert.Null(result);
		loggerMock.Verify(
			x => x.Log(
				LogLevel.Warning,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((@v, @t) => @v.ToString()!.Contains("Invalid TipRanks URL format")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task GetPriceTarget_WhenApiReturnsError_ShouldReturnNull()
	{
		// Arrange
		var (scraper, loggerMock) = CreateScraper(httpStatusCode: HttpStatusCode.NotFound);
		var symbol = BuildSymbolProfileWithUrl("AAPL", Datasource.TIPRANKS, "https://www.tipranks.com/stocks/nl:asrnl/forecast");

		// Act
		var result = await scraper.GetPriceTarget(symbol);

		// Assert
		Assert.Null(result);
		loggerMock.Verify(
			x => x.Log(
				LogLevel.Warning,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((@v, @t) => @v.ToString()!.Contains("Failed to fetch data from TipRanks API")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task GetPriceTarget_WhenApiReturnsEmptyContent_ShouldReturnNull()
	{
		// Arrange
		var (scraper, loggerMock) = CreateScraper(emptyContent: true);
		var symbol = BuildSymbolProfileWithUrl("AAPL", Datasource.TIPRANKS, "https://www.tipranks.com/stocks/nl:asrnl/forecast");

		// Act
		var result = await scraper.GetPriceTarget(symbol);

		// Assert
		Assert.Null(result);
		loggerMock.Verify(
			x => x.Log(
				LogLevel.Warning,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((@v, @t) => @v.ToString()!.Contains("Empty response from TipRanks API")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task GetPriceTarget_WhenApiReturnsNoStocks_ShouldReturnNull()
	{
		// Arrange
		var (scraper, loggerMock) = CreateScraper(noStocks: true);
		var symbol = BuildSymbolProfileWithUrl("AAPL", Datasource.TIPRANKS, "https://www.tipranks.com/stocks/nl:asrnl/forecast");

		// Act
		var result = await scraper.GetPriceTarget(symbol);

		// Assert
		Assert.Null(result);
		loggerMock.Verify(
			x => x.Log(
				LogLevel.Warning,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((@v, @t) => @v.ToString()!.Contains("No stock data found")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task GetPriceTarget_WhenNoAnalystRatings_ShouldReturnNull()
	{
		// Arrange
		var (scraper, loggerMock) = CreateScraper(noAnalystRatings: true);
		var symbol = BuildSymbolProfileWithUrl("AAPL", Datasource.TIPRANKS, "https://www.tipranks.com/stocks/nl:asrnl/forecast");

		// Act
		var result = await scraper.GetPriceTarget(symbol);

		// Assert
		Assert.Null(result);
		loggerMock.Verify(
			x => x.Log(
				LogLevel.Warning,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((@v, @t) => @v.ToString()!.Contains("No analyst ratings found")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task GetPriceTarget_WhenValidData_ShouldReturnPriceTarget()
	{
		// Arrange
		var validJson = "{\"models\":{\"stocks\":[{\"_id\":\"nl:asrnl\",\"analystRatings\":{\"all\":{\"id\":\"buy\",\"buy\":10,\"hold\":5,\"sell\":2,\"priceTarget\":{\"value\":120},\"highPriceTarget\":150,\"lowPriceTarget\":90}}}]}}";
		var (scraper, _) = CreateScraper(validJson: validJson);
		var symbol = BuildSymbolProfileWithUrl("AAPL", Datasource.TIPRANKS, "https://www.tipranks.com/stocks/nl:asrnl/forecast");

		// Act
		var result = await scraper.GetPriceTarget(symbol);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(150m, result!.HighestTargetPriceAmount);
		Assert.Equal(120m, result.AverageTargetPriceAmount);
		Assert.Equal(90m, result.LowestTargetPriceAmount);
		Assert.Equal(AnalystRating.Buy, result.Rating);
		Assert.Equal(10, result.NumberOfBuys);
		Assert.Equal(5, result.NumberOfHolds);
		Assert.Equal(2, result.NumberOfSells);
	}

	[Fact]
	public async Task GetPriceTarget_WhenPriceTargetValueIsNull_ShouldDefaultToZero()
	{
		// Arrange
		var validJson = "{\"models\":{\"stocks\":[{\"_id\":\"nl:asrnl\",\"analystRatings\":{\"all\":{\"id\":\"buy\",\"buy\":10,\"hold\":5,\"sell\":2,\"highPriceTarget\":150,\"lowPriceTarget\":90}}}]}}";
		var (scraper, _) = CreateScraper(validJson: validJson);
		var symbol = BuildSymbolProfileWithUrl("AAPL", Datasource.TIPRANKS, "https://www.tipranks.com/stocks/nl:asrnl/forecast");

		// Act
		var result = await scraper.GetPriceTarget(symbol);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(0m, result!.AverageTargetPriceAmount);
	}

	private static (TipRanksScraper Scraper, Mock<ILogger<TipRanksScraper>> LoggerMock) CreateScraper(
		HttpStatusCode httpStatusCode = HttpStatusCode.OK,
		string? validJson = null,
		bool emptyContent = false,
		bool noStocks = false,
		bool noAnalystRatings = false)
	{
		var loggerMock = new Mock<ILogger<TipRanksScraper>>();

		var handlerMock = new Mock<HttpMessageHandler>();
		string content;
		if (validJson != null)
		{
			content = validJson;
		}
		else if (noStocks)
		{
			content = "{\"models\":{\"stocks\":[]}}";
		}
		else if (noAnalystRatings)
		{
			content = "{\"models\":{\"stocks\":[{\"_id\":\"nl:asrnl\",\"analystRatings\":{}}]}}";
		}
		else if (emptyContent)
		{
			content = "";
		}
		else
		{
			content = "{}";
		}

		handlerMock.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage { StatusCode = httpStatusCode, Content = new StringContent(content) });

		var httpClient = new HttpClient(handlerMock.Object);
		var scraper = new TipRanksScraper(loggerMock.Object, httpClient);

		return (scraper, loggerMock);
	}

	private static SymbolProfile BuildSymbolProfile(string symbol, string dataSource)
	{
		return new SymbolProfile
		{
			Symbol = symbol,
			DataSource = dataSource
		};
	}

	private static SymbolProfile BuildSymbolProfileWithUrl(string symbol, string dataSource, string websiteUrl)
	{
		return new SymbolProfile
		{
			Symbol = symbol,
			DataSource = dataSource,
			WebsiteUrl = websiteUrl
		};
	}
}

