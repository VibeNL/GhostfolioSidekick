using GhostfolioSidekick.GhostfolioAPI.API;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using RestSharp;
using System.Net;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API
{
	public class BaseAPITests
	{
		protected readonly Mock<IRestClient> restClient;
		protected readonly RestCall restCall;

		public BaseAPITests()
		{
			restClient = new Mock<IRestClient>();
			restCall = new RestCall(
				restClient.Object, 
				new MemoryCache(new MemoryCacheOptions()),
				new Mock<ILogger<RestCall>>().Object,
				"https://www.google.com",
				"wow", 
				new RestCallOptions { CircuitBreakerDuration = TimeSpan.Zero, MaxRetryAttempts = 1, PauseBetweenFailures = TimeSpan.Zero });

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains("api/v1/auth/anonymous")), default))
				.ReturnsAsync(CreateResponse(HttpStatusCode.OK, "{\"authToken\":\"abcd\"}"));
		}
		
		protected RestResponse CreateResponse(HttpStatusCode httpStatusCode, string? response = null)
		{
			return new RestResponse
			{
				IsSuccessStatusCode = httpStatusCode == HttpStatusCode.OK,
				StatusCode = httpStatusCode,
				ResponseStatus = ResponseStatus.Completed,
				Content = response
			};
		}
	}
}