using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.DeGiro;
using GhostfolioSidekick.FileImporter.Trading212;
using GhostfolioSidekick.Ghostfolio.API;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.Trading212
{
	public class Trading212Tests
	{
		readonly Mock<IGhostfolioAPI> api;

		public Trading212Tests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanConvertOrders_TestFileSingleOrder_True()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);

			// Act
			var canParse = await parser.CanConvertOrders(new[] { "./FileImporter/TestFiles/Trading212/Example1/TestFileSingleOrder.csv" });

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task ConvertToOrders_TestFileSingleOrder_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Asset>().With(x => x.Currency, "USD").Create();
			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("US67066G1040")).ReturnsAsync(asset);

			// Act
			var orders = await parser.ConvertToOrders(account.Name, new[] { "./FileImporter/TestFiles/Trading212/Example1/TestFileSingleOrder.csv" });

			// Assert
			orders.Should().BeEquivalentTo(new[] { new Order {
				AccountId = account.Id,
				Asset = asset,
				Comment = "Transaction Reference: [EOF3219953148]",
				Currency = asset.Currency,
				FeeCurrency = "EUR",
				Date = new DateTime(2023,08,7, 0,0,0, DateTimeKind.Utc),
				Fee = 0.02M,
				Quantity = 0.0267001M,
				Type = OrderType.BUY,
				UnitPrice = 453.33M
			} });
		}
	}
}