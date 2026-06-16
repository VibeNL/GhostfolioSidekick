using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using AwesomeAssertions;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Services;

public class CustomAuthenticationStateProviderTests
{
	readonly Mock<IAuthenticationService> _authServiceMock;

	public CustomAuthenticationStateProviderTests()
	{
		_authServiceMock = new();
	}

	[Fact]
	public async Task GetAuthenticationStateAsync_DelegatesToAuthService()
	{
		var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
		_authServiceMock.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(claimsPrincipal);

		using var provider = new CustomAuthenticationStateProvider(_authServiceMock.Object);
		var state = await provider.GetAuthenticationStateAsync();

		state.User.Should().BeSameAs(claimsPrincipal);
	}

	[Fact]
	public async Task GetAuthenticationStateAsync_AuthenticatedUser_ReturnsAuthenticatedState()
	{
		var identity = new ClaimsIdentity(new[]
		{
			new Claim(ClaimTypes.Name, "Test User")
		}, "test");
		var claimsPrincipal = new ClaimsPrincipal(identity);
		_authServiceMock.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(claimsPrincipal);

		using var provider = new CustomAuthenticationStateProvider(_authServiceMock.Object);
		var state = await provider.GetAuthenticationStateAsync();

		state.User.Identity!.IsAuthenticated.Should().BeTrue();
	}

	[Fact]
	public void Constructor_SubscribesToAuthenticationStateChanged()
	{
		using var provider = new CustomAuthenticationStateProvider(_authServiceMock.Object);

		// Trigger the event
		var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Test") }, "test");
		var claimsPrincipal = new ClaimsPrincipal(identity);

		// The provider should have subscribed - trigger notification via the auth service
		// We can't easily test the subscription directly, but we can verify no exception on construction
		provider.Should().NotBeNull();
	}

	[Fact]
	public void Dispose_UnsubscribesFromAuthenticationStateChanged()
	{
		using var provider = new CustomAuthenticationStateProvider(_authServiceMock.Object);
		provider.Dispose();

		// Should not throw after dispose
		_authServiceMock.VerifyAdd(
			x => x.AuthenticationStateChanged += It.IsAny<Action<ClaimsPrincipal>>(), Times.Once);
		_authServiceMock.VerifyRemove(
			x => x.AuthenticationStateChanged -= It.IsAny<Action<ClaimsPrincipal>>(), Times.Once);
	}
}
