using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;
using RestSharp;
using System.Diagnostics;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public class RestCall
	{
		private Mutex mutex = new Mutex();

		private static int _maxRetryAttempts = 5;
		private static TimeSpan _pauseBetweenFailures = TimeSpan.FromSeconds(1);

		private readonly IMemoryCache memoryCache;
		private readonly ILogger<GhostfolioAPI> logger;
		private readonly string url;
		private readonly string accessToken;
		private readonly RetryPolicy<RestResponse> retryPolicy;

		public RestCall(
			IMemoryCache memoryCache,
			ILogger<GhostfolioAPI> logger,
			string url,
			string accessToken)
		{
			this.memoryCache = memoryCache;
			this.logger = logger;
			this.url = url;
			this.accessToken = accessToken;

			retryPolicy = Policy
				.HandleResult<RestResponse>(x => !x.IsSuccessful)
				.WaitAndRetry(_maxRetryAttempts, x => _pauseBetweenFailures, async (iRestResponse, timeSpan, retryCount, context) =>
				{
					logger.LogDebug($"The request failed. HttpStatusCode={iRestResponse.Result.StatusCode}. Waiting {timeSpan} seconds before retry. Number attempt {retryCount}. Uri={iRestResponse.Result.ResponseUri};");
				});
		}

		public async Task<string?> DoRestGet(string suffixUrl, MemoryCacheEntryOptions cacheEntryOptions)
		{
			if (memoryCache.TryGetValue<string?>(suffixUrl, out var result))
			{
				return result;
			}

			try
			{
				mutex.WaitOne();

				if (memoryCache.TryGetValue<string?>(suffixUrl, out var result2))
				{
					return result2;
				}

				var options = new RestClientOptions(url)
				{
					ThrowOnAnyError = false,
					ThrowOnDeserializationError = false,
				};

				var client = new RestClient(options);
				var request = new RestRequest($"{url}/{suffixUrl}")
				{
					RequestFormat = DataFormat.Json
				};

				request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
				request.AddHeader("Content-Type", "application/json");

				var stopwatch = new Stopwatch();

				stopwatch.Start();
				var r = retryPolicy.Execute(() => client.ExecuteGetAsync(request).Result);
				stopwatch.Stop();

				logger.LogDebug($"Url {url}/{suffixUrl} took {stopwatch.ElapsedMilliseconds}ms");

				if (!r.IsSuccessStatusCode)
				{
					throw new NotSupportedException($"Error executing url [{r.StatusCode}]: {url}/{suffixUrl}");
				}

				memoryCache.Set(suffixUrl, r.Content, cacheEntryOptions);

				return r.Content;
			}
			finally
			{
				//Call the ReleaseMutex method to unblock so that other threads
				//that are trying to gain ownership of the mutex can enter  
				mutex.ReleaseMutex();
			}
		}

		public async Task<RestResponse?> DoRestPost(string suffixUrl, string body)
		{
			try
			{
				mutex.WaitOne();

				var options = new RestClientOptions(url)
				{
					ThrowOnAnyError = false,
					ThrowOnDeserializationError = false
				};

				var client = new RestClient(options);
				var request = new RestRequest($"{url}/{suffixUrl}")
				{
					RequestFormat = DataFormat.Json
				};

				request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
				request.AddHeader("Content-Type", "application/json");

				request.AddJsonBody(body);
				var r = retryPolicy.Execute(() => client.ExecutePostAsync(request).Result);

				if (!r.IsSuccessStatusCode)
				{
					throw new NotSupportedException($"Error executing url [{r.StatusCode}]: {url}/{suffixUrl}");
				}

				return r;
			}
			finally
			{
				//Call the ReleaseMutex method to unblock so that other threads
				//that are trying to gain ownership of the mutex can enter  
				mutex.ReleaseMutex();
			}
		}

		public async Task<RestResponse?> DoRestDelete(string suffixUrl)
		{
			try
			{
				mutex.WaitOne();

				var options = new RestClientOptions(url)
				{
					ThrowOnAnyError = false,
					ThrowOnDeserializationError = false
				};

				var client = new RestClient(options);
				var request = new RestRequest($"{url}/{suffixUrl}")
				{
					RequestFormat = DataFormat.Json
				};

				request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
				request.AddHeader("Content-Type", "application/json");

				var r = retryPolicy.Execute(() => client.DeleteAsync(request).Result);

				if (!r.IsSuccessStatusCode)
				{
					throw new NotSupportedException($"Error executing url [{r.StatusCode}]: {url}/{suffixUrl}");
				}

				return r;
			}
			finally
			{
				//Call the ReleaseMutex method to unblock so that other threads
				//that are trying to gain ownership of the mutex can enter  
				mutex.ReleaseMutex();
			}
		}

		private async Task<string> GetAuthenticationToken()
		{
			var suffixUrl = "api/v1/auth/anonymous";
			if (memoryCache.TryGetValue<string?>(suffixUrl, out var result))
			{
				return result;
			}

			var options = new RestClientOptions(url)
			{
				ThrowOnAnyError = false,
				ThrowOnDeserializationError = false
			};

			var client = new RestClient(options);
			var request = new RestRequest($"{url}/{suffixUrl}")
			{
				RequestFormat = DataFormat.Json
			};

			request.AddHeader("Content-Type", "application/json");

			var body = new JObject
			{
				["accessToken"] = accessToken
			};
			request.AddJsonBody(body.ToString());
			var r = retryPolicy.Execute(() => client.ExecutePostAsync(request).Result);

			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Error executing url [{r.StatusCode}]: {url}/{suffixUrl}");
			}

			dynamic stuff = JsonConvert.DeserializeObject(r.Content);
			string token = stuff.authToken.ToString();

			memoryCache.Set(suffixUrl, token, CacheDuration.Short());
			return token;
		}
	}
}
