using Microsoft.Extensions.Logging;
using AwesomeAssertions;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Moq;
using System.Net;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Services;

public class TokenValidationServiceTests
{
	readonly TestHttpMessageHandler _httpMessageHandler;
	readonly HttpClient _httpClient;
	readonly TokenValidationService _tokenValidationService;

	public TokenValidationServiceTests()
	{
		_httpMessageHandler = new();
		_httpClient = new(_httpMessageHandler) { BaseAddress = new Uri("https://test.com/") };
		var logger = new Mock<ILogger<TokenValidationService>>();
		_tokenValidationService = new(_httpClient, logger.Object);
	}

	[Fact]
	public async Task ValidateTokenAsync_HealthAndValidateSucceed_ReturnsTrue()
	{
		_httpMessageHandler.SetupResponse(HttpStatusCode.OK);

		var result = await _tokenValidationService.ValidateTokenAsync("test-token");

		result.Should().BeTrue();
	}

	[Fact]
	public async Task ValidateTokenAsync_HealthFails_ReturnsFalse()
	{
		_httpMessageHandler.SetupResponse(HttpStatusCode.InternalServerError);

		var result = await _tokenValidationService.ValidateTokenAsync("test-token");

		result.Should().BeFalse();
	}

	[Fact]
	public async Task ValidateTokenAsync_ValidateFails_ReturnsFalse()
	{
		_httpMessageHandler.SetupResponse([HttpStatusCode.OK, HttpStatusCode.Unauthorized]);

		var result = await _tokenValidationService.ValidateTokenAsync("test-token");

		result.Should().BeFalse();
	}

	[Fact]
	public async Task ValidateTokenAsync_SendsTokenInAuthorizationHeader()
	{
		_httpMessageHandler.SetupResponse(HttpStatusCode.OK);

		await _tokenValidationService.ValidateTokenAsync("my-secret-token");

		_httpMessageHandler.Requests.Should().HaveCount(2);
		var validateRequest = _httpMessageHandler.Requests[1];
		validateRequest.Headers.Authorization.Should().NotBeNull();
		validateRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
		validateRequest.Headers.Authorization.Parameter.Should().Be("my-secret-token");
	}

	[Fact]
	public async Task ValidateTokenAsync_ApiException_ReturnsFalse()
	{
		_httpMessageHandler.ThrowException = new HttpRequestException("Network error");

		var result = await _tokenValidationService.ValidateTokenAsync("test-token");

		result.Should().BeFalse();
	}

	[Fact]
	public async Task ValidateTokenAsync_HealthNotOk_SkipsValidate()
	{
		_httpMessageHandler.SetupResponse(HttpStatusCode.BadGateway);

		var result = await _tokenValidationService.ValidateTokenAsync("test-token");

		result.Should().BeFalse();
		_httpMessageHandler.Requests.Should().HaveCount(1);
	}
}

class TestHttpMessageHandler : HttpMessageHandler
{
	public List<HttpRequestMessage> Requests { get; } = [];
	public HttpStatusCode[]? Responses { get; set; }
	public int RequestIndex { get; set; }
	public Exception? ThrowException { get; set; }

	public void SetupResponse(params HttpStatusCode[] statuses)
	{
		Responses = statuses;
	}

	public void SetupResponse(HttpStatusCode status)
	{
		Responses = [status, status];
	}

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (ThrowException is not null)
			throw ThrowException;

		Requests.Add(request);

		var statusCode = Responses is not null && RequestIndex < Responses.Length
			? Responses[RequestIndex++]
			: HttpStatusCode.OK;

		request.Dispose();
		return Task.FromResult(new HttpResponseMessage { StatusCode = statusCode });
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			// cleanup
		}
		base.Dispose(disposing);
	}
}
