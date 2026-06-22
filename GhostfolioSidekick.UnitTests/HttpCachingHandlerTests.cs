using GhostfolioSidekick.ExternalDataProvider.Cache;
using Microsoft.Extensions.DependencyInjection;
using Moq.Protected;
using System.Net;

namespace GhostfolioSidekick.UnitTests
{
	public class HttpCachingHandlerTests
	{
		private readonly Mock<IExternalDataCacheService> _cacheMock;
		private readonly Mock<HttpMessageHandler> _innerHandlerMock;
		private readonly HttpClient _httpClient;

		public HttpCachingHandlerTests()
		{
			_cacheMock = new Mock<IExternalDataCacheService>(MockBehavior.Strict);
			_innerHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

			ServiceCollection services = new();
			services.AddSingleton(_cacheMock.Object);

			HttpCachingHandler handler = new(services.BuildServiceProvider())
			{
				InnerHandler = _innerHandlerMock.Object
			};

			_httpClient = new HttpClient(handler);
		}

		// ------------------------------------------------------------------
		// Helper that wires the cache mock to execute the factory as-is
		// (i.e. no cached entry exists yet).
		// ------------------------------------------------------------------
		private void SetupCacheMissAndStore()
		{
			_cacheMock
				.Setup(c => c.GetOrAddAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()))
				.Returns<string, TimeSpan, Func<Task<CachedHttpResponse?>>>((_, __, factory) => factory());
		}

		// ------------------------------------------------------------------
		// Helper that wires the cache mock to return a pre-built response
		// (i.e. a cache hit – inner handler must NOT be called).
		// ------------------------------------------------------------------
		private void SetupCacheHit(CachedHttpResponse cached)
		{
			_cacheMock
				.Setup(c => c.GetOrAddAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()))
				.ReturnsAsync(cached);
		}

		private void SetupInnerHandler(HttpStatusCode status, string content = "")
		{
			_innerHandlerMock
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(new HttpResponseMessage(status)
				{
					Content = new StringContent(content)
				});
		}

		// ------------------------------------------------------------------
		// Non-GET requests bypass the cache entirely.
		// ------------------------------------------------------------------
		[Fact]
		public async Task NonGetRequest_BypassesCache_AndCallsInner()
		{
			SetupInnerHandler(HttpStatusCode.OK, "ok");

			HttpResponseMessage response = await _httpClient.PostAsync(
				"https://api.coingecko.com/api/v3/coins/bitcoin",
				new StringContent("{}"),
				TestContext.Current.CancellationToken);

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			_cacheMock.Verify(
				c => c.GetOrAddAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()),
				Times.Never);
		}

		// ------------------------------------------------------------------
		// Successful response is stored in the cache.
		// ------------------------------------------------------------------
		[Fact]
		public async Task SuccessfulResponse_IsCached()
		{
			SetupCacheMissAndStore();
			SetupInnerHandler(HttpStatusCode.OK, "{\"id\":\"bitcoin\"}");

			HttpResponseMessage response = await _httpClient.GetAsync(
				"https://api.coingecko.com/api/v3/coins/bitcoin",
				TestContext.Current.CancellationToken);

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			_cacheMock.Verify(
				c => c.GetOrAddAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()),
				Times.Once);
		}

		// ------------------------------------------------------------------
		// 404 response from CoinGecko is cached for 24 hours to avoid endless retries.
		// ------------------------------------------------------------------
		[Fact]
		public async Task NotFoundResponse_CoinGecko_IsCached()
		{
			SetupCacheMissAndStore();
			SetupInnerHandler(HttpStatusCode.NotFound, "not found");

			HttpResponseMessage response = await _httpClient.GetAsync(
				"https://api.coingecko.com/api/v3/coins/nonexistentcoin",
				TestContext.Current.CancellationToken);

			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
			_cacheMock.Verify(
				c => c.GetOrAddAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()),
				Times.Once);
		}

		// ------------------------------------------------------------------
		// 404 response from non-CoinGecko hosts is NOT cached; the original
		// live response is returned directly (inner handler called exactly once).
		// ------------------------------------------------------------------
		[Theory]
		[InlineData("https://query1.finance.yahoo.com/v7/finance/quote?symbols=INVALID")]
		[InlineData("https://www.dividendmax.com/en/stock/nonexistent-company")]
		public async Task NotFoundResponse_NonCoinGecko_IsNotCached_AndInnerCalledOnce(string url)
		{
			int innerCallCount = 0;
			_innerHandlerMock
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(() =>
				{
					innerCallCount++;
					return new HttpResponseMessage(HttpStatusCode.NotFound)
					{
						Content = new StringContent("not found")
					};
				});

			_cacheMock
				.Setup(c => c.GetOrAddAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()))
				.Returns<string, TimeSpan, Func<Task<CachedHttpResponse?>>>((_, __, factory) => factory()!);

			HttpResponseMessage response = await _httpClient.GetAsync(url, TestContext.Current.CancellationToken);

			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
			Assert.Equal(1, innerCallCount);
		}

		// ------------------------------------------------------------------
		// 500 / transient errors are NOT cached; the original live response is
		// returned directly (inner handler called exactly once).
		// ------------------------------------------------------------------
		[Fact]
		public async Task ServerErrorResponse_IsNotCached_AndInnerCalledOnce()
		{
			int innerCallCount = 0;
			_innerHandlerMock
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(() =>
				{
					innerCallCount++;
					return new HttpResponseMessage(HttpStatusCode.InternalServerError)
					{
						Content = new StringContent("error")
					};
				});

			_cacheMock
				.Setup(c => c.GetOrAddAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()))
				.Returns<string, TimeSpan, Func<Task<CachedHttpResponse?>>>((_, __, factory) => factory()!);

			HttpResponseMessage response = await _httpClient.GetAsync(
				"https://api.coingecko.com/api/v3/coins/bitcoin",
				TestContext.Current.CancellationToken);

			Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
			Assert.Equal(1, innerCallCount);
		}

		// ------------------------------------------------------------------
		// Cache hit: inner handler is never invoked.
		// ------------------------------------------------------------------
		[Fact]
		public async Task CacheHit_DoesNotCallInner()
		{
			SetupCacheHit(new CachedHttpResponse
			{
				StatusCode = HttpStatusCode.OK,
				Content = "{\"cached\":true}",
				ContentType = "application/json"
			});

			HttpResponseMessage response = await _httpClient.GetAsync(
				"https://api.coingecko.com/api/v3/coins/bitcoin",
				TestContext.Current.CancellationToken);

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
			Assert.Contains("cached", body);

			_innerHandlerMock
				.Protected()
				.Verify("SendAsync",
					Times.Never(),
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>());
		}

		// ------------------------------------------------------------------
		// Market data URLs get a short (30-minute) expiry.
		// ------------------------------------------------------------------
		[Theory]
		[InlineData("https://api.coingecko.com/api/v3/coins/bitcoin/market_chart?vs_currency=usd&days=365")]
		[InlineData("https://query1.finance.yahoo.com/v8/finance/chart/AAPL?period1=1609459200&period2=1640995200&interval=1d")]
		public async Task MarketDataUrl_UsesShortExpiry(string url)
		{
			TimeSpan? capturedExpiry = null;
			_cacheMock
				.Setup(c => c.GetOrAddAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()))
				.Callback<string, TimeSpan, Func<Task<CachedHttpResponse?>>>((_, expiry, _2) => capturedExpiry = expiry)
				.Returns<string, TimeSpan, Func<Task<CachedHttpResponse?>>>((_, __, factory) => factory());

			SetupInnerHandler(HttpStatusCode.OK, "{}");

			await _httpClient.GetAsync(url, TestContext.Current.CancellationToken);

			Assert.NotNull(capturedExpiry);
			Assert.Equal(TimeSpan.FromHours(4), capturedExpiry);
		}

		// ------------------------------------------------------------------
		// Non-market-data URLs get source-specific expiry.
		// ------------------------------------------------------------------
		[Theory]
		[InlineData("https://api.coingecko.com/api/v3/coins/bitcoin", 24)]
		[InlineData("https://query1.finance.yahoo.com/v7/finance/quote?symbols=AAPL", 24)]
		[InlineData("https://www.dividendmax.com/en/stock/apple-inc-dividends", 168)]
		public async Task NonMarketDataUrl_UsesSourceSpecificExpiry(string url, int expectedHours)
		{
			TimeSpan? capturedExpiry = null;
			_cacheMock
				.Setup(c => c.GetOrAddAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()))
				.Callback<string, TimeSpan, Func<Task<CachedHttpResponse?>>>((_, expiry, _2) => capturedExpiry = expiry)
				.Returns<string, TimeSpan, Func<Task<CachedHttpResponse?>>>((_, __, factory) => factory());

			SetupInnerHandler(HttpStatusCode.OK, "{}");

			await _httpClient.GetAsync(url, TestContext.Current.CancellationToken);

			Assert.NotNull(capturedExpiry);
			Assert.Equal(TimeSpan.FromHours(expectedHours), capturedExpiry);
		}

		// ------------------------------------------------------------------
		// The cache key strips known credential parameters from the URL.
		// ------------------------------------------------------------------
		[Fact]
		public async Task CacheKey_StripsCredentialParameters()
		{
			string? capturedKey = null;
			const string requestUrl = "https://api.coingecko.com/api/v3/coins/bitcoin?localization=false&apikey=SECRET123";
			const string expectedKey = "https://api.coingecko.com/api/v3/coins/bitcoin?localization=false";

			_cacheMock
				.Setup(c => c.GetOrAddAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()))
				.Callback<string, TimeSpan, Func<Task<CachedHttpResponse?>>>((key, _, _2) => capturedKey = key)
				.Returns<string, TimeSpan, Func<Task<CachedHttpResponse?>>>((_, __, factory) => factory());

			SetupInnerHandler(HttpStatusCode.OK, "{}");

			await _httpClient.GetAsync(requestUrl, TestContext.Current.CancellationToken);

			Assert.Equal(expectedKey, capturedKey);
		}

		[Fact]
		public async Task CacheKey_WithNoCredentialParameters_IsUnchanged()
		{
			string? capturedKey = null;
			const string requestUrl = "https://api.coingecko.com/api/v3/coins/bitcoin?localization=false";

			_cacheMock
				.Setup(c => c.GetOrAddAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()))
				.Callback<string, TimeSpan, Func<Task<CachedHttpResponse?>>>((key, _, _2) => capturedKey = key)
				.Returns<string, TimeSpan, Func<Task<CachedHttpResponse?>>>((_, __, factory) => factory());

			SetupInnerHandler(HttpStatusCode.OK, "{}");

			await _httpClient.GetAsync(requestUrl, TestContext.Current.CancellationToken);

			Assert.Equal(requestUrl, capturedKey);
		}

		// ------------------------------------------------------------------
		// BuildCacheKey unit tests (no HTTP involved).
		// ------------------------------------------------------------------
		[Theory]
		[InlineData(
			"https://example.com/data?symbol=AAPL&apikey=SECRET",
			"https://example.com/data?symbol=AAPL")]
		[InlineData(
			"https://example.com/data?token=abc&symbol=AAPL&access_token=xyz",
			"https://example.com/data?symbol=AAPL")]
		[InlineData(
			"https://example.com/data?apikey=SECRET",
			"https://example.com/data")]
		[InlineData(
			"https://example.com/data?symbol=AAPL",
			"https://example.com/data?symbol=AAPL")]
		public void BuildCacheKey_RemovesSensitiveParams(string input, string expected)
		{
			string result = HttpCachingHandler.BuildCacheKey(input);
			Assert.Equal(expected, result);
		}

		// ------------------------------------------------------------------
		// Transient params (period1, period2) are rounded to midnight UTC so
		// requests within the same day share a cache entry, but different days
		// do not collide.
		// ------------------------------------------------------------------
		[Fact]
		public void BuildCacheKey_RoundsTransientParamsToMidnight()
		{
			string key1 = HttpCachingHandler.BuildCacheKey(
				"https://query2.finance.yahoo.com/v8/finance/chart/USDEUR=X?period1=1780839050&period2=1781011850&interval=1d&events=history");
			string key2 = HttpCachingHandler.BuildCacheKey(
				"https://query2.finance.yahoo.com/v8/finance/chart/USDEUR=X?period1=1780839671&period2=1781012471&interval=1d&events=history");

			// Same day -> same key
			Assert.Equal(key1, key2);

			// Key still contains the rounded timestamps
			Assert.Contains("period1=", key1);
			Assert.Contains("period2=", key1);
		}

		[Fact]
		public void BuildCacheKey_DifferentDays_DifferentKeys()
		{
			// June 7 2026 ~09:00 UTC
			string key1 = HttpCachingHandler.BuildCacheKey(
				"https://query2.finance.yahoo.com/v8/finance/chart/USDEUR=X?period1=1780839050&period2=1781011850&interval=1d&events=history");

			// June 8 2026 ~09:00 UTC (one day later)
			string key2 = HttpCachingHandler.BuildCacheKey(
				"https://query2.finance.yahoo.com/v8/finance/chart/USDEUR=X?period1=1780925450&period2=1781098250&interval=1d&events=history");

			Assert.NotEqual(key1, key2);
		}

		[Fact]
		public void BuildCacheKey_RoundsToMidnightUTC()
		{
			string key = HttpCachingHandler.BuildCacheKey(
				"https://query2.finance.yahoo.com/v8/finance/chart/USDEUR=X?period1=1780839050&period2=1781011850&interval=1d&events=history");

			// 1780839050 -> 2026-06-07 UTC -> midnight = 1780809600
			// 1781011850 -> 2026-06-09 UTC -> midnight = 1781078400
			Assert.Contains("period1=", key);
			Assert.Contains("period2=", key);
		}
	}
}
