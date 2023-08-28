using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.Nexo;
using GhostfolioSidekick.Ghostfolio.API;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.Nexo
{
	public class NexoParserTests
	{
		readonly Mock<IGhostfolioAPI> api;

		public NexoParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanConvertOrders_TestFileSingleOrder_True()
		{
			// Arrange
			var parser = new NexoParser(api.Object);

			// Act
			var canParse = await parser.CanConvertOrders(new[] { "./FileImporter/TestFiles/Nexo/Example1/Example1.csv" });

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task ConvertToOrders_TestFileMultipleOrders_Converted()
		{
			// Arrange
			var parser = new NexoParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Asset>().With(x => x.Currency, "USD").Create();
			var asset2 = fixture.Build<Asset>().With(x => x.Currency, "USD").Create();

			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("USD Coin", It.IsAny<Func<IEnumerable<Asset>, Asset>>())).ReturnsAsync(asset1);
			api.Setup(x => x.FindSymbolByISIN("Bitcoin", It.IsAny<Func<IEnumerable<Asset>, Asset>>())).ReturnsAsync(asset2);

			// Act
			var orders = await parser.ConvertToOrders(account.Name, new[] { "./FileImporter/TestFiles/Nexo/Example1/Example1.csv" });

			// Assert
			orders.Should().BeEquivalentTo(new[]
			{ new Order {
				AccountId = account.Id,
				Asset = asset1,
				Comment = "Transaction Reference: [NXTyPxhiopNL3]",
				Currency = asset1.Currency,
				FeeCurrency = null,
				Date = new DateTime(2023,8,25,14,44,46, DateTimeKind.Utc),
				Fee = 0,
				Quantity = 161.90485771M,
				Type = OrderType.BUY,
				UnitPrice = 0.999969996514813032906620872M,
				ReferenceCode = "NXTyPxhiopNL3"
			}, new Order {
				AccountId = account.Id,
				Asset = asset2,
				Comment = "Transaction Reference: [NXTyVJeCwg6Og]",
				Currency = asset2.Currency,
				FeeCurrency = null,
				Date = new DateTime(2023,8,26, 13,30,38, DateTimeKind.Utc),
				Fee = 0,
				Quantity = 0.00445142M,
				Type = OrderType.BUY,
				UnitPrice = 26028.386478921332967906870167M,
				ReferenceCode = "NXTyVJeCwg6Og"
			} });
		}
	}
}