using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.Coinbase;
using GhostfolioSidekick.Ghostfolio.API;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.Coinbase
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

			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("Bitcoin", It.IsAny<Func<IEnumerable<Asset>, Asset>>())).ReturnsAsync(asset1);

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

		[Fact]
		public async Task ConvertToOrders_Example2_Converted()
		{
			// Arrange
			var parser = new CoinbaseParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();
			var asset2 = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();
			var asset3 = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();

			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("Bitcoin", It.IsAny<Func<IEnumerable<Asset>, Asset>>())).ReturnsAsync(asset1);
			api.Setup(x => x.FindSymbolByISIN("Ethereum", It.IsAny<Func<IEnumerable<Asset>, Asset>>())).ReturnsAsync(asset2);
			api.Setup(x => x.FindSymbolByISIN("Cosmos", It.IsAny<Func<IEnumerable<Asset>, Asset>>())).ReturnsAsync(asset3);

			// Act
			var orders = await parser.ConvertToOrders(account.Name, new[] { "./FileImporter/TestFiles/Coinbase/Example2/Example2.csv" });

			// Assert
			orders.Should().BeEquivalentTo(new[]
			{
			new Order {
				AccountId = account.Id,
				Asset = asset1, // BTC
				Comment = "Transaction Reference: [SELL_BTC_638280698190000000]",
				Currency = asset1.Currency,
				FeeCurrency = asset1.Currency,
				Date = new DateTime(2023,08,19,17,23,39, DateTimeKind.Utc),
				Fee = 0,
				Quantity = 0.00205323M,
				Type = OrderType.SELL,
				UnitPrice = 24073.28M,
				ReferenceCode = "SELL_BTC_638280698190000000"
			},
			new Order {
				AccountId = account.Id,
				Asset = asset2, // ETH
				Comment = "Transaction Reference: [BUY_ETH_638175675400000000]",
				Currency = asset2.Currency,
				FeeCurrency = asset2.Currency,
				Date = new DateTime(2023,04,20,4,5,40, DateTimeKind.Utc),
				Fee = 0.990000M,
				Quantity = 0.00213232M,
				Type = OrderType.BUY,
				UnitPrice =1810.23M,
				ReferenceCode = "BUY_ETH_638175675400000000"
			},
			new Order {
				AccountId = account.Id,
				Asset = asset2, // ETH
				Comment = "Transaction Reference: [BUY_ETH_638177486840000000]",
				Currency = asset2.Currency,
				FeeCurrency = asset2.Currency,
				Date = new DateTime(2023,4,22,6,24,44, DateTimeKind.Utc),
				Fee = 0,
				Quantity = 1.0e-08M,
				Type = OrderType.BUY,
				UnitPrice =1689.10M,
				ReferenceCode = "BUY_ETH_638177486840000000"
			},
			new Order {
				AccountId = account.Id,
				Asset = asset2, // ETH -> ATOM
				Comment = "Transaction Reference: [SELL_ETH_638181207820000000]",
				Currency = asset2.Currency,
				FeeCurrency = asset2.Currency,
				Date = new DateTime(2023,04,26,13,46,22, DateTimeKind.Utc),
				Fee = 0.020000M,
				Quantity = 0.00052203M,
				Type = OrderType.SELL,
				UnitPrice = 1762.35M,
				ReferenceCode = "SELL_ETH_638181207820000000"
			},
			new Order {
				AccountId = account.Id,
				Asset = asset3, // ETH -> ATOM
				Comment = "Transaction Reference: [BUY_ATOM_638181207820000000]",
				Currency = asset3.Currency,
				FeeCurrency = asset3.Currency,
				Date = new DateTime(2023,04,26,13,46,22, DateTimeKind.Utc),
				Fee = 0,
				Quantity = 0.087842M,
				Type = OrderType.BUY,
				UnitPrice = 10.473344988729764804990778898M,
				ReferenceCode = "BUY_ATOM_638181207820000000"
			}});
		}
	}
}