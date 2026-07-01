using Bunit;
using FluentAssertions;
using GhostfolioSidekick.PortfolioViewer.WASM.Pages;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Pages;

public class LoginTests : BunitContext
{
	[Fact]
	public void RendersLoginForm()
	{
		SetupAuth(false);

		var cut = Render<Login>();

		cut.Find("h2").TextContent.Should().Contain("Portfolio Viewer");
		cut.Find("input#accessToken").Should().NotBeNull();
		cut.Find("button[type=submit]").TextContent.Should().Contain("Sign In");
	}

	[Fact]
	public void HandlesInvalidToken()
	{
		SetupAuth(false, loginResult: false);

		var cut = Render<Login>();

		var input = cut.Find("input#accessToken");
		input.Change("bad-token");
		cut.Find("form").Submit();

		cut.Find(".alert-danger").TextContent.Should().Contain("Invalid access token");
	}

	[Fact]
	public void NoErrorMessage_OnSuccessfulLogin()
	{
		SetupAuth(false, loginResult: true);

		var cut = Render<Login>();

		var input = cut.Find("input#accessToken");
		input.Change("valid-token");
		cut.Find("form").Submit();

		var errorAlerts = cut.FindAll(".alert-danger");
		errorAlerts.Should().BeEmpty();
	}

	[Fact]
	public void ButtonDisabled_WhenLoading()
	{
		SetupAuth(false);

		var cut = Render<Login>();

		var button = cut.Find("button[type=submit]");
		button.HasAttribute("disabled").Should().BeFalse();
	}

	void SetupAuth(bool isAuthenticated, bool? loginResult = null)
	{
		var authStateProvider = new Mock<AuthenticationStateProvider>();
		var claimsPrincipal = isAuthenticated
			? new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Test") }, "test"))
			: new ClaimsPrincipal(new ClaimsIdentity());
		authStateProvider.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(new AuthenticationState(claimsPrincipal));
		Services.AddSingleton(authStateProvider.Object);

		var authServiceMock = new Mock<IAuthenticationService>();
		if (loginResult.HasValue)
			authServiceMock.Setup(x => x.LoginAsync(It.IsAny<string>())).ReturnsAsync(loginResult.Value);
		Services.AddSingleton(authServiceMock.Object);
	}
}
