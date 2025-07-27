using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using GhostfolioSidekick.PortfolioViewer.WASM.Pages;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.Model;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests
{
    public class HoldingsRazorTests : TestContext
    {
        [Fact]
        public void Holdings_ShowsLoadingState_WhenIsLoadingIsTrue()
        {
            // Arrange
            var mockService = new Mock<IHoldingsDataService>();
            Services.AddSingleton(mockService.Object);

            // Act
            var cut = RenderComponent<Holdings>();

            // Assert
            Assert.Contains("Loading Portfolio Data...", cut.Markup);
        }

        [Fact]
        public void Holdings_ShowsErrorState_WhenHasErrorIsTrue()
        {
            // Arrange
            var mockService = new Mock<IHoldingsDataService>();
            Services.AddSingleton(mockService.Object);
            // Render and set error state via reflection
            var cut = RenderComponent<Holdings>();
            cut.Instance.GetType().GetProperty("HasError", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(cut.Instance, true);
            cut.Instance.GetType().GetProperty("ErrorMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(cut.Instance, "Test error!");
            cut.Render();

            // Assert
            Assert.Contains("Error Loading Data", cut.Markup);
            Assert.Contains("Test error!", cut.Markup);
            Assert.Contains("Try Again", cut.Markup);
        }

        private class FakeHoldingsDataService : IHoldingsDataService
        {
            private readonly List<HoldingDisplayModel> _holdings;
            public FakeHoldingsDataService(List<HoldingDisplayModel> holdings) => _holdings = holdings;
            public Task<List<HoldingDisplayModel>> GetHoldingsAsync(Currency targetCurrency, CancellationToken cancellationToken = default) => Task.FromResult(_holdings);
        }

        [Fact]
        public void Holdings_ShowsEmptyState_WhenHoldingsListIsEmpty()
        {
            // Arrange
            Services.AddSingleton<IHoldingsDataService>(new FakeHoldingsDataService(new List<HoldingDisplayModel>()));
            var cut = RenderComponent<Holdings>();

            // Act
            cut.WaitForAssertion(() => Assert.Contains("No Holdings Found", cut.Markup));
        }

        [Fact]
        public void Holdings_RendersTable_WhenHoldingsExistAndViewModeIsTable()
        {
            // Arrange
            var holdings = new List<HoldingDisplayModel>
            {
                new HoldingDisplayModel {
                    Symbol = "AAPL", Name = "Apple Inc.", Quantity = 10, AveragePrice = 100, CurrentPrice = 150, CurrentValue = 1500, GainLoss = 500, GainLossPercentage = 0.5m, Weight = 1, Sector = "Tech", AssetClass = "Equity", Currency = "USD"
                }
            };
            Services.AddSingleton<IHoldingsDataService>(new FakeHoldingsDataService(holdings));
            var cut = RenderComponent<Holdings>();
            // Switch to table view
            cut.FindAll("button").First(b => b.TextContent.Contains("Table")).Click();

            // Assert
            cut.WaitForAssertion(() => Assert.Contains("Apple Inc.", cut.Markup));
            Assert.Contains("AAPL", cut.Markup);
            Assert.Contains("Current Value", cut.Markup);
        }

        [Fact]
        public void Holdings_RendersTreemap_WhenViewModeIsTreemap()
        {
            // Arrange
            var holdings = new List<HoldingDisplayModel>
            {
                new HoldingDisplayModel {
                    Symbol = "AAPL", Name = "Apple Inc.", Quantity = 10, AveragePrice = 100, CurrentPrice = 150, CurrentValue = 1500, GainLoss = 500, GainLossPercentage = 0.5m, Weight = 1, Sector = "Tech", AssetClass = "Equity", Currency = "USD"
                }
            };
            Services.AddSingleton<IHoldingsDataService>(new FakeHoldingsDataService(holdings));
            var cut = RenderComponent<Holdings>();

            // Assert
            // Treemap is default view
            cut.WaitForAssertion(() => Assert.Contains("treemap-container", cut.Markup));
        }
    }
}
