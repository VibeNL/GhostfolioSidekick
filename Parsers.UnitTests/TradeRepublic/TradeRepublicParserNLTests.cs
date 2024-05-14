using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using GhostfolioSidekick.Parsers.TradeRepublic;

namespace GhostfolioSidekick.Parsers.UnitTests.TradeRepublic
{
	public class TradeRepublicParserNLTests
	{
		private readonly TestPdfToWords pdfToWords;
		readonly TradeRepublicParserNL parser;
		private readonly Account account;
		private readonly TestHoldingsCollection holdingsAndAccountsCollection;

		public TradeRepublicParserNLTests()
		{
			pdfToWords = new TestPdfToWords();
			parser = new TradeRepublicParserNL(pdfToWords);

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
			var parser = new TradeRepublicParserNL(new PdfToWordsParser());
			foreach (var file in Directory.GetFiles("./TestFiles/TradeRepublic/", "*.pdf", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleInterest_Converted()
		{
			// Arrange
			pdfToWords.Text = new Dictionary<int, string>
			{
				{ 0, single_interest }
			};

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/testfile1.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateInterest(
						Currency.EUR,
						new DateTime(2024, 05, 01, 0, 0, 0, DateTimeKind.Utc),
						33.31m,
						"Your interest payment",
						new Money(Currency.EUR, 33.31m),
						"Trade_Republic_Interest_2024-05-01")
				]);
		}

		private class TestPdfToWords : PdfToWordsParser
		{
			public Dictionary<int, string> Text { get; internal set; } = new Dictionary<int, string>();

			public override List<SingleWordToken> ParseTokens(string filePath)
			{
				var lst = new List<SingleWordToken>();
				foreach (var item in Text)
				{
					lst.AddRange(ParseWords(item.Value, item.Key));
				}

				return lst;
			}
		}

		private string single_interest = @"
Aangemaakt op 07 mei 2024
Pagina  1 3
Trade Republic Bank GmbH
DATUM 01 mei 2024 - 06 mei 2024
IBAN
BIC
REKENINGOVERZICHT SAMENVATTING
PRODUCT OPENINGSSALDO BEDRAG BIJ BEDRAF AF EINDSALDO
Effectenrekening € 10.437,71 € 442,95 € 446,94 € 10.433,72
MUTATIEOVERZICHT
DATUM TYPE BESCHRIJVING
BEDRAG 
BIJ
BEDRAF AF SALDO
01 mei 
2024
Rentebetaling Your interest payment € 33,31 € 10.471,02

";


	}
}