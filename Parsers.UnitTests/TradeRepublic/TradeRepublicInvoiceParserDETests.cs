using AutoFixture;
using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using GhostfolioSidekick.Parsers.TradeRepublic;

namespace GhostfolioSidekick.Parsers.UnitTests.TradeRepublic
{
	public class TradeRepublicInvoiceParserDETests
	{
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		private readonly List<ITradeRepublicActivityParser> SubParsers = [
			];

		public TradeRepublicInvoiceParserDETests()
		{
			var fixture = CustomFixture.New();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, [new Balance(DateOnly.FromDateTime(DateTime.Today), new Money(Currency.EUR, 0))])
				.Create();
			activityManager = new TestActivityManager();
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange, use the real parser to test the real files
			var parser = new TradeRepublicParser(new PdfToWordsParser(), SubParsers);
			foreach (var file in Directory.GetFiles("./TestFiles/TradeRepublic/DE", "*.pdf", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		// BuyOrders
		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuyStockFull_Converted()
		{
			// Arrange
			var parser = new TradeRepublicParser(new PdfToWordsParser(), SubParsers);

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/DE/BuyOrders/single_buy_stock_full.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2024, 08, 01, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US67066G1040")],
						1m,
						new Money(Currency.EUR, 101.50m),
						new Money(Currency.EUR, 101.50m),
						"Trade_Republic_US67066G1040_2024-08-01"),
				PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2024, 08, 01, 0, 0, 0, DateTimeKind.Utc),
						1m,
						new Money(Currency.EUR, 1m),
						"Trade_Republic_US67066G1040_2024-08-01")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuyStockFraction_Converted()
		{
			// Arrange
			var parser = new TradeRepublicParser(new PdfToWordsParser(), SubParsers);

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/DE/BuyOrders/single_buy_stock_fraction.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2024, 08, 01, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US0079031078")],
						0.410846m,
						new Money(Currency.EUR, 121.70m),
						new Money(Currency.EUR, 50.00m),
						"Trade_Republic_US0079031078_2024-08-01"),
				PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2024, 08, 01, 0, 0, 0, DateTimeKind.Utc),
						1m,
						new Money(Currency.EUR, 1m),
						"Trade_Republic_US0079031078_2024-08-01")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuySavingsplan_Converted()
		{
			// Arrange
			var parser = new TradeRepublicParser(new PdfToWordsParser(), SubParsers);

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/DE/BuyOrders/single_buy_savingsplan_etf.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2024, 09, 02, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("IE00B52VJ196")],
						0.694251m,
						new Money(Currency.EUR, 72.02m),
						new Money(Currency.EUR, 50.00m),
						"Trade_Republic_IE00B52VJ196_2024-09-02")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleLimitBuyStock_Converted()
		{
			// Arrange
			var parser = new TradeRepublicParser(new PdfToWordsParser(), SubParsers);

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/DE/BuyOrders/single_limit_buy_stock.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2024, 08, 02, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("JP3756600007")],
						1m,
						new Money(Currency.EUR, 48.95m),
						new Money(Currency.EUR, 48.95m),
						"Trade_Republic_JP3756600007_2024-08-02"),
				PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2024, 08, 02, 0, 0, 0, DateTimeKind.Utc),
						1m,
						new Money(Currency.EUR, 1m),
						"Trade_Republic_JP3756600007_2024-08-02")
				]);
		}
	}
}
