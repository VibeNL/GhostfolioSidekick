using AutoFixture;
using FluentAssertions;
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
				Date = new DateTime(2023,08,7, 19,56,2, DateTimeKind.Utc),
				Fee = 0.02M,
				Quantity = 0.0267001M,
				Type = OrderType.BUY,
				UnitPrice = 453.33M,
				ReferenceCode = "EOF3219953148"
			} });
		}

		[Fact]
		public async Task ConvertToOrders_TestFileMultipleOrdersUS_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Asset>().With(x => x.Currency, "USD").Create();
			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("US67066G1040")).ReturnsAsync(asset);

			// Act
			var orders = await parser.ConvertToOrders(account.Name, new[] { "./FileImporter/TestFiles/Trading212/Example2/TestFileMultipleOrdersUS.csv" });

			// Assert
			orders.Should().BeEquivalentTo(new[] {
			new Order {
				AccountId = account.Id,
				Asset = asset,
				Comment = "Transaction Reference: [EOF3219953148]",
				Currency = asset.Currency,
				FeeCurrency = "EUR",
				Date = new DateTime(2023,08,7, 19,56,2, DateTimeKind.Utc),
				Fee = 0.02M,
				Quantity = 0.0267001M,
				Type = OrderType.BUY,
				UnitPrice = 453.33M,
				ReferenceCode = "EOF3219953148"
			},
			new Order {
				AccountId = account.Id,
				Asset = asset,
				Comment = "Transaction Reference: [EOF3224031567]",
				Currency = asset.Currency,
				FeeCurrency = "",
				Date = new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc),
				Fee = 0,
				Quantity = 0.0026199M,
				Type = OrderType.BUY,
				UnitPrice = 423.25M,
				ReferenceCode = "EOF3224031567"
			}});
		}

		[Fact]
		public async Task ConvertToOrders_TestFileSingleOrderUK_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Asset>().With(x => x.Currency, "GBX").Create();
			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("GB0007188757")).ReturnsAsync(asset);

			// Act
			var orders = await parser.ConvertToOrders(account.Name, new[] { "./FileImporter/TestFiles/Trading212/Example3/TestFileSingleOrderUK.csv" });

			// Assert
			orders.Should().BeEquivalentTo(new[] { new Order {
				AccountId = account.Id,
				Asset = asset,
				Comment = "Transaction Reference: [EOF3224031549]",
				Currency = asset.Currency,
				FeeCurrency = "EUR",
				Date = new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc),
				Fee = 0.07M,
				Quantity = 0.18625698M,
				Type = OrderType.BUY,
				UnitPrice = 4947.00M,
				ReferenceCode = "EOF3224031549"
			} });
		}

		[Fact]
		public async Task ConvertToOrders_TestFileSingleDividend_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Asset>().With(x => x.Currency, "USD").Create();
			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("US0378331005")).ReturnsAsync(asset);

			// Act
			var orders = await parser.ConvertToOrders(account.Name, new[] { "./FileImporter/TestFiles/Trading212/Example4/TestFileSingleDividend.csv" });

			// Assert
			orders.Should().BeEquivalentTo(new[] { new Order {
				AccountId = account.Id,
				Asset = asset,
				Comment = "Transaction Reference: [DIVIDEND_US0378331005_2023-08-17]",
				Currency = asset.Currency,
				FeeCurrency = "",
				Date = new DateTime(2023,08,17, 10,49,49, DateTimeKind.Utc),
				Fee = 0M,
				Quantity = 0.1279177000M,
				Type = OrderType.DIVIDEND,
				UnitPrice =  0.20M,
				ReferenceCode = "DIVIDEND_US0378331005_2023-08-17"
			} });
		}

		[Fact]
		public async Task ConvertToOrders_TestFileSingleOrderUKNativeCurrency_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Asset>().With(x => x.Currency, "GBX").Create();
			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("GB0007188757")).ReturnsAsync(asset);

			// Act
			var orders = await parser.ConvertToOrders(account.Name, new[] { "./FileImporter/TestFiles/Trading212/Example5/TestFileSingleOrderUKNativeCurrency.csv" });

			// Assert
			orders.Should().BeEquivalentTo(new[] { new Order {
				AccountId = account.Id,
				Asset = asset,
				Comment = "Transaction Reference: [EOF3224031549]",
				Currency = asset.Currency,
				FeeCurrency = "GBP",
				Date = new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc),
				Fee = 0.05M,
				Quantity = 0.18625698M,
				Type = OrderType.BUY,
				UnitPrice = 4947.00M,
				ReferenceCode = "EOF3224031549"
			} });
		}
	}
}