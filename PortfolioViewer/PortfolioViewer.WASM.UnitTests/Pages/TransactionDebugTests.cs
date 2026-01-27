using Bunit;
using GhostfolioSidekick.PortfolioViewer.WASM.Pages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Pages
{
    public class TransactionDebugTests : BunitContext
	{
        [Fact]
        public void RendersAccountDropdown()
        {
            // Arrange
            var mockAccountService = new Mock<IAccountDataService>();
            mockAccountService.Setup(s => s.GetAccountInfo())
                .ReturnsAsync([
                    new() { Id = 1, Name = "Test Account 1" },
                    new() { Id = 2, Name = "Test Account 2" }
                ]);
            Services.AddSingleton(mockAccountService.Object);
            Services.AddSingleton(Mock.Of<ITransactionService>());

            // Act
            var cut = Render<TransactionDebug>();

            // Assert
            var select = cut.Find("select#accountSelect");
            Assert.NotNull(select);
            Assert.Contains("Test Account 1", select.InnerHtml);
            Assert.Contains("Test Account 2", select.InnerHtml);
        }

        [Fact]
        public void ShowsLoadingIndicatorOnAccountChange()
        {
            // Arrange
            var mockAccountService = new Mock<IAccountDataService>();
            mockAccountService.Setup(s => s.GetAccountInfo())
                .ReturnsAsync([new() { Id = 1, Name = "Test Account" }]);
            var mockTransactionService = new Mock<ITransactionService>();
            mockTransactionService.Setup(s => s.GetTransactionsPaginatedAsync(It.IsAny<TransactionQueryParameters>(), default))
                .ReturnsAsync(new PaginatedTransactionResult { Transactions = [] });
            Services.AddSingleton(mockAccountService.Object);
            Services.AddSingleton(mockTransactionService.Object);

            var cut = Render<TransactionDebug>();
            var select = cut.Find("select#accountSelect");

            // Act
            select.Change("1");

            // Assert
            cut.WaitForAssertion(() =>
                Assert.Contains("Loading transaction debug data", cut.Markup),
                timeout: TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void FiltersOutEmptyAssetPositions()
        {
            // Arrange
            var mockAccountService = new Mock<IAccountDataService>();
            mockAccountService.Setup(s => s.GetAccountInfo())
                .ReturnsAsync([new() { Id = 1, Name = "Test Account" }]);
            var tx = new TransactionDisplayModel
            {
                Date = DateTime.Now,
                Type = "Buy",
                Symbol = "AAPL",
                AccountName = "Test Account",
                Quantity = 0,
                TotalValue = new Model.Money(GhostfolioSidekick.Model.Currency.USD, 100),
                TransactionId = "T1"
            };
            var mockTransactionService = new Mock<ITransactionService>();
            mockTransactionService.Setup(s => s.GetTransactionsPaginatedAsync(It.IsAny<TransactionQueryParameters>(), default))
                .ReturnsAsync(new PaginatedTransactionResult { Transactions = [tx] });
            Services.AddSingleton(mockAccountService.Object);
            Services.AddSingleton(mockTransactionService.Object);

            var cut = Render<TransactionDebug>();
            var select = cut.Find("select#accountSelect");
            select.Change("1");

            // Act
            cut.WaitForAssertion(() =>
            {
                var assetCells = cut.FindAll("ul.mb-0");
                Assert.All(assetCells, ul => Assert.DoesNotContain("AAPL", ul.InnerHtml));
            }, timeout: TimeSpan.FromSeconds(1));
        }
    }
}


