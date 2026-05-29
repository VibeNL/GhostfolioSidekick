using GhostfolioSidekick.ExternalDataProvider.Cache;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Web;

namespace GhostfolioSidekick
{
	/// <summary>
	/// HTTP message handler that caches GET responses from known external data providers.
	/// The cache key is derived from the request URL with sensitive query parameters
	/// (e.g. API keys, tokens) stripped out so they are never persisted in the database.
	/// </summary>
	internal class HttpCachingHandler : DelegatingHandler
	{
		private const string Coingecko = "coingecko.com";

		// Query-string parameter names that may carry credentials and must not be cached.
		private static readonly HashSet<string> SensitiveParams = new(StringComparer.OrdinalIgnoreCase)
		{
			"apikey", "api_key", "api-key",
			"token", "access_token", "auth_token",
			"secret", "client_secret",
			"key", "x-api-key", "crumb"
		};

		// Only cache responses whose media type starts with one of these prefixes.
		// Binary, image, audio, video, and compressed payloads are excluded.
		private static readonly string[] CacheableMediaTypePrefixes =
		[
			"text/",
			"application/json",
			"application/xml",
			"application/atom+xml",
			"application/rss+xml",
			"application/xhtml+xml",
			"application/ld+json",
		];

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

			// Captures the live response when it is not cacheable, so we can return it
			// directly without re-sending the same HttpRequestMessage a second time.
			HttpResponseMessage? uncachedResponse = null;

			string cacheKey = BuildCacheKey(url);
			CachedHttpResponse? cachedResponse = await cacheService.GetOrAddAsync<CachedHttpResponse?>(cacheKey, expiry, async () =>
			{
				HttpResponseMessage live = await base.SendAsync(request, cancellationToken);

				if (live.StatusCode == HttpStatusCode.NotFound && cachNotFound)
					{
						if (!IsCacheableContentType(live.Content.Headers.ContentType?.MediaType))
						{
							uncachedResponse = live;
							return null;
						}

						string content = await live.Content.ReadAsStringAsync(cancellationToken);
						HttpStatusCode statusCode = live.StatusCode;
						string? contentType = live.Content.Headers.ContentType?.ToString();
						live.Dispose();
						return new CachedHttpResponse
						{
							StatusCode = statusCode,
							Content = content,
							ContentType = contentType
						};
					}

					if (!live.IsSuccessStatusCode)
					{
						// Keep the response alive so the caller can return it as-is.
						uncachedResponse = live;
						return null;
					}

					if (!IsCacheableContentType(live.Content.Headers.ContentType?.MediaType))
					{
						// Non-text payloads are passed through without caching.
						uncachedResponse = live;
						return null;
					}

				string successContent = await live.Content.ReadAsStringAsync(cancellationToken);
				HttpStatusCode successStatus = live.StatusCode;
				string? successContentType = live.Content.Headers.ContentType?.ToString();
				live.Dispose();
				return new CachedHttpResponse
				{
					StatusCode = successStatus,
					Content = successContent,
					ContentType = successContentType
				};
			});

			if (cachedResponse == null)
			{
				return uncachedResponse ?? throw new InvalidOperationException("The HTTP response was not available.");
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

		/// <summary>
		/// Returns <see langword="true"/> when <paramref name="mediaType"/> is a text-like
		/// type that can be safely round-tripped through a <see cref="string"/> cache.
		/// A <see langword="null"/> or unrecognised media type is treated as non-cacheable.
		/// </summary>
		internal static bool IsCacheableContentType(string? mediaType)
		{
			if (string.IsNullOrWhiteSpace(mediaType))
			{
				return false;
			}

			return CacheableMediaTypePrefixes.Any(prefix =>
				mediaType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Returns a cache-safe key for <paramref name="url"/> by removing any query-string
		/// parameters whose names appear in <see cref="SensitiveParams"/>.
		/// </summary>
		internal static string BuildCacheKey(string url)
		{
			if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
			{
				return url;
			}

			var query = HttpUtility.ParseQueryString(uri.Query);
			string[] sensitiveKeys = [.. query.AllKeys.Where(k => k != null && SensitiveParams.Contains(k!))!];

			if (sensitiveKeys.Length == 0)
			{
				return url;
			}

			foreach (string key in sensitiveKeys)
			{
				query.Remove(key);
			}

			string sanitizedQuery = query.Count > 0 ? "?" + query.ToString() : string.Empty;
			return new UriBuilder(uri) { Query = sanitizedQuery }.Uri.ToString();
		}

		private static HttpResponseMessage BuildResponseFromCache(CachedHttpResponse cached)
		{
			string fullContentType = cached.ContentType ?? "application/json";

			// Parse via the BCL type to correctly extract media type and charset.
			Encoding encoding = Encoding.UTF8;
			string mediaType = fullContentType.Split(';')[0].Trim();

			if (System.Net.Http.Headers.MediaTypeHeaderValue.TryParse(fullContentType, out var parsed) &&
				!string.IsNullOrWhiteSpace(parsed.CharSet))
			{
				try
				{
					encoding = Encoding.GetEncoding(parsed.CharSet);
				}
				catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
				{
					// Unknown or unavailable charset – fall back to UTF-8.
				}
			}

			return new HttpResponseMessage(cached.StatusCode)
			{
				Content = new StringContent(cached.Content, encoding, mediaType)
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
