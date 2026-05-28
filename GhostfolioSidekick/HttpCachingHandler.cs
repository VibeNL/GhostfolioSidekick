using GhostfolioSidekick.ExternalDataProvider.Cache;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace GhostfolioSidekick
{
	/// <summary>
	/// HTTP message handler that caches responses from external data providers.
	/// Uses the ExternalDataCacheService to store HTTP responses in the database.
	/// </summary>
	internal class HttpCachingHandler : DelegatingHandler
	{
		private readonly IExternalDataCacheService cacheService;
		private readonly IServiceProvider serviceProvider;

		public HttpCachingHandler(IServiceProvider serviceProvider)
		{
			this.serviceProvider = serviceProvider;
			this.cacheService = serviceProvider.GetRequiredService<IExternalDataCacheService>();
		}

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			// Only cache GET requests
			if (request.Method != HttpMethod.Get)
			{
				return await base.SendAsync(request, cancellationToken);
			}

			// Determine the source and data type from the request URL
			if (!TryDetermineCacheKey(request, out CacheKey? cacheKey) || cacheKey == null)
			{
				// If we can't determine cache key, bypass cache
				return await base.SendAsync(request, cancellationToken);
			}

			// Try to get from cache or execute the request
			CachedHttpResponse? cachedResponse = await cacheService.GetOrAddAsync(cacheKey, async () =>
			{
				HttpResponseMessage result = await base.SendAsync(request, cancellationToken);

				// Cache successful responses and 404s (to avoid endless retries for missing resources)
				if (!result.IsSuccessStatusCode && result.StatusCode != System.Net.HttpStatusCode.NotFound)
				{
					return null;
				}

				// Read and cache the response content
				string content = await result.Content.ReadAsStringAsync(cancellationToken);
				return new CachedHttpResponse
				{
					StatusCode = result.StatusCode,
					Content = content,
					ContentType = result.Content.Headers.ContentType?.ToString()
				};
			});

			// If cache returned null (e.g., transient failure), execute without caching
			if (cachedResponse == null)
			{
				return await base.SendAsync(request, cancellationToken);
			}

			// Reconstruct the response from cached data
			return BuildResponseFromCache(cachedResponse);
		}

		// Strip any parameters (e.g. "text/plain; charset=utf-8" → "text/plain") because
		// StringContent's mediaType parameter only accepts the bare media-type token.
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

		private static bool TryDetermineCacheKey(HttpRequestMessage request, out CacheKey? cacheKey)
		{
			cacheKey = null;
			string? url = request.RequestUri?.ToString();

			if (string.IsNullOrWhiteSpace(url))
			{
				return false;
			}

			// Determine source based on URL
			Source source;
			TypeOfData dataType;
			string key;

			if (url.Contains("coingecko.com", StringComparison.OrdinalIgnoreCase))
			{
				source = Source.CoinGecko;

				if (url.Contains("/coins/") && url.Contains("/market_chart"))
				{
					dataType = TypeOfData.MarketData;
					// Extract coin ID from URL
					key = ExtractCoinIdFromUrl(url);
				}
				else if (url.Contains("/coins/"))
				{
					dataType = TypeOfData.SymbolProfile;
					key = ExtractCoinIdFromUrl(url);
				}
				else
				{
					// Use hash of URL as key for other requests
					key = ComputeUrlHash(url);
					dataType = TypeOfData.SymbolProfile;
				}
			}
			else if (url.Contains("yahoo", StringComparison.OrdinalIgnoreCase) ||
					 url.Contains("query1.finance.yahoo.com", StringComparison.OrdinalIgnoreCase) ||
					 url.Contains("query2.finance.yahoo.com", StringComparison.OrdinalIgnoreCase))
			{
				source = Source.Yahoo;

				if (url.Contains("/v8/finance/chart/"))
				{
					dataType = TypeOfData.MarketData;
					key = ExtractYahooSymbolFromChartUrl(url);
				}
				else if (url.Contains("/v7/finance/quote"))
				{
					dataType = TypeOfData.SymbolProfile;
					key = ExtractYahooSymbolsFromQuoteUrl(url);
				}
				else
				{
					// Use hash of URL as key for other requests
					key = ComputeUrlHash(url);
					dataType = TypeOfData.SymbolProfile;
				}
			}
			else if (url.Contains("dividendmax.com", StringComparison.OrdinalIgnoreCase))
			{
				source = Source.DividendMax;
				dataType = TypeOfData.Dividends;
				key = ComputeUrlHash(url);
			}
			else
			{
				// Unknown source, don't cache
				return false;
			}

			// Create the appropriate cache key based on data type
			if (dataType == TypeOfData.MarketData && url.Contains("period1=") && url.Contains("period2="))
			{
				// Extract date range for market data
				DateOnly startDate = ExtractPeriodFromUrl(url, "period1");
				DateOnly endDate = ExtractPeriodFromUrl(url, "period2");
				cacheKey = CacheKey.CreateMarketData(source, startDate, endDate, key);
			}
			else if (dataType == TypeOfData.Dividends)
			{
				cacheKey = CacheKey.CreateDividend(source, key);
			}
			else
			{
				cacheKey = CacheKey.CreateSymbolProfile(source, key);
			}

			return true;
		}

		private static string ExtractCoinIdFromUrl(string url)
		{
			// Extract coin ID from URLs like: https://api.coingecko.com/api/v3/coins/{coin_id}
			int coinsIndex = url.IndexOf("/coins/", StringComparison.OrdinalIgnoreCase);
			if (coinsIndex == -1)
			{
				return ComputeUrlHash(url);
			}

			int startIndex = coinsIndex + "/coins/".Length;
			int endIndex = url.IndexOf('/', startIndex);
			int queryIndex = url.IndexOf('?', startIndex);

			if (endIndex == -1 && queryIndex == -1)
			{
				return url[startIndex..];
			}

			if (endIndex == -1)
			{
				endIndex = queryIndex;
			}
			else if (queryIndex != -1)
			{
				endIndex = Math.Min(endIndex, queryIndex);
			}

			return url[startIndex..endIndex];
		}

		private static string ExtractYahooSymbolFromChartUrl(string url)
		{
			// Extract symbol from URLs like: https://query1.finance.yahoo.com/v8/finance/chart/{symbol}
			int chartIndex = url.IndexOf("/chart/", StringComparison.OrdinalIgnoreCase);
			if (chartIndex == -1)
			{
				return ComputeUrlHash(url);
			}

			int startIndex = chartIndex + "/chart/".Length;
			int queryIndex = url.IndexOf('?', startIndex);

			return queryIndex == -1 ? url[startIndex..] : url[startIndex..queryIndex];
		}

		private static string ExtractYahooSymbolsFromQuoteUrl(string url)
		{
			// Extract symbols from query parameter: ?symbols=AAPL,MSFT
			Uri? uri = Uri.TryCreate(url, UriKind.Absolute, out Uri? parsedUri) ? parsedUri : null;
			if (uri == null)
			{
				return ComputeUrlHash(url);
			}

			string? query = uri.Query;
			if (string.IsNullOrWhiteSpace(query))
			{
				return ComputeUrlHash(url);
			}

			string[] parts = query.TrimStart('?').Split('&');
			foreach (string part in parts)
			{
				if (part.StartsWith("symbols=", StringComparison.OrdinalIgnoreCase))
				{
					return part["symbols=".Length..];
				}
			}

			return ComputeUrlHash(url);
		}

		private static DateOnly ExtractPeriodFromUrl(string url, string paramName)
		{
			// Extract Unix timestamp and convert to DateOnly
			int paramIndex = url.IndexOf($"{paramName}=", StringComparison.OrdinalIgnoreCase);
			if (paramIndex == -1)
			{
				return DateOnly.MinValue;
			}

			int startIndex = paramIndex + paramName.Length + 1;
			int endIndex = url.IndexOf('&', startIndex);
			string timestampStr = endIndex == -1 ? url[startIndex..] : url[startIndex..endIndex];

			if (long.TryParse(timestampStr, out long timestamp))
			{
				DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
				return DateOnly.FromDateTime(dateTime.Date);
			}

			return DateOnly.MinValue;
		}

		private static string ComputeUrlHash(string url)
		{
			// Compute SHA256 hash of URL to use as cache key
			byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(url));
			return Convert.ToHexString(bytes)[..16]; // Take first 16 chars for brevity
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
