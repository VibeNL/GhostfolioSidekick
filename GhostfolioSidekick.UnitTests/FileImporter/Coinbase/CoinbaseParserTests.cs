using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.Coinbase;
using GhostfolioSidekick.Ghostfolio.API;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.DeGiro
{
	public class CoinbaseParserTests
	{
		readonly Mock<IGhostfolioAPI> api;

		public CoinbaseParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanConvertOrders_Example1_True()
		{
			// Arrange
			var parser = new CoinbaseParser(api.Object);

			// Act
			var canParse = await parser.CanConvertOrders(new[] { "./FileImporter/TestFiles/Coinbase/Example1/Example1.csv" });

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task ConvertToOrders_Example1_Converted()
		{
			// Arrange
			var parser = new CoinbaseParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();
			var asset2 = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();
			var asset3 = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();
			var asset4 = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();

			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("Bitcoin")).ReturnsAsync(asset1);
			api.Setup(x => x.FindSymbolByISIN("Ethereum")).ReturnsAsync(asset2);
			api.Setup(x => x.FindSymbolByISIN("Eth 2.0 Staking by Pool-X")).ReturnsAsync(asset2);
			api.Setup(x => x.FindSymbolByISIN("Cosmos")).ReturnsAsync(asset3);
			api.Setup(x => x.FindSymbolByISIN("USD Coin")).ReturnsAsync(asset4);

			// Act
			var orders = await parser.ConvertToOrders(account.Name, new[] { "./FileImporter/TestFiles/Coinbase/Example1/Example1.csv" });

			// Assert
			orders.Should().BeEquivalentTo(new[]
			{
			new Order {
				AccountId = account.Id,
				Asset = asset1,
				Comment = "Transaction Reference: [SELL_BTC_638280698190000000]",
				Currency = asset1.Currency,
				FeeCurrency = asset1.Currency,
				Date = new DateTime(2023,08,19,17,23,39, DateTimeKind.Utc),
				Fee = 0,
				Quantity = 0.00205323M,
				Type = OrderType.SELL,
				UnitPrice = 24073.28M,
				ReferenceCode = "SELL_BTC_638280698190000000"
			}});
		}
	}
}