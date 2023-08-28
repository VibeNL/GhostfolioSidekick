using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.DeGiro;
using GhostfolioSidekick.Ghostfolio.API;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.DeGiro
{
	public class DeGiroParserTests
	{
		readonly Mock<IGhostfolioAPI> api;

		public DeGiroParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanConvertOrders_TestFileSingleOrder_True()
		{
			// Arrange
			var parser = new DeGiroParser(api.Object);

			// Act
			var canParse = await parser.CanConvertOrders(new[] { "./FileImporter/TestFiles/DeGiro/Example1/TestFileSingleOrder.csv" });

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task CanConvertOrders_TestFileMissingField_False()
		{
			// Arrange
			var parser = new DeGiroParser(api.Object);

			// Act
			var canParse = await parser.CanConvertOrders(new[] { "./FileImporter/TestFiles/DeGiro/Example2/TestFileMissingField.csv" });

			// Assert
			canParse.Should().BeFalse();
		}

		[Fact]
		public async Task ConvertToOrders_TestFileSingleOrder_Converted()
		{
			// Arrange
			var parser = new DeGiroParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();
			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("IE00B3XXRP09", null)).ReturnsAsync(asset);

			// Act
			var orders = await parser.ConvertToOrders(account.Name, new[] { "./FileImporter/TestFiles/DeGiro/Example1/TestFileSingleOrder.csv" });

			// Assert
			orders.Should().BeEquivalentTo(new[] { new Order {
				AccountId = account.Id,
				Asset = asset,
				Comment = "Transaction Reference: [b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5b]",
				Currency = asset.Currency,
				FeeCurrency = asset.Currency,
				Date = new DateTime(2023,07,6, 9, 39,0, DateTimeKind.Utc),
				Fee = 1,
				Quantity = 1,
				Type = OrderType.BUY,
				UnitPrice = 77.30M,
				ReferenceCode = "b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5b"
			} });
		}

		[Fact]
		public async Task ConvertToOrders_TestFileMuitpleOrders_Converted()
		{
			// Arrange
			var parser = new DeGiroParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();
			var asset2 = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();

			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("IE00B3XXRP09", null)).ReturnsAsync(asset1);
			api.Setup(x => x.FindSymbolByISIN("NL0009690239", null)).ReturnsAsync(asset2);

			// Act
			var orders = await parser.ConvertToOrders(account.Name, new[] { "./FileImporter/TestFiles/DeGiro/Example3/TestFileMultipleOrders.csv" });

			// Assert
			orders.Should().BeEquivalentTo(new[]
			{ new Order {
				AccountId = account.Id,
				Asset = asset1,
				Comment = "Transaction Reference: [b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5b]",
				Currency = asset1.Currency,
				FeeCurrency = asset1.Currency,
				Date = new DateTime(2023,07,6,9,39,0, DateTimeKind.Utc),
				Fee = 1,
				Quantity = 1,
				Type = OrderType.BUY,
				UnitPrice = 77.30M,
				ReferenceCode = "b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5b"
			}, new Order {
				AccountId = account.Id,
				Asset = asset2,
				Comment = "Transaction Reference: [67e39ca1-2f10-4f82-8365-1baad98c398f]",
				Currency = asset2.Currency,
				FeeCurrency = asset2.Currency,
				Date = new DateTime(2023,07,11, 9,33,0, DateTimeKind.Utc),
				Fee = 1,
				Quantity = 29,
				Type = OrderType.BUY,
				UnitPrice = 34.375M,
				ReferenceCode = "67e39ca1-2f10-4f82-8365-1baad98c398f"
			} });
		}
	}
}