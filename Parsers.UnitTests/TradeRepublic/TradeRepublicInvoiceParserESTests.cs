using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using GhostfolioSidekick.Parsers.TradeRepublic;

namespace GhostfolioSidekick.Parsers.UnitTests.TradeRepublic
{
	public class TradeRepublicInvoiceParserESTests
	{
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		public TradeRepublicInvoiceParserESTests()
		{
			var fixture = new Fixture();
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
			var parser = new TradeRepublicInvoiceParserNL(new PdfToWordsParser());
			foreach (var file in Directory.GetFiles("./TestFiles/TradeRepublic/ES/CashTransactions", "*.pdf", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleDividend_Converted()
		{
			// Arrange
			var parser = new TradeRepublicInvoiceParserNL(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/ES/CashTransactions/single_dividend_stock.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateDividend(
						Currency.USD,
						new DateTime(2025, 03, 10, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US02079K3059")],
						4.54m,
						new Money(Currency.USD, 4.54m),
						"Trade_Republic_US02079K3059_2025-03-10")
				]);
		}
	}
}
