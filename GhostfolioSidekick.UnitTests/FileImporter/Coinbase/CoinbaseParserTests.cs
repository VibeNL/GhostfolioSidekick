using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.FileImporter.Coinbase;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.Coinbase
{
	public class CoinbaseParserTests
	{
		readonly Mock<IGhostfolioAPI> api;
		private readonly Mock<IApplicationSettings> cs;

		public CoinbaseParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
			cs = new Mock<IApplicationSettings>();
			cs.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance());
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			var parser = new CoinbaseParser(cs.Object, api.Object);

			foreach (var file in Directory.GetFiles("./FileImporter/TestFiles/Coinbase/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(new[] { file });

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuy_Converted()
		{
			// Arrange
			var parser = new CoinbaseParser(cs.Object, api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.USD)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier(new string[] { "Ethereum", "ETH" }, It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Coinbase/BuyOrders/single_buy.csv" });

			// Assert
			account.Activities.Should().BeEquivalentTo(new[] { new Activity(
				ActivityType.Buy,
				asset,
				new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc),
				0.00213232M,
				new Money(DefaultCurrency.EUR, 1810.23M, new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc)),
				new[] { new Money(DefaultCurrency.EUR, 0.99M, new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc)) },
				"Transaction Reference: [Buy_2023-04-20] (Details: asset ETH)",
				"Buy_2023-04-20"
			)});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleConvert_Converted()
		{
			// Arrange
			var parser = new CoinbaseParser(cs.Object, api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var asset2 = fixture.Build<Model.SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.USD)).Create();

			api.Setup(x => x.GetMarketPrice(It.IsAny<SymbolProfile>(), It.IsAny<DateTime>())).ReturnsAsync(new Money("USD", 1.1M, new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc)));
			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier(new string[] { "Ethereum", "ETH" }, It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset);
			api.Setup(x => x.FindSymbolByIdentifier(new string[] { "USD Coin", "USDC" }, It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset2);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Coinbase/BuyOrders/single_convert.csv" });

			// Assert
			account.Activities.Should().BeEquivalentTo(new[] { new Activity(
				ActivityType.Sell,
				asset,
				new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc),
				0.00087766M,
				new Money(DefaultCurrency.EUR, 1709.09M, new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc)),
				new[] { new Money(DefaultCurrency.EUR, 0.040000M, new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc)) },
				"Transaction Reference: [Sell_2023-04-20] (Details: asset ETH)",
				"Sell_2023-04-20"
			),
			new Activity(
				ActivityType.Buy,
				asset2,
				new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc),
				1.629352M,
				new Money(DefaultCurrency.USD, 1.1M, new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc)),
				null,
				"Transaction Reference: [Buy_2023-04-20] (Details: asset USDC)",
				"Buy_2023-04-20"
			)});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSell_Converted()
		{
			// Arrange
			var parser = new CoinbaseParser(cs.Object, api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.USD)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier(new string[] { "USD Coin", "USDC" }, It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Coinbase/SellOrders/single_sell.csv" });

			// Assert
			account.Activities.Should().BeEquivalentTo(new[] { new Activity(
				ActivityType.Sell,
				asset,
				new DateTime(2023, 07, 14, 10, 40, 14, DateTimeKind.Utc),
				11.275271M,
				new Money(DefaultCurrency.EUR, 0.886900M, new DateTime(2023, 07, 14, 10, 40, 14, DateTimeKind.Utc)),
				new[] { new Money(DefaultCurrency.EUR, 0, new DateTime(2023, 07, 14, 10, 40, 14, DateTimeKind.Utc)) },
				"Transaction Reference: [Sell_2023-07-14] (Details: asset USDC)",
				"Sell_2023-07-14"
				)
			});
		}
	}
}