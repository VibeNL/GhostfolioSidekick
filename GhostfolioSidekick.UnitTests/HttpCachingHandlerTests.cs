using GhostfolioSidekick.ExternalDataProvider.Cache;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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
				.Setup(c => c.GetOrAddAsync(It.IsAny<CacheKey>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()))
				.Returns<CacheKey, Func<Task<CachedHttpResponse?>>>((_, factory) => factory());
		}

		// ------------------------------------------------------------------
		// Helper that wires the cache mock to return a pre-built response
		// (i.e. a cache hit – inner handler must NOT be called).
		// ------------------------------------------------------------------
		private void SetupCacheHit(CachedHttpResponse cached)
		{
			_cacheMock
				.Setup(c => c.GetOrAddAsync(It.IsAny<CacheKey>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()))
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
				c => c.GetOrAddAsync(It.IsAny<CacheKey>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()),
				Times.Never);
		}

		// ------------------------------------------------------------------
		// Unknown host bypasses the cache.
		// ------------------------------------------------------------------
		[Fact]
		public async Task UnknownHost_BypassesCache_AndCallsInner()
		{
			SetupInnerHandler(HttpStatusCode.OK, "ok");

			HttpResponseMessage response = await _httpClient.GetAsync("https://example.com/data", TestContext.Current.CancellationToken);

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			_cacheMock.Verify(
				c => c.GetOrAddAsync(It.IsAny<CacheKey>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()),
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
				c => c.GetOrAddAsync(It.IsAny<CacheKey>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()),
				Times.Once);
		}

		// ------------------------------------------------------------------
		// 404 response is cached to avoid endless retries.
		// ------------------------------------------------------------------
		[Fact]
		public async Task NotFoundResponse_IsCached()
		{
			SetupCacheMissAndStore();
			SetupInnerHandler(HttpStatusCode.NotFound, "not found");

			HttpResponseMessage response = await _httpClient.GetAsync(
				"https://api.coingecko.com/api/v3/coins/nonexistentcoin",
				TestContext.Current.CancellationToken);

			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
			_cacheMock.Verify(
				c => c.GetOrAddAsync(It.IsAny<CacheKey>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()),
				Times.Once);
		}

		// ------------------------------------------------------------------
		// 500 / transient errors are NOT cached – inner handler re-called.
		// ------------------------------------------------------------------
		[Fact]
		public async Task ServerErrorResponse_IsNotCached_AndInnerCalledTwice()
		{
			// First call: cache miss, factory runs, inner returns 500 → not cached → null returned
			// Handler then falls back to calling inner directly for the second send.
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

			// Cache always runs the factory (miss), and factory returns null (500 → not stored).
			_cacheMock
				.Setup(c => c.GetOrAddAsync(It.IsAny<CacheKey>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()))
				.Returns<CacheKey, Func<Task<CachedHttpResponse?>>>((_, factory) => factory()!);

			HttpResponseMessage response = await _httpClient.GetAsync(
				"https://api.coingecko.com/api/v3/coins/bitcoin",
				TestContext.Current.CancellationToken);

			// The handler falls back to a direct inner call when cache returns null.
			Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
			Assert.Equal(2, innerCallCount);
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
		// CoinGecko coin URL resolves to SymbolProfile cache key.
		// ------------------------------------------------------------------
		[Theory]
		[InlineData("https://api.coingecko.com/api/v3/coins/bitcoin")]
		[InlineData("https://api.coingecko.com/api/v3/coins/ethereum?localization=false")]
		public async Task CoinGeckoCoinUrl_UsesSymbolProfileKey(string url)
		{
			CacheKey? capturedKey = null;
			_cacheMock
				.Setup(c => c.GetOrAddAsync(It.IsAny<CacheKey>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()))
				.Callback<CacheKey, Func<Task<CachedHttpResponse?>>>((key, _) => capturedKey = key)
				.Returns<CacheKey, Func<Task<CachedHttpResponse?>>>((_, factory) => factory());

			SetupInnerHandler(HttpStatusCode.OK, "{}");

			await _httpClient.GetAsync(url, TestContext.Current.CancellationToken);

			Assert.NotNull(capturedKey);
			Assert.Equal(Source.CoinGecko, capturedKey!.Source);
			Assert.Equal(TypeOfData.SymbolProfile, capturedKey.DataType);
		}

		// ------------------------------------------------------------------
		// Yahoo chart URL with period params resolves to MarketData cache key.
		// ------------------------------------------------------------------
		[Fact]
		public async Task YahooChartUrl_WithPeriods_UsesMarketDataKey()
		{
			CacheKey? capturedKey = null;
			_cacheMock
				.Setup(c => c.GetOrAddAsync(It.IsAny<CacheKey>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()))
				.Callback<CacheKey, Func<Task<CachedHttpResponse?>>>((key, _) => capturedKey = key)
				.Returns<CacheKey, Func<Task<CachedHttpResponse?>>>((_, factory) => factory());

			SetupInnerHandler(HttpStatusCode.OK, "{}");

			await _httpClient.GetAsync(
				"https://query1.finance.yahoo.com/v8/finance/chart/AAPL?period1=1609459200&period2=1640995200&interval=1d",
				TestContext.Current.CancellationToken);

			Assert.NotNull(capturedKey);
			Assert.Equal(Source.Yahoo, capturedKey!.Source);
			Assert.Equal(TypeOfData.MarketData, capturedKey.DataType);
		}

		// ------------------------------------------------------------------
		// Yahoo chart URL without period params resolves to SymbolProfile key.
		// ------------------------------------------------------------------
		[Fact]
		public async Task YahooChartUrl_WithoutPeriods_UsesSymbolProfileKey()
		{
			CacheKey? capturedKey = null;
			_cacheMock
				.Setup(c => c.GetOrAddAsync(It.IsAny<CacheKey>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()))
				.Callback<CacheKey, Func<Task<CachedHttpResponse?>>>((key, _) => capturedKey = key)
				.Returns<CacheKey, Func<Task<CachedHttpResponse?>>>((_, factory) => factory());

			SetupInnerHandler(HttpStatusCode.OK, "{}");

			await _httpClient.GetAsync(
				"https://query1.finance.yahoo.com/v8/finance/chart/AAPL?interval=1d",
				TestContext.Current.CancellationToken);

			Assert.NotNull(capturedKey);
			Assert.Equal(Source.Yahoo, capturedKey!.Source);
			Assert.Equal(TypeOfData.SymbolProfile, capturedKey.DataType);
		}

		// ------------------------------------------------------------------
		// Yahoo quote URL resolves to SymbolProfile cache key.
		// ------------------------------------------------------------------
		[Fact]
		public async Task YahooQuoteUrl_UsesSymbolProfileKey()
		{
			CacheKey? capturedKey = null;
			_cacheMock
				.Setup(c => c.GetOrAddAsync(It.IsAny<CacheKey>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()))
				.Callback<CacheKey, Func<Task<CachedHttpResponse?>>>((key, _) => capturedKey = key)
				.Returns<CacheKey, Func<Task<CachedHttpResponse?>>>((_, factory) => factory());

			SetupInnerHandler(HttpStatusCode.OK, "{}");

			await _httpClient.GetAsync(
				"https://query1.finance.yahoo.com/v7/finance/quote?symbols=AAPL",
				TestContext.Current.CancellationToken);

			Assert.NotNull(capturedKey);
			Assert.Equal(Source.Yahoo, capturedKey!.Source);
			Assert.Equal(TypeOfData.SymbolProfile, capturedKey.DataType);
		}

		// ------------------------------------------------------------------
		// DividendMax URL resolves to Dividends cache key.
		// ------------------------------------------------------------------
		[Fact]
		public async Task DividendMaxUrl_UsesDividendsKey()
		{
			CacheKey? capturedKey = null;
			_cacheMock
				.Setup(c => c.GetOrAddAsync(It.IsAny<CacheKey>(), It.IsAny<Func<Task<CachedHttpResponse?>>>()))
				.Callback<CacheKey, Func<Task<CachedHttpResponse?>>>((key, _) => capturedKey = key)
				.Returns<CacheKey, Func<Task<CachedHttpResponse?>>>((_, factory) => factory());

			SetupInnerHandler(HttpStatusCode.OK, "<html/>");

			await _httpClient.GetAsync(
				"https://www.dividendmax.com/en/stock/apple-inc-dividends",
				TestContext.Current.CancellationToken);

			Assert.NotNull(capturedKey);
			Assert.Equal(Source.DividendMax, capturedKey!.Source);
			Assert.Equal(TypeOfData.Dividends, capturedKey.DataType);
		}
	}
}
