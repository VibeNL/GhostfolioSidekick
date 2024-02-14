using Xunit;
using Moq;
using FluentAssertions;
using GhostfolioSidekick.GhostfolioAPI.API;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RestSharp;
using System;
using System.Net;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using Newtonsoft.Json;
using AutoFixture;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API
{
	public class RestCallTests
	{
		private readonly Mock<IRestClient> restClientMock;
		private readonly IMemoryCache memoryCache;
		private readonly Mock<ILogger<RestCall>> loggerMock;
		private readonly RestCall restCall;

		public RestCallTests()
		{
			restClientMock = new Mock<IRestClient>();
			memoryCache = new MemoryCache(new MemoryCacheOptions());
			loggerMock = new Mock<ILogger<RestCall>>();

			restCall = new RestCall(
				restClientMock.Object,
				memoryCache,
				loggerMock.Object,
				"http://testurl.com",
				"testToken");
		}

		[Fact]
		public async Task DoRestGet_NoAuthToken_Success()
		{
			// Arrange
			var restResponse = new RestResponse
			{
				IsSuccessStatusCode = false,
				StatusCode = HttpStatusCode.OK,
				Content = "test content"
			};
			SetupClient(false, [restResponse]);

			// Act
			var a = () => restCall.DoRestGet("testSuffixUrl");

			// Assert
			await this.Invoking(_ => a()).Should().ThrowAsync<NotSupportedException>();
		}

		[Fact]
		public async Task DoRestGet_CircuitBreaker_Triggered()
		{
			// Arrange
			var restResponse = CreateTimeoutResponse("test content");
			SetupClient(true, [restResponse]);

			// Act
			for (int i = 0; i < 10; i++)
			{
				try
				{
					await restCall.DoRestGet("testSuffixUrl", true);
				}
				catch
				{
					// Ignore
				}
			}

			// Assert
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString() == "Circuit Breaker on a break"),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public async Task DoRestGet_Success()
		{
			// Arrange
			var restResponse = CreateSuccessResponse("test content");
			SetupClient(true, [restResponse]);

			// Act
			var result = await restCall.DoRestGet("testSuffixUrl");

			// Assert
			result.Should().Be(restResponse.Content);
		}

		[Fact]
		public async Task DoRestGet_Failed()
		{
			// Arrange
			var restResponse = CreateBadRequestResponse("test content");
			SetupClient(true, [restResponse]);

			// Act
			Func<Task> act = async () => { await restCall.DoRestGet("testSuffixUrl"); };

			// Assert
			await act.Should().ThrowAsync<NotSupportedException>();
		}

		[Fact]
		public async Task DoRestPost_Success()
		{
			// Arrange
			var restResponse = CreateSuccessResponse("test content");
			SetupClient(true, [restResponse]);

			// Act
			var result = await restCall.DoRestPost("testSuffixUrl", string.Empty);

			// Assert
			result.Should().Be(restResponse);
		}

		[Fact]
		public async Task DoRestPost_Failed()
		{
			// Arrange
			var restResponse = CreateBadRequestResponse("test content");
			SetupClient(true, [restResponse]);

			// Act
			Func<Task> act = async () => { await restCall.DoRestPost("testSuffixUrl", string.Empty); };

			// Assert
			await act.Should().ThrowAsync<NotSupportedException>();
		}

		[Fact]
		public async Task DoRestPut_Success()
		{
			// Arrange
			var restResponse = CreateSuccessResponse("test content");
			SetupClient(true, [restResponse]);

			// Act
			var result = await restCall.DoRestPut("testSuffixUrl", string.Empty);

			// Assert
			result.Should().Be(restResponse);
		}

		[Fact]
		public async Task DoRestPut_Failed()
		{
			// Arrange
			var restResponse = CreateBadRequestResponse("test content");
			SetupClient(true, [restResponse]);

			// Act
			Func<Task> act = async () => { await restCall.DoRestPut("testSuffixUrl", string.Empty); };

			// Assert
			await act.Should().ThrowAsync<NotSupportedException>();
		}

		[Fact]
		public async Task DoRestDelete_Success()
		{
			// Arrange
			var restResponse = CreateSuccessResponse("test content");
			SetupClient(true, [restResponse]);

			// Act
			var result = await restCall.DoRestDelete("testSuffixUrl");

			// Assert
			result.Should().Be(restResponse);
		}

		[Fact]
		public async Task DoRestDelete_Failed()
		{
			// Arrange
			var restResponse = CreateBadRequestResponse("test content");
			SetupClient(true, [restResponse]);

			// Act
			Func<Task> act = async () => { await restCall.DoRestDelete("testSuffixUrl"); };

			// Assert
			await act.Should().ThrowAsync<NotSupportedException>();
		}

		[Fact]
		public async Task DoRestPatch_Success()
		{
			// Arrange
			var restResponse = CreateSuccessResponse("test content");
			SetupClient(true, [restResponse]);

			// Act
			var result = await restCall.DoRestPatch("testSuffixUrl", string.Empty);

			// Assert
			result.Should().Be(restResponse);
		}

		[Fact]
		public async Task DoRestPatch_Failed()
		{
			// Arrange
			var restResponse = CreateBadRequestResponse("test content");
			SetupClient(true, [restResponse]);

			// Act
			Func<Task> act = async () => { await restCall.DoRestPatch("testSuffixUrl", string.Empty); };

			// Assert
			await act.Should().ThrowAsync<NotSupportedException>();
		}

		private void SetupClient(bool withAuth, RestResponse[] responses)
		{
			var seq = restClientMock.SetupSequence(x => x.ExecuteAsync(It.IsAny<RestRequest>(), default));
			if (withAuth)
			{
				var content = new Fixture().Create<Token>();
				var serialized = JsonConvert.SerializeObject(content);
				RestResponse authResponse = CreateSuccessResponse(serialized);
				seq.ReturnsAsync(authResponse);
			}

			foreach (var response in responses)
			{
				seq.ReturnsAsync(response);
			}

			for (int i = 0; i < 100; i++)
			{
				seq.ReturnsAsync(responses[responses.Length - 1]);
			}
		}

		private static RestResponse CreateSuccessResponse(string serialized)
		{
			return new RestResponse
			{
				IsSuccessStatusCode = true,
				ResponseStatus = ResponseStatus.Completed,
				StatusCode = HttpStatusCode.OK,
				Content = serialized
			};
		}

		private static RestResponse CreateBadRequestResponse(string serialized)
		{
			return new RestResponse
			{
				IsSuccessStatusCode = false,
				ResponseStatus = ResponseStatus.Completed,
				StatusCode = HttpStatusCode.BadRequest,
				Content = serialized
			};
		}

		private static RestResponse CreateTimeoutResponse(string serialized)
		{
			return new RestResponse
			{
				IsSuccessStatusCode = false,
				ResponseStatus = ResponseStatus.Aborted,
				StatusCode = HttpStatusCode.GatewayTimeout,
				Content = serialized
			};
		}
	}
}
