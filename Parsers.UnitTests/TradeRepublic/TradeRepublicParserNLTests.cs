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
						"Trade_Republic_Rentebetaling_2024-05-01")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleDeposit_Converted()
		{
			// Arrange
			pdfToWords.Text = new Dictionary<int, string>
			{
				{ 0, single_deposit }
			};

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/testfile1.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateCashDeposit(
						Currency.EUR,
						new DateTime(2024, 05, 02, 0, 0, 0, DateTimeKind.Utc),
						9.67m,
						new Money(Currency.EUR, 9.67m),
						"Trade_Republic_Overschrijving_2024-05-02")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleWithdrawal_Converted()
		{
			// Arrange
			pdfToWords.Text = new Dictionary<int, string>
			{
				{ 0, single_withdrawal }
			};

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/testfile1.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateCashWithdrawal(
						Currency.EUR,
						new DateTime(2024, 05, 05, 0, 0, 0, DateTimeKind.Utc),
						13.98m,
						new Money(Currency.EUR, 13.98m),
						"Trade_Republic_Kaarttransactie_2024-05-05")
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

		private string single_deposit = @"
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
02 mei 
2024
Overschrijving Storting geaccepteerd:  naar € 9,67 € 10.480,69
  ";

		private string single_withdrawal = @"
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
05 mei 
2024
Kaarttransactie € 13,98 € 10.322,24
  ";


		private string multi_statement = @"
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
02 mei 
2024
Overschrijving Storting geaccepteerd:  naar € 9,67 € 10.480,69
02 mei 
2024
Kaarttransactie € 9,67 € 10.471,02
02 mei 
2024
Beloning Your Saveback payment € 4,31 € 10.475,33
02 mei 
2024
Overschrijving Storting geaccepteerd:  naar € 11,19 € 10.486,52
02 mei 
2024
Handel
Uitvoering Handel Directe aankoop Aankoop IE0032895942 IS DL CORP BD U.ETF DLD 
5586292820240502 KW
€ 4,31 € 10.482,21
02 mei 
2024
Handel
Uitvoering Handel Directe aankoop Aankoop IE0032895942 IS DL CORP BD U.ETF DLD 
2364262820240502 KW
€ 50,00 € 10.432,21
03 mei 
2024
Kaarttransactie € 11,19 € 10.421,02
03 mei 
2024
Overschrijving PayOut to transit € 243,00 € 10.178,02
03 mei 
2024
Overschrijving Storting geaccepteerd:  naar € 243,22 € 10.421,24
03 mei 
2024
Overschrijving Storting geaccepteerd:  naar € 15,35 € 10.436,59
04 mei 
2024
Kaarttransactie € 15,35 € 10.421,24
05 mei 
2024
Kaarttransactie € 85,02 € 10.336,22
05 mei 
2024
Kaarttransactie € 13,98 € 10.322,24
  ";


	}
}