using Flurl.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Fallback;
using Polly.Retry;
using System.Net;

namespace GhostfolioSidekick.ExternalDataProvider
{
	internal static class RetryPolicyHelper
	{
		public static AsyncRetryPolicy GetRetryPolicy(ILogger logger)
		{
			return Policy
			   .Handle<Exception>(x =>
			   {
				   if (x is FlurlHttpException flurlHttpException && flurlHttpException.StatusCode == (int)HttpStatusCode.NotFound)
				   {
					   return false;
				   }

				   logger.LogWarning($"An error occurred: {x.Message}");
				   return true;
			   })
			   .WaitAndRetryAsync(10, retryAttempt =>
			   {
				   return TimeSpan.FromSeconds(30);
			   }, 
			   (exception, timeSpan, retryCount, context) =>
			   {
				   logger.LogWarning($"Retry {retryCount} encountered an error: {exception.Message}. Waiting {timeSpan} before next retry.");
			   });
		}

		public static AsyncFallbackPolicy<T> GetFallbackPolicy<T>(ILogger logger)
		{
			return Policy<T>
				.Handle<WebException>()
				.Or<Exception>()
				.FallbackAsync((action) =>
				{
					logger.LogWarning("All Retries Failed");
					return Task.FromResult<T>(default!);
				});
		}
	}
}
