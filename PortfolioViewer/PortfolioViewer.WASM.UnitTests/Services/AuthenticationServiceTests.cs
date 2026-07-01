using Microsoft.JSInterop;
using System.Security.Claims;
using AwesomeAssertions;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Services;

public class AuthenticationServiceTests
{
	readonly Mock<ITokenValidationService> _tokenValidationServiceMock;
	readonly Mock<IJSRuntime> _jsRuntimeMock;
	readonly AuthenticationService _authenticationService;

	public AuthenticationServiceTests()
	{
		_tokenValidationServiceMock = new();
		_jsRuntimeMock = new();
		_authenticationService = new(_tokenValidationServiceMock.Object, _jsRuntimeMock.Object);
	}

	[Fact]
	public async Task LoginAsync_ValidToken_ReturnsTrue()
	{
		_tokenValidationServiceMock.Setup(x => x.ValidateTokenAsync(It.IsAny<string>())).ReturnsAsync(true);

		var result = await _authenticationService.LoginAsync("test-token");

		result.Should().BeTrue();
	}

	[Fact]
	public async Task LoginAsync_InvalidToken_ReturnsFalse()
	{
		_tokenValidationServiceMock.Setup(x => x.ValidateTokenAsync(It.IsAny<string>())).ReturnsAsync(false);

		var result = await _authenticationService.LoginAsync("bad-token");

		result.Should().BeFalse();
	}

	[Fact]
	public async Task LoginAsync_Success_RaisesAuthenticationStateChanged()
	{
		_tokenValidationServiceMock.Setup(x => x.ValidateTokenAsync(It.IsAny<string>())).ReturnsAsync(true);

		var principalReceived = default(ClaimsPrincipal?);
		_authenticationService.AuthenticationStateChanged += p => principalReceived = p;

		await _authenticationService.LoginAsync("test-token");

		principalReceived.Should().NotBeNull();
		principalReceived!.Identity!.IsAuthenticated.Should().BeTrue();
	}

	[Fact]
	public async Task LogoutAsync_ClearsAuthentication()
	{
		_tokenValidationServiceMock.Setup(x => x.ValidateTokenAsync(It.IsAny<string>())).ReturnsAsync(true);
		await _authenticationService.LoginAsync("test-token");

		await _authenticationService.LogoutAsync();

		var state = await _authenticationService.GetAuthenticationStateAsync();
		state.Identity!.IsAuthenticated.Should().BeFalse();
	}

	[Fact]
	public async Task GetAuthenticationStateAsync_AfterLogin_ReturnsAuthenticatedUser()
	{
		_tokenValidationServiceMock.Setup(x => x.ValidateTokenAsync(It.IsAny<string>())).ReturnsAsync(true);
		await _authenticationService.LoginAsync("test-token");

		var state = await _authenticationService.GetAuthenticationStateAsync();

		state.Identity!.IsAuthenticated.Should().BeTrue();
		state.FindFirst(ClaimTypes.Name)!.Value.Should().Be("Authenticated User");
	}

	[Fact]
	public async Task GetAuthenticationStateAsync_NoAuth_ReturnsAnonymousUser()
	{
		var state = await _authenticationService.GetAuthenticationStateAsync();

		state.Identity!.IsAuthenticated.Should().BeFalse();
	}

	[Fact]
	public async Task GetAuthenticationStateAsync_WithStoredToken_Valid_ReturnsAuthenticated()
	{
		_jsRuntimeMock.Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
			.ReturnsAsync("stored-token");
		_tokenValidationServiceMock.Setup(x => x.ValidateTokenAsync("stored-token")).ReturnsAsync(true);

		var state = await _authenticationService.GetAuthenticationStateAsync();

		state.Identity!.IsAuthenticated.Should().BeTrue();
	}

	[Fact]
	public async Task GetAuthenticationStateAsync_WithStoredToken_Invalid_RemovesToken()
	{
		_jsRuntimeMock.Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
			.ReturnsAsync("bad-stored-token");
		_tokenValidationServiceMock.Setup(x => x.ValidateTokenAsync("bad-stored-token")).ReturnsAsync(false);

		var state = await _authenticationService.GetAuthenticationStateAsync();

		state.Identity!.IsAuthenticated.Should().BeFalse();
		_tokenValidationServiceMock.Verify(x => x.ValidateTokenAsync("bad-stored-token"), Times.Once);
	}
}
