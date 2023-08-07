using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.DeGiro;
using GhostfolioSidekick.FileImporter.ScalableCaptial;
using GhostfolioSidekick.Ghostfolio.API;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.ScalableCapital
{
	public class ScalableCaptialParserTests
	{
		readonly Mock<IGhostfolioAPI> api;

		public ScalableCaptialParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanConvertOrders_WUMExample1_True()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);

			// Act
			var canParse = await parser.CanConvertOrders("./FileImporter/ScalableCapital/WUMExample1.csv");

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task CanConvertOrders_RKKExample1_True()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);

			// Act
			var canParse = await parser.CanConvertOrders("./FileImporter/ScalableCapital/RKKExample1.csv");

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task ConvertToOrders_Example1()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();
			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("IE00077FRP95")).ReturnsAsync(asset);

			// Act
			var orders = await parser.ConvertToOrders(account.Name, "./FileImporter/ScalableCapital/WUMExample1.csv");

			// Assert
			orders.Should().BeEquivalentTo(new[] { new Order {
				AccountId = account.Id,
				Asset = asset,
				Comment = "Transaction Reference: [SCALQbWiZnN9DtQ]",
				Currency = asset.Currency,
				Date = new DateTime(2023,8,3, 0,0,0, DateTimeKind.Utc),
				Fee = 0.99M,
				Quantity = 5,
				Type = OrderType.BUY,
				UnitPrice = 8.685M
			} });
		}
	}
}
