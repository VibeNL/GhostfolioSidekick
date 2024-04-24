using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.CentraalBeheer;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using GhostfolioSidekick.Parsers.TradeRepublic;
using Moq;
using static System.Net.Mime.MediaTypeNames;

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
			parser = new TradeRepublicParserNL(new PdfToWordsParser());

			var fixture = new Fixture();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, new Balance(new Money(Currency.EUR, 0)))
				.Create();
			holdingsAndAccountsCollection = new TestHoldingsCollection(account);
		}

		[Fact(Skip = "Not Yet Implemented")]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/TradeRepublic/", "*.pdf", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact(Skip = "Not Yet Implemented")]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuy_Converted()
		{
			// Arrange
			pdfToWords.Text = new Dictionary<int, string>
			{
				{ 0, single_buy }
			};

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/multi-month-statement-nl.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.Holdings.Should().NotBeEmpty();
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

		private string single_buy = @"
		                ***** **** *****                                                                                               DATUM              01 okt. 2023 - 31 mrt. 2024
                ***** **** *****                                                                                                IBAN              ********
                ***** **** *****                                                                                                    BIC                                    ********



                REKENINGOVERZICHT SAMENVATTING



                PRODUCT                                      OPENINGSSALDO                     BEDRAG BIJ                     BEDRAF AF                                      EINDSALDO

                Effectenrekening                             € 0,00                            € 11.692,04                    € 1.847,12                                  € 9.844,92



                MUTATIEOVERZICHT



                DATUM       TYPE            BESCHRIJVING                                                                                          BEDRAG BIJ  BEDRAF AF         SALDO

                06 okt.                     Uitvoering Handel Directe aankoop Aankoop DE0001102333 1.75% BUNDANL.V.14/24 
                            Handel                                                                                                                            € 0,48        € 249,52
                2023                        1232793620231006
		";

		private string single_cashdeposit = @"
		                ***** **** *****                                                                                               DATUM              01 okt. 2023 - 31 mrt. 2024
                ***** **** *****                                                                                                IBAN              ********
                ***** **** *****                                                                                                    BIC                                    ********



                REKENINGOVERZICHT SAMENVATTING



                PRODUCT                                      OPENINGSSALDO                     BEDRAG BIJ                     BEDRAF AF                                      EINDSALDO

                Effectenrekening                             € 0,00                            € 11.692,04                    € 1.847,12                                  € 9.844,92



                MUTATIEOVERZICHT



                DATUM       TYPE            BESCHRIJVING                                                                                          BEDRAG BIJ  BEDRAF AF         SALDO

                06 okt. 
                            OverschrijvingCustomer ************************************ inpayed net: 250.000000 fee: 0.000000  € 250,00                                     € 250,00
                2023

                06 okt.                     
		";
	}
}