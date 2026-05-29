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
		private const string Coingecko = "coingecko.com";

		private static readonly string[] IgnoreUrlsPartials = [
			"https://fc.yahoo.com/",
			"test/getcrumb"
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
			bool ignorePartial = IgnoreUrlsPartials.Any(partial => url?.Contains(partial, StringComparison.OrdinalIgnoreCase) == true);

			if (string.IsNullOrWhiteSpace(url) || ignorePartial)
			{
				return await base.SendAsync(request, cancellationToken);
			}

			TimeSpan expiry = IsMarketDataUrl(url) ? MarketDataExpiry : DefaultExpiry;
			bool cachNotFound = IsCoinGeckoUrl(url);
			HttpResponseMessage? liveResponse = null;
			
			CachedHttpResponse? cachedResponse = await cacheService.GetOrAddAsync<CachedHttpResponse?>(url, expiry, async () =>
			{
				liveResponse = await base.SendAsync(request, cancellationToken);

				if (liveResponse.StatusCode == HttpStatusCode.NotFound && cachNotFound)
				{
					string content = await liveResponse.Content.ReadAsStringAsync(cancellationToken);
					liveResponse.Dispose();
					return new CachedHttpResponse
					{
						StatusCode = liveResponse.StatusCode,
						Content = content,
						ContentType = liveResponse.Content.Headers.ContentType?.ToString()
					};
				}

				if (!liveResponse.IsSuccessStatusCode)
				{
					return null;
				}

				string successContent = await liveResponse.Content.ReadAsStringAsync(cancellationToken);
				liveResponse.Dispose();
				return new CachedHttpResponse
				{
					StatusCode = liveResponse.StatusCode,
					Content = successContent,
					ContentType = liveResponse.Content.Headers.ContentType?.ToString()
				};
			});

			if (cachedResponse == null)
			{
				return liveResponse ?? throw new InvalidOperationException("The HTTP response was not available.");
			}

			return BuildResponseFromCache(cachedResponse);
		}
		
		private static bool IsMarketDataUrl(string url)
		{
			return MarketDataSegments.Any(segment => url.Contains(segment, StringComparison.OrdinalIgnoreCase));
		}

		private static bool IsCoinGeckoUrl(string url)
		{
			return url.Contains(Coingecko, StringComparison.OrdinalIgnoreCase);
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
