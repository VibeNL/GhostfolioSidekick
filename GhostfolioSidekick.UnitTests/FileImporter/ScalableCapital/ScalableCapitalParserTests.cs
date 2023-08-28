using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.ScalableCaptial;
using GhostfolioSidekick.Ghostfolio.API;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.ScalableCapital
{
	public class ScalableCapitalParserTests
	{
		readonly Mock<IGhostfolioAPI> api;

		public ScalableCapitalParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanConvertOrders_WUMExample1_True()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);

			// Act
			var canParse = await parser.CanConvertOrders(new[] { "./FileImporter/TestFiles/ScalableCapital/Example1/WUMExample1.csv" });

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task CanConvertOrders_RKKExample1_True()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);

			// Act
			var canParse = await parser.CanConvertOrders(new[] { "./FileImporter/TestFiles/ScalableCapital/Example1/RKKExample1.csv" });

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task ConvertToOrders_Example1_OrderOnly()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();
			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("IE00077FRP95", null)).ReturnsAsync(asset);

			// Act
			var orders = await parser.ConvertToOrders(account.Name, new[] { "./FileImporter/TestFiles/ScalableCapital/Example1/WUMExample1.csv" });

			// Assert
			orders.Should().BeEquivalentTo(new[] { new Order {
				AccountId = account.Id,
				Asset = asset,
				Comment = "Transaction Reference: [SCALQbWiZnN9DtQ]",
				Currency = asset.Currency,
				FeeCurrency = asset.Currency,
				Date = new DateTime(2023,8,3, 0,0,0, DateTimeKind.Utc),
				Fee = 0,
				Quantity = 5,
				Type = OrderType.BUY,
				UnitPrice = 8.685M,
				ReferenceCode = "SCALQbWiZnN9DtQ"
			} });
		}

		[Fact]
		public async Task ConvertToOrders_Example1_DividendOnly()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();
			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("US92343V1044", null)).ReturnsAsync(asset);

			// Act
			var orders = await parser.ConvertToOrders(account.Name, new[] { "./FileImporter/TestFiles/ScalableCapital/Example1/RKKExample1.csv" });

			// Assert
			orders.Should().BeEquivalentTo(new[] { new Order {
				AccountId = account.Id,
				Asset = asset,
				Comment = "Transaction Reference: [WWEK 16100100]",
				Currency = asset.Currency,
				FeeCurrency = asset.Currency,
				Date = new DateTime(2023,8,1, 0,0,0, DateTimeKind.Utc),
				Fee = 0,
				Quantity = 14,
				Type = OrderType.DIVIDEND,
				UnitPrice = 0.5057142857142857142857142857M,
				ReferenceCode = "WWEK 16100100"
			} });
		}

		[Fact]
		public async Task ConvertToOrders_Example1_Both()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();
			var asset2 = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();
			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("IE00077FRP95", null)).ReturnsAsync(asset1);
			api.Setup(x => x.FindSymbolByISIN("US92343V1044", null)).ReturnsAsync(asset2);

			// Act
			var orders = await parser.ConvertToOrders(account.Name, new[] {
				"./FileImporter/TestFiles/ScalableCapital/Example1/WUMExample1.csv",
				"./FileImporter/TestFiles/ScalableCapital/Example1/RKKExample1.csv" });

			// Assert
			orders.Should().BeEquivalentTo(new[] {
			new Order {
				AccountId = account.Id,
				Asset = asset1,
				Comment = "Transaction Reference: [SCALQbWiZnN9DtQ]",
				Currency = asset1.Currency,
				FeeCurrency = asset1.Currency,
				Date = new DateTime(2023,8,3, 0,0,0, DateTimeKind.Utc),
				Fee = 0.99M,
				Quantity = 5,
				Type = OrderType.BUY,
				UnitPrice = 8.685M,
				ReferenceCode = "SCALQbWiZnN9DtQ"
			},
			new Order {
				AccountId = account.Id,
				Asset = asset2,
				Comment = "Transaction Reference: [WWEK 16100100]",
				Currency = asset2.Currency,
				FeeCurrency = asset2.Currency,
				Date = new DateTime(2023,8,1, 0,0,0, DateTimeKind.Utc),
				Fee = 0,
				Quantity = 14,
				Type = OrderType.DIVIDEND,
				UnitPrice = 0.5057142857142857142857142857M,
				ReferenceCode = "WWEK 16100100"
			} });
		}

		[Fact]
		public async Task ConvertToOrders_Example2_NotDuplicateFeesAndDividend()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();
			var asset2 = fixture.Build<Asset>().With(x => x.Currency, "EUR").Create();
			var account = fixture.Create<Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("IE00077FRP95", null)).ReturnsAsync(asset1);
			api.Setup(x => x.FindSymbolByISIN("US92343V1044", null)).ReturnsAsync(asset2);

			// Act
			var orders = await parser.ConvertToOrders(account.Name, new[] {
				"./FileImporter/TestFiles/ScalableCapital/Example2/WUMExample1.csv",
				"./FileImporter/TestFiles/ScalableCapital/Example2/RKKExample1.csv",
				"./FileImporter/TestFiles/ScalableCapital/Example2/RKKExample2.csv" });

			// Assert
			orders.Should().BeEquivalentTo(new[] {
			new Order {
				AccountId = account.Id,
				Asset = asset1,
				Comment = "Transaction Reference: [SCALQbWiZnN9DtQ]",
				Currency = asset1.Currency,
				FeeCurrency = asset1.Currency,
				Date = new DateTime(2023,8,3, 0,0,0, DateTimeKind.Utc),
				Fee = 0.99M,
				Quantity = 5,
				Type = OrderType.BUY,
				UnitPrice = 8.685M,
				ReferenceCode = "SCALQbWiZnN9DtQ"
			},
			new Order {
				AccountId = account.Id,
				Asset = asset2,
				Comment = "Transaction Reference: [WWEK 16100100]",
				Currency = asset2.Currency,
				FeeCurrency = asset2.Currency,
				Date = new DateTime(2023,8,1, 0,0,0, DateTimeKind.Utc),
				Fee = 0,
				Quantity = 14,
				Type = OrderType.DIVIDEND,
				UnitPrice = 0.5057142857142857142857142857M,
				ReferenceCode = "WWEK 16100100"
			} });
		}
	}
}
