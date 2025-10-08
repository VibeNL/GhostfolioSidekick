using Flurl.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Fallback;
using Polly.Retry;
using System.Net;

namespace GhostfolioSidekick.ExternalDataProvider
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

				   if (x is not null && x.Message.Contains("'System.Dynamic.ExpandoObject' does not contain a definition for"))
				   {
					   return false;
				   }

				   logger.LogWarning(x, "An error occurred: {Message}", x?.Message);
				   return true;
			   })
			   .WaitAndRetryAsync(10, retryAttempt =>
			   {
				   return TimeSpan.FromSeconds(30);
			   },
			   (exception, timeSpan, retryCount, context) =>
			   {
				   logger.LogWarning(exception, "Retry {RetryCount} encountered an error: {Message}. Waiting {TimeSpan} before next retry.", retryCount, exception.Message, timeSpan);
			   });
		}

		public static AsyncFallbackPolicy<T> GetFallbackPolicy<T>(ILogger logger)
		{
			return Policy<T>
				.Handle<Exception>()
				.FallbackAsync((action) =>
				{
					return Task.FromResult<T>(default!);
				}, (ex) =>
				{
					if (ex.Exception is not FlurlHttpException flurlHttpException || flurlHttpException.StatusCode != (int)HttpStatusCode.NotFound)
					{
						logger.LogWarning("All Retries Failed");
					}

					return Task.FromResult<T>(default!);
				});
		}
	}
}
