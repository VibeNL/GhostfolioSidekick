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
						new DateTime(2024, 08, 02, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("JP3756600007")],
						1m,
						47.81m,
						new Money(Currency.EUR, 47.81m),
						"Trade_Republic_JP3756600007_2024-08-02"),
				PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2024, 08, 02, 0, 0, 0, DateTimeKind.Utc),
						1m,
						new Money(Currency.EUR, 1m),
						"Trade_Republic_JP3756600007_2024-08-02")
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
						new DateTime(2024, 08, 05, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("IE00B4L5YC18")],
						0.356945m,
						34.078m,
						new Money(Currency.EUR, 12.16m),
						"Trade_Republic_IE00B4L5YC18_2024-08-05")
				]);
		}
	}
}
