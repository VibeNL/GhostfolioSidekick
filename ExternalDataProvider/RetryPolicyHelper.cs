using Microsoft.Extensions.Logging;
using Polly;
using Polly.Fallback;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.ExternalDataProvider
{
	internal static class RetryPolicyHelper
	{
		public static AsyncRetryPolicy GetRetryPolicy(ILogger logger)
		{
			return Policy
			   .Handle<Exception>()
			   .WaitAndRetryAsync(3, retryAttempt =>
			   {
				   return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
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
