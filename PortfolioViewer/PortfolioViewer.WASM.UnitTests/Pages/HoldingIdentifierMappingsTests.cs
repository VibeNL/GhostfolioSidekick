using Bunit;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Pages;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using GhostfolioSidekick.Model.Activities;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Pages
{
	public class HoldingIdentifierMappingsTests : TestContext
	{
		[Fact]
		public void HoldingIdentifierMappings_ShouldRenderLoadingState_Initially()
		{
			// Arrange
			var mockService = new Mock<IHoldingIdentifierMappingService>();
			var mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
			var mockTestContextService = new Mock<ITestContextService>();

			Services.AddScoped(_ => mockService.Object);
			Services.AddScoped(_ => mockAuthStateProvider.Object);
			Services.AddScoped(_ => mockTestContextService.Object);

			// Setup authentication
			var authState = Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
			mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync()).Returns(authState);

			// Setup a task that never completes to simulate loading
			var neverCompletingTask = new TaskCompletionSource<List<HoldingIdentifierMappingModel>>();
			mockService.Setup(x => x.GetAllHoldingIdentifierMappingsAsync(It.IsAny<CancellationToken>()))
		.Returns(neverCompletingTask.Task);

			// Act
			var component = RenderComponent<HoldingIdentifierMappings>();

			// Assert
			Assert.Contains("Loading Identifier Mappings...", component.Markup);
			Assert.Contains("spinner-border", component.Markup);
		}

		[Fact]
		public void HoldingIdentifierMappings_ShouldRenderEmptyState_WhenNoMappingsFound()
		{
			// Arrange
			var mockService = new Mock<IHoldingIdentifierMappingService>();
			var mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
			var mockTestContextService = new Mock<ITestContextService>();

			Services.AddScoped(_ => mockService.Object);
			Services.AddScoped(_ => mockAuthStateProvider.Object);
			Services.AddScoped(_ => mockTestContextService.Object);

			// Setup authentication
			var authState = Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
			mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync()).Returns(authState);

			// Setup service to return empty list
			mockService.Setup(x => x.GetAllHoldingIdentifierMappingsAsync(It.IsAny<CancellationToken>()))
 .ReturnsAsync([]);

			// Act
			var component = RenderComponent<HoldingIdentifierMappings>();

			// Assert loading spinner appears first
			Assert.Contains("spinner-border", component.Markup);

			// Wait for the empty state to appear
			component.WaitForAssertion(() =>
			{
				Assert.Contains("No Identifier Mappings Found", component.Markup);
				Assert.Contains("No holdings with identifier mappings were found", component.Markup);
			});
		}

		[Fact]
		public void HoldingIdentifierMappings_ShouldRenderMappingsTable_WhenMappingsExist()
		{
			// Arrange
			var mockService = new Mock<IHoldingIdentifierMappingService>();
			var mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
			var mockTestContextService = new Mock<ITestContextService>();

			Services.AddScoped(_ => mockService.Object);
			Services.AddScoped(_ => mockAuthStateProvider.Object);
			Services.AddScoped(_ => mockTestContextService.Object);

			// Setup authentication
			var authState = Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
			mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync()).Returns(authState);

			// Setup test data
			var testMappings = new List<HoldingIdentifierMappingModel>
			{
	 new()
		 {
		 Symbol = "AAPL",
   Name = "Apple Inc.",
   HoldingId = 1,
   PartialIdentifiers =
	   [
   new()
   {
		 Identifier = "AAPL",
  MatchedDataProviders = ["YAHOO"],
	 HasUnresolvedMapping = false
	}
		],
		DataProviderMappings =
	  [
	   new()
		{
	DataSource = "YAHOO",
  Symbol = "AAPL",
	   Name = "Apple Inc.",
	  Currency = "USD",
		AssetClass = AssetClass.Equity,
		Identifiers = ["AAPL"],
	MatchedPartialIdentifiers = ["AAPL"]
	  }
		 ]
	 }
		  };

			mockService.Setup(x => x.GetAllHoldingIdentifierMappingsAsync(It.IsAny<CancellationToken>()))
		  .ReturnsAsync(testMappings);

			// Act
			var component = RenderComponent<HoldingIdentifierMappings>();

			// Wait for the async operation to complete and assert expected content
			component.WaitForAssertion(() =>
			{
				Assert.Contains("AAPL", component.Markup);
				Assert.Contains("Apple Inc.", component.Markup);
				Assert.Contains("Holdings Identifier Mappings Overview", component.Markup);
			});
		}
	}
}