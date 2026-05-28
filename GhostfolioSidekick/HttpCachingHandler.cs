using GhostfolioSidekick.ExternalDataProvider.Cache;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;

namespace GhostfolioSidekick
{
	/// <summary>
	/// HTTP message handler that caches GET responses from known external data providers.
	/// Uses the raw request URL as the cache key.
	/// </summary>
	internal class HttpCachingHandler : DelegatingHandler
	{
		private static readonly string[] KnownHosts =
		[
			"coingecko.com",
			"yahoo.com",
			"dividendmax.com",
		];

		// URLs containing these segments are considered market data (short-lived cache).
		private static readonly string[] MarketDataSegments =
		[
			"/market_chart",
			"/v8/finance/chart/",
		];

		private static readonly TimeSpan MarketDataExpiry = TimeSpan.FromMinutes(30);
		private static readonly TimeSpan DefaultExpiry = TimeSpan.FromDays(1);

		private readonly IExternalDataCacheService cacheService;

		public HttpCachingHandler(IServiceProvider serviceProvider)
		{
			this.cacheService = serviceProvider.GetRequiredService<IExternalDataCacheService>();
		}

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			// Only cache GET requests from known hosts
			if (request.Method != HttpMethod.Get)
			{
				return await base.SendAsync(request, cancellationToken);
			}

			string? url = request.RequestUri?.ToString();
			if (string.IsNullOrWhiteSpace(url) || !IsKnownHost(url))
			{
				return await base.SendAsync(request, cancellationToken);
			}

			TimeSpan expiry = IsMarketDataUrl(url) ? MarketDataExpiry : DefaultExpiry;
			bool cachNotFound = IsCoinGeckoUrl(url);

			CachedHttpResponse? cachedResponse = await cacheService.GetOrAddAsync<CachedHttpResponse?>(url, expiry, async () =>
			{
				HttpResponseMessage result = await base.SendAsync(request, cancellationToken);

				if (result.StatusCode == HttpStatusCode.NotFound && cachNotFound)
				{
					string content = await result.Content.ReadAsStringAsync(cancellationToken);
					return new CachedHttpResponse
					{
						StatusCode = result.StatusCode,
						Content = content,
						ContentType = result.Content.Headers.ContentType?.ToString()
					};
				}

				if (!result.IsSuccessStatusCode)
				{
					return null;
				}

				string successContent = await result.Content.ReadAsStringAsync(cancellationToken);
				return new CachedHttpResponse
				{
					StatusCode = result.StatusCode,
					Content = successContent,
					ContentType = result.Content.Headers.ContentType?.ToString()
				};
			});

			if (cachedResponse == null)
			{
				return await base.SendAsync(request, cancellationToken);
			}

			return BuildResponseFromCache(cachedResponse);
		}

		private static bool IsKnownHost(string url)
		{
			return KnownHosts.Any(host => url.Contains(host, StringComparison.OrdinalIgnoreCase));
		}

		private static bool IsMarketDataUrl(string url)
		{
			return MarketDataSegments.Any(segment => url.Contains(segment, StringComparison.OrdinalIgnoreCase));
		}

		private static bool IsCoinGeckoUrl(string url)
		{
			return url.Contains("coingecko.com", StringComparison.OrdinalIgnoreCase);
		}

		private static HttpResponseMessage BuildResponseFromCache(CachedHttpResponse cached)
		{
			string mediaType = (cached.ContentType ?? "application/json")
				.Split(';')[0]
				.Trim();

			return new HttpResponseMessage(cached.StatusCode)
			{
				Content = new StringContent(cached.Content, Encoding.UTF8, mediaType)
			};
		}
	}

	/// <summary>
	/// Represents a cached HTTP response with serializable content.
	/// </summary>
	internal class CachedHttpResponse
	{
		public HttpStatusCode StatusCode { get; set; }
		public string Content { get; set; } = string.Empty;
		public string? ContentType { get; set; }
	}
}
