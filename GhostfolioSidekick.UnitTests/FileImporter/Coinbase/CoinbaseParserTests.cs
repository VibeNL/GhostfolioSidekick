//using AutoFixture;
//using FluentAssertions;
//using GhostfolioSidekick.FileImporter.Coinbase;
//using GhostfolioSidekick.Ghostfolio.API;
//using Moq;

//namespace GhostfolioSidekick.UnitTests.FileImporter.Coinbase
//{
//	public class CoinbaseParserTests
//	{
//		readonly Mock<IGhostfolioAPI> api;

//		public CoinbaseParserTests()
//		{
//			api = new Mock<IGhostfolioAPI>();
//		}

//		[Fact]
//		public async Task CanParseActivities_Example1_True()
//		{
//			// Arrange
//			var parser = new CoinbaseParser(api.Object);

//			// Act
//			var canParse = await parser.CanParseActivities(new[] { "./FileImporter/TestFiles/Coinbase/Example1/Example1.csv" });

//			// Assert
//			canParse.Should().BeTrue();
//		}

//		[Fact]
//		public async Task ConvertActivitiesForAccount_Example1_Converted()
//		{
//			// Arrange
//			var parser = new CoinbaseParser(api.Object);
//			var fixture = new Fixture();

//			var asset1 = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.EUR).Create();

//			var account = fixture.Create<Model.Account>();

//			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
//			api.Setup(x => x.FindSymbolByISIN("BTC", It.IsAny<Func<IEnumerable<Model.Asset>, Model.Asset>>())).ReturnsAsync(asset1);

//			// Act
//			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Coinbase/Example1/Example1.csv" });

//			// Assert
//			account.Activities.Should().BeEquivalentTo(new[]
//			{
//				new Model.Activity {
//					Asset = asset1,
//					Comment = "Transaction Reference: [Sell_BTC_638280626190000000]",
//					Date = new DateTime(2023,08,19,17,23,39, DateTimeKind.Utc),
//					Fee = null,
//					Quantity = 0.00205323M,
//					ActivityType = Model.ActivityType.Sell,
//					UnitPrice = new Model.Money(asset1.Currency, 24073.28M, new DateTime(2023,08,19,17,23,39, DateTimeKind.Utc)),
//					ReferenceCode = "Sell_BTC_638280626190000000"
//			}});
//		}

//		[Fact]
//		public async Task ConvertActivitiesForAccount_Example2_Converted()
//		{
//			// Arrange
//			var parser = new CoinbaseParser(api.Object);
//			var fixture = new Fixture();

//			var asset1 = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.EUR).Create();
//			var asset2 = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.EUR).Create();
//			var asset3 = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.EUR).Create();

//			var account = fixture.Create<Model.Account>();

//			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
//			api.Setup(x => x.FindSymbolByISIN("BTC", It.IsAny<Func<IEnumerable<Model.Asset>, Model.Asset>>())).ReturnsAsync(asset1);
//			api.Setup(x => x.FindSymbolByISIN("ETH", It.IsAny<Func<IEnumerable<Model.Asset>, Model.Asset>>())).ReturnsAsync(asset2);
//			api.Setup(x => x.FindSymbolByISIN("ATOM", It.IsAny<Func<IEnumerable<Model.Asset>, Model.Asset>>())).ReturnsAsync(asset3);

//			// Act
//			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Coinbase/Example2/Example2.csv" });

//			// Assert
//			account.Activities.Should().BeEquivalentTo(new[]
//			{
//				new Model.Activity {
//					Asset = asset1, // BTC
//					Comment = "Transaction Reference: [Send_BTC_638280626190000000]",
//					Date = new DateTime(2023,08,19,17,23,39, DateTimeKind.Utc),
//					Fee = null,
//					Quantity = 0.00205323M,
//					ActivityType = Model.ActivityType.Send,
//					UnitPrice = new Model.Money(asset1.Currency, 24073.28M, new DateTime(2023,08,19,17,23,39, DateTimeKind.Utc)),
//					ReferenceCode = "Send_BTC_638280626190000000"
//				},
//				new Model.Activity {
//					Asset = asset2, // ETH
//					Comment = "Transaction Reference: [Buy_ETH_638175603400000000]",
//					Date = new DateTime(2023,04,20,4,5,40, DateTimeKind.Utc),
//					Fee = new Model.Money(asset2.Currency,0.990000M, new DateTime(2023,04,20,4,5,40, DateTimeKind.Utc)),
//					Quantity = 0.00213232M,
//					ActivityType = Model.ActivityType.Buy,
//					UnitPrice =new Model.Money(asset2.Currency,1810.23M, new DateTime(2023,04,20,4,5,40, DateTimeKind.Utc)),
//					ReferenceCode = "Buy_ETH_638175603400000000"
//				},
//				new Model.Activity {
//					Asset = asset2, // ETH
//					Comment = "Transaction Reference: [Receive_ETH_638177414840000000]",
//					Date = new DateTime(2023,4,22,6,24,44, DateTimeKind.Utc),
//					Fee = null,
//					Quantity = 1.0e-08M,
//					ActivityType = Model.ActivityType.Receive,
//					UnitPrice = new Model.Money(asset2.Currency,1689.10M, new DateTime(2023,4,22,6,24,44, DateTimeKind.Utc)),
//					ReferenceCode = "Receive_ETH_638177414840000000"
//				},
//				new Model.Activity {
//					Asset = asset2, // ETH -> ATOM
//					Comment = "Transaction Reference: [Sell_ETH_638181135820000000]",
//					Date = new DateTime(2023,04,26,13,46,22, DateTimeKind.Utc),
//					Fee = new Model.Money(asset2.Currency,0.020000M, new DateTime(2023,04,26,13,46,22, DateTimeKind.Utc)),
//					Quantity = 0.00052203M,
//					ActivityType = Model.ActivityType.Sell,
//					UnitPrice = new Model.Money(asset2.Currency,1762.35M, new DateTime(2023,04,26,13,46,22, DateTimeKind.Utc)),
//					ReferenceCode = "Sell_ETH_638181135820000000"
//				},
//				new Model.Activity {
//					Asset = asset3, // ETH -> ATOM
//					Comment = "Transaction Reference: [Buy_ATOM_638181135820000000]",
//					Date = new DateTime(2023,04,26,13,46,22, DateTimeKind.Utc),
//					Fee = null,
//					Quantity = 0.087842M,
//					ActivityType = Model.ActivityType.Buy,
//					UnitPrice = new Model.Money(asset3.Currency,10.473344988729764804990778898M, new DateTime(2023,04,26,13,46,22, DateTimeKind.Utc)),
//					ReferenceCode = "Buy_ATOM_638181135820000000"
//				}});
//		}
//	}
//}