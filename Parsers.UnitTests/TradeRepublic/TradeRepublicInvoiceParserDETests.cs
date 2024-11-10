using AutoFixture;
using FluentAssertions;
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
		private readonly TestHoldingsCollection holdingsAndAccountsCollection;

		public TradeRepublicInvoiceParserDETests()
		{
			var fixture = new Fixture();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, new Balance(DateTime.Now, new Money(Currency.EUR, 0)))
				.Create();
			holdingsAndAccountsCollection = new TestHoldingsCollection(account);
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange, use the real parser to test the real files
			var parser = new TradeRepublicInvoiceParserDE(new PdfToWordsParser());
			foreach (var file in Directory.GetFiles("./TestFiles/TradeRepublic/DE/BuyOrders", "*.pdf", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuyStockFull_Converted()
		{
			// Arrange
			var parser = new TradeRepublicInvoiceParserDE(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/DE/BuyOrders/single_buy_stock_full.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2024, 08, 01, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US67066G1040")],
						1m,
						101.50m,
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
			var parser = new TradeRepublicInvoiceParserDE(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/DE/BuyOrders/single_buy_stock_fraction.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2024, 08, 01, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US0079031078")],
						0.410846m,
						121.70m,
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
	}
}
