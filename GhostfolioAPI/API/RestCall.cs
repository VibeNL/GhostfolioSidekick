using GhostfolioSidekick.GhostfolioAPI.Contract;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using RestSharp;
using System.Diagnostics;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public class RestCall
	{
		private readonly SemaphoreSlim semaphore = new(1);

		private readonly IMemoryCache memoryCache;
		private readonly IRestClient restClient;
		private readonly ILogger<RestCall> logger;
		private readonly string url;
		private readonly string accessToken;
		private readonly RetryPolicy<RestResponse> retryPolicy;
		private readonly CircuitBreakerPolicy<RestResponse> basicCircuitBreakerPolicy;
		private readonly RestCallOptions options;

		public RestCall(
			IRestClient restClient,
			IMemoryCache memoryCache,
			ILogger<RestCall> logger,
			string url,
			string accessToken,
			RestCallOptions options)
		{
			if (string.IsNullOrEmpty(url))
			{
				throw new ArgumentException($"'{nameof(url)}' cannot be null or empty.", nameof(url));
			}

			this.memoryCache = memoryCache;
			this.restClient = restClient;
			this.logger = logger;
			this.url = url;
			this.accessToken = accessToken;

			retryPolicy = Policy
				.HandleResult<RestResponse>(x =>
				!x.IsSuccessful
				&& x.StatusCode != System.Net.HttpStatusCode.Forbidden
				&& x.StatusCode != System.Net.HttpStatusCode.BadRequest)
				.WaitAndRetry(options.MaxRetryAttempts, x => x * options.PauseBetweenFailures, (iRestResponse, timeSpan, retryCount, context) =>
				{
					logger.LogWarning("The request failed. HttpStatusCode={StatusCode}. Waiting {TotalSeconds} seconds before retry. Number attempt {RetryCount}. Uri={ResponseUri};",
						iRestResponse.Result.StatusCode,
						timeSpan.TotalSeconds,
						retryCount,
						iRestResponse.Result.ResponseUri);
				});

			basicCircuitBreakerPolicy = Policy
				.HandleResult<RestResponse>(r =>
				!r.IsSuccessStatusCode
				&& r.StatusCode != System.Net.HttpStatusCode.Forbidden
				&& r.StatusCode != System.Net.HttpStatusCode.BadRequest)
				.CircuitBreaker(2, options.CircuitBreakerDuration, (iRestResponse, timeSpan) =>
				{
					logger.LogWarning("Circuit Breaker on a break");
				}, () =>
				{
					logger.LogDebug("Circuit Breaker reset");
				});

			this.options = options;
		}

		public async Task<string?> DoRestGet(string suffixUrl, bool useCircuitBreaker = false)
		{
			Policy<RestResponse> policy = retryPolicy;
			if (useCircuitBreaker)
			{
				policy = basicCircuitBreakerPolicy.Wrap(retryPolicy);
			}

			try
			{
				await semaphore.WaitAsync();

				var request = new RestRequest($"{url}/{suffixUrl}")
				{
					RequestFormat = DataFormat.Json
				};

				request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
				request.AddHeader("Content-Type", "application/json");

				var stopwatch = new Stopwatch();

				stopwatch.Start();

				var r = policy.Execute(() => restClient.ExecuteGetAsync(request).Result);
				stopwatch.Stop();

				logger.LogTrace("Url {Url}/{SuffixUrl} took {ElapsedMilliseconds}ms", url, suffixUrl, stopwatch.ElapsedMilliseconds);

				if (!r.IsSuccessStatusCode)
				{
					if (r.StatusCode == System.Net.HttpStatusCode.Forbidden)
					{
						throw new NotAuthorizedException($"Not authorized executing url [{r.StatusCode}]: {url}/{suffixUrl}");
					}

					throw new NotSupportedException($"Error executing url [{r.StatusCode}]: {url}/{suffixUrl}");
				}

				return r.Content;
			}
			catch (BrokenCircuitException)
			{
				return null;
			}
			finally
			{
				await ExecuteTrottling();
				semaphore.Release();
			}
		}

		public async Task<RestResponse> DoRestPost(string suffixUrl, string body)
		{
			try
			{
				await semaphore.WaitAsync();

				var request = new RestRequest($"{url}/{suffixUrl}")
				{
					RequestFormat = DataFormat.Json
				};

				request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
				request.AddHeader("Content-Type", "application/json");

				request.AddJsonBody(body);
				var r = retryPolicy.Execute(() => restClient.ExecutePostAsync(request).Result);

				if (!r.IsSuccessStatusCode)
				{
					if (r.StatusCode == System.Net.HttpStatusCode.Forbidden)
					{
						throw new NotAuthorizedException($"Not authorized executing url [{r.StatusCode}]: {url}/{suffixUrl}");
					}

					throw new NotSupportedException($"Error executing url [{r.StatusCode}]: {url}/{suffixUrl}");
				}

				return r;
			}
			finally
			{
				await ExecuteTrottling();
				semaphore.Release();
			}
		}

		public async Task<RestResponse> DoRestPut(string suffixUrl, string body)
		{
			try
			{
				await semaphore.WaitAsync();

				var request = new RestRequest($"{url}/{suffixUrl}")
				{
					RequestFormat = DataFormat.Json
				};

				request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
				request.AddHeader("Content-Type", "application/json");

				request.AddJsonBody(body);
				var r = retryPolicy.Execute(() => restClient.ExecutePutAsync(request).Result);

				if (!r.IsSuccessStatusCode)
				{
					if (r.StatusCode == System.Net.HttpStatusCode.Forbidden)
					{
						throw new NotAuthorizedException($"Not authorized executing url [{r.StatusCode}]: {url}/{suffixUrl}");
					}

					throw new NotSupportedException($"Error executing url [{r.StatusCode}]: {url}/{suffixUrl}");
				}

				return r;
			}
			finally
			{
				await ExecuteTrottling();
				semaphore.Release();
			}
		}

		public async Task<RestResponse> DoRestPatch(string suffixUrl, string body)
		{
			try
			{
				await semaphore.WaitAsync();

				var request = new RestRequest($"{url}/{suffixUrl}")
				{
					RequestFormat = DataFormat.Json
				};

				request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
				request.AddHeader("Content-Type", "application/json");

				request.AddJsonBody(body);
				var r = retryPolicy.Execute(() => restClient.PatchAsync(request).Result);

				if (!r.IsSuccessStatusCode)
				{
					if (r.StatusCode == System.Net.HttpStatusCode.Forbidden)
					{
						throw new NotAuthorizedException($"Not authorized executing url [{r.StatusCode}]: {url}/{suffixUrl}");
					}

					throw new NotSupportedException($"Error executing url [{r.StatusCode}]: {url}/{suffixUrl}");
				}

				return r;
			}
			finally
			{
				await ExecuteTrottling();
				semaphore.Release();
			}
		}

		public async Task<RestResponse> DoRestDelete(string suffixUrl)
		{
			try
			{
				await semaphore.WaitAsync();

				var request = new RestRequest($"{url}/{suffixUrl}")
				{
					RequestFormat = DataFormat.Json
				};

				request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
				request.AddHeader("Content-Type", "application/json");

				var r = retryPolicy.Execute(() => restClient.DeleteAsync(request).Result);

				if (!r.IsSuccessStatusCode)
				{
					if (r.StatusCode == System.Net.HttpStatusCode.Forbidden)
					{
						throw new NotAuthorizedException($"Not authorized executing url [{r.StatusCode}]: {url}/{suffixUrl}");
					}

					throw new NotSupportedException($"Error executing url [{r.StatusCode}]: {url}/{suffixUrl}");
				}

				return r;
			}
			finally
			{
				await ExecuteTrottling();
				semaphore.Release();
			}
		}

		private Task<string> GetAuthenticationToken()
		{
			var suffixUrl = "api/v1/auth/anonymous";
			if (memoryCache.TryGetValue<string>(suffixUrl, out var result))
			{
				return Task.FromResult(result!);
			}

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
			var r = retryPolicy.Execute(() => restClient.ExecutePostAsync(request).Result);

			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Error executing url [{r.StatusCode}]: {url}/{suffixUrl}");
			}

			var content = r.Content;
			if (content == null)
			{
				throw new NotSupportedException($"No token found [{r.StatusCode}]: {url}/{suffixUrl}");
			}

			try
			{
				var token = JsonConvert.DeserializeObject<Token>(content)!.AuthToken;
				memoryCache.Set(suffixUrl, token, CacheDuration.Short());
				return Task.FromResult(token);
			}
			catch (Exception e)
			{
				throw new NotSupportedException($"No token could be found [{r.StatusCode}]: {url}/{suffixUrl}. Exception {e.Message}");
			}
		}

		private Task ExecuteTrottling()
		{
			// Naive implementation of trottling for now
			return Task.Delay(options.TrottleTimeout);
		}
	}
}
