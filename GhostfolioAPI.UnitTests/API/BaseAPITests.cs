using GhostfolioSidekick.GhostfolioAPI.API;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using RestSharp;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API
{
	public class BaseAPITests
	{
		protected readonly Mock<IRestClient> restClient;
		protected readonly RestCall restCall;

		public BaseAPITests()
		{
			restClient = new Mock<IRestClient>();
			restCall = new RestCall(restClient.Object, new MemoryCache(new MemoryCacheOptions()), new Mock<ILogger<RestCall>>().Object, "https://www.google.com", "wow");

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains("api/v1/auth/anonymous")), default))
				.ReturnsAsync(CreateResponse(true, "{\"authToken\":\"abcd\"}"));
		}
		protected RestResponse CreateResponse(bool succesfull, string? response = null)
		{
			return new RestResponse
			{
				IsSuccessStatusCode = succesfull,
				StatusCode = succesfull ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.BadRequest,
				ResponseStatus = ResponseStatus.Completed,
				Content = response
			};
		}
	}
}