using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using GhostfolioSidekick.Parsers.TradeRepublic;

namespace GhostfolioSidekick.Parsers.UnitTests.TradeRepublic
{
	public class TradeRepublicStatementParserNLTests
	{
		private readonly Account account;
		private readonly TestHoldingsCollection holdingsAndAccountsCollection;

		public TradeRepublicStatementParserNLTests()
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
			var parser = new TradeRepublicStatementParserNL(new PdfToWordsParser());
			foreach (var file in Directory.GetFiles("./TestFiles/TradeRepublic/", "*_statement.pdf", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFile1_True()
		{
			// Arrange, use the real parser to test the real files
			var parser = new TradeRepublicStatementParserNL(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/montly_statement.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().HaveCount(18);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleInterest_Converted()
		{
			// Arrange
			var parser = new TradeRepublicStatementParserNL(new TestPdfToWords(new Dictionary<int, string>
			{
				{ 0, single_interest }
			}));

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
			var parser = new TradeRepublicStatementParserNL(new TestPdfToWords(new Dictionary<int, string>
			{
				{ 0, single_deposit }
			}));

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
			var parser = new TradeRepublicStatementParserNL(new TestPdfToWords(new Dictionary<int, string>
			{
				{ 0, single_withdrawal }
			}));

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

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleDividend_Converted()
		{
			// Arrange
			var parser = new TradeRepublicStatementParserNL(new TestPdfToWords(new Dictionary<int, string>
			{
				{ 0, single_dividend }
			}));

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/testfile1.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEmpty();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleGift_Converted()
		{
			// Arrange
			var parser = new TradeRepublicStatementParserNL(new TestPdfToWords(new Dictionary<int, string>
			{
				{ 0, single_gift }
			}));

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/testfile1.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateGift(
						Currency.EUR,
						new DateTime(2023, 10, 06, 0, 0, 0, DateTimeKind.Utc),
						25.13m,
						new Money(Currency.EUR, 25.13m),
						"Trade_Republic_Verwijzing_2023-10-06")
				]);
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

		private string single_dividend = @"
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
10 jan. 
2024
Inkomsten Gebeurtenisuitvoering Inkomsten US2546871060 DISNEY (WALT) CO. 6912966220240110 € 0,07 € 8.883,88
  ";

		private string single_gift = @"
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
06 okt. 
2023
Verwijzing Restitutie voor je geschenk € 25,13 € 174,13
";
	}
}