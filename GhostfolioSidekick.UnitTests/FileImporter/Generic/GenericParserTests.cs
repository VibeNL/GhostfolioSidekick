using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.Generic;
using GhostfolioSidekick.Ghostfolio.API;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.Generic
{ 
	public class GenericParserTests
	{
		readonly Mock<IGhostfolioAPI> api;

		public GenericParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanConvertOrders_TestFileSingleOrder_True()
		{
			// Arrange
			var parser = new GenericParser(api.Object);

			// Act
			var canParse = await parser.CanConvertOrders(new[] { "./FileImporter/TestFiles/Generic/Example1/Example1.csv" });

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task ConvertToOrders_TestFileSingleOrder_Converted()
		{
			// Arrange
			var parser = new GenericParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Asset>().With(x => x.Currency, "USD").Create();
			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("US67066G1040", null)).ReturnsAsync(asset);

			// Act
			var orders = await parser.ConvertToOrders(account.Name, new[] { "./FileImporter/TestFiles/Generic/Example1/Example1.csv" });

			// Assert
			orders.Should().BeEquivalentTo(new[] { new Activity {
				AccountId = account.Id,
				Asset = asset,
				Comment = "Transaction Reference: [BUY_US67066G1040_2023-08-07]",
				Currency = asset.Currency,
				FeeCurrency = "USD",
				Date = new DateTime(2023,08,7, 0,0,0, DateTimeKind.Utc),
				Fee = 0.02M,
				Quantity = 0.0267001M,
				Type = ActivityType.BUY,
				UnitPrice = 453.33M,
				ReferenceCode = "BUY_US67066G1040_2023-08-07"
			} });
		}
	}
}