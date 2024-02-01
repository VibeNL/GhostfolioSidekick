using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.Coinbase;
using GhostfolioSidekick.Parsers.UnitTests;

namespace GhostfolioSidekick.Parsers.UnitTests.Coinbase
{
	public class CoinbaseParserTests
	{
		private CoinbaseParser parser;
		private Account account;
		private TestHoldingsCollection holdingsAndAccountsCollection;

		public CoinbaseParserTests()
		{
			parser = new CoinbaseParser();

			var fixture = new Fixture();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, new Balance(new Money(Currency.EUR, 0)))
				.Create();
			holdingsAndAccountsCollection = new TestHoldingsCollection(account);
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/Coinbase/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuy_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Coinbase/BuyOrders/single_buy.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("ETH")],
						0.00213232M,
						1810.23M,
						"Buy_ETH_2023-04-20 04:05:40:+00:00"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc),
						0.99M,
						"Buy_ETH_2023-04-20 04:05:40:+00:00"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleConvert_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Coinbase/BuyOrders/single_convert.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			var a = PartialActivity.CreateAssetConvert(
						new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("ETH")],
						0.00087766M,
						[PartialSymbolIdentifier.CreateCrypto("USDC")],
						1.629352M,
						"Convert_ETH_2023-04-20 04:05:40:+00:00").ToArray();
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					a[0],
					a[1],
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc),
						0.040000M,
						"Convert_ETH_2023-04-20 04:05:40:+00:00"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSell_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Coinbase/SellOrders/single_sell.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSell(
						Currency.EUR,
						new DateTime(2023, 07, 14, 10, 40, 14, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("USDC")],
						11.275271M,
						0.886900M,
						"Sell_USDC_2023-07-14 10:40:14:+00:00")
				]);
		}
	}
}