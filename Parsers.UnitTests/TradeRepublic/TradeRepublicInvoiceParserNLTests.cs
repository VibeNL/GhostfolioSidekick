using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using GhostfolioSidekick.Parsers.TradeRepublic;

namespace GhostfolioSidekick.Parsers.UnitTests.TradeRepublic
{
	public class TradeRepublicInvoiceParserNLTests
	{
		private readonly Account account;
		private readonly TestHoldingsCollection holdingsAndAccountsCollection;

		public TradeRepublicInvoiceParserNLTests()
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
			var parser = new TradeRepublicInvoiceParserNL(new PdfToWordsParser());
			foreach (var file in Directory.GetFiles("./TestFiles/TradeRepublic/BuyOrders", "*.pdf", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuyBond_Converted()
		{
			// Arrange
			var parser = new TradeRepublicInvoiceParserNL(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/BuyOrders/single_buy_bond.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 10, 06, 15, 39, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("DE0001102333")],
						99m,
						0.9939m,
						new Money(Currency.EUR, 98.40m),
						""),
				 PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 10, 06, 15, 39, 0, DateTimeKind.Utc),
						1.12m,
						new Money(Currency.EUR, 1.12m),
						""),
				 PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 10, 06, 15, 39, 0, DateTimeKind.Utc),
						1m,
						new Money(Currency.EUR, 1m),
						""),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuyStock_Converted()
		{
			// Arrange
			var parser = new TradeRepublicInvoiceParserNL(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/BuyOrders/single_buy_stock.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 10, 06, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("US2546871060")],
						0.3247m,
						77.39m,
						new Money(Currency.EUR, 25.13m),
						"Trade_Republic_US2546871060_2023-10-06")
				]);
		}
	}
}
