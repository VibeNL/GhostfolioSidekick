using AutoFixture;
using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using GhostfolioSidekick.Parsers.TradeRepublic;
using GhostfolioSidekick.Parsers.TradeRepublic.EN;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.Parsers.UnitTests.TradeRepublic
{
	public class TradeRepublicInvoiceParserENTests
	{
		private readonly Account account;
		private readonly TestActivityManager activityManager;
		private readonly List<ITradeRepublicActivityParser> SubParsers = [
			new EnglishStockInvoiceParser(),
	   new EnglishSavingPlanInvoiceParser(),
	   new EnglishBondInvoiceParser(),
	   new EnglishDividendInvoiceParser(),
	   new EnglishInterestPaymentInvoiceParser(),
	   new EnglishBondRepaymentInvoiceParser(),
	   new EnglishAccountStatementParser(Mock.Of<ILogger<EnglishAccountStatementParser>>())
		];
		private readonly ITestOutputHelper output;

		public TradeRepublicInvoiceParserENTests(ITestOutputHelper output)
		{
			var fixture = CustomFixture.New();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, [new Balance(DateOnly.FromDateTime(DateTime.Today), new Money(Currency.EUR, 0))])
				.Create();
			activityManager = new TestActivityManager();
			this.output = output;
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange, use the real parser to test the real files
			var parser = new TradeRepublicParser(new PdfToWordsParser(), SubParsers);
			foreach (var file in Directory.GetFiles("./TestFiles/TradeRepublic/EN/BuyOrders", "*.pdf", SearchOption.AllDirectories)
						  .Union(Directory.GetFiles("./TestFiles/TradeRepublic/EN/CashTransactions", "*.pdf", SearchOption.AllDirectories)))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuyBond_Converted()
		{
			// Arrange
			var parser = new TradeRepublicParser(new PdfToWordsParser(), SubParsers);

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/EN/BuyOrders/single_buy_bond.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 10, 06, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("DE0001102333")],
						99m,
						new Money(Currency.EUR, 0.9939m),
						new Money(Currency.EUR, 98.40m),
						"Trade_Republic_single_buy_bond.pdf"),
				 PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 10, 06, 0, 0, 0, DateTimeKind.Utc),
						1.12m,
						new Money(Currency.EUR, 1.12m),
						"Trade_Republic_single_buy_bond.pdf"),
				 PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 10, 06, 0, 0, 0, DateTimeKind.Utc),
						1m,
						new Money(Currency.EUR, 1m),
						"Trade_Republic_single_buy_bond.pdf"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuyStock_Converted()
		{
			// Arrange
			var parser = new TradeRepublicParser(new PdfToWordsParser(), SubParsers);

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/EN/BuyOrders/single_buy_stock.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 10, 06, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US2546871060")],
						0.3247m,
						new Money(Currency.EUR, 77.39m),
						new Money(Currency.EUR, 25.13m),
						"Trade_Republic_single_buy_stock.pdf")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuySavingsplan_Converted()
		{
			// Arrange
			var parser = new TradeRepublicParser(new PdfToWordsParser(), SubParsers);

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/EN/BuyOrders/single_savingsplan_stock.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 12, 18, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US2546871060")],
						0.058377m,
						new Money(Currency.EUR, 85.65m),
						new Money(Currency.EUR, 5m),
						"Trade_Republic_single_savingsplan_stock.pdf")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleRoundUpInformation_Converted()
		{
			// Arrange
			var parser = new TradeRepublicParser(new PdfToWordsParser(), SubParsers);

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/EN/InformationOnly/single_round_up_information.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEmpty();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleDividend_Converted()
		{
			// Arrange
			var parser = new TradeRepublicParser(new PdfToWordsParser(), SubParsers);

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/EN/CashTransactions/single_dividend.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateDividend(
						Currency.USD,
						new DateTime(2024, 01, 09, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US2546871060")],
						0.1m,
						new Money(Currency.USD, 0.1m),
						"Trade_Republic_single_dividend.pdf"),
				 PartialActivity.CreateFee(
						Currency.USD,
						new DateTime(2024, 01, 09, 0, 0, 0, DateTimeKind.Utc),
						0.02m,
						new Money(Currency.USD, 0.02m),
						"Trade_Republic_single_dividend.pdf")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleInterestBond_Converted()
		{
			// Arrange
			var parser = new TradeRepublicParser(new PdfToWordsParser(), SubParsers);

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/EN/CashTransactions/single_interest_bond.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateDividend(
						Currency.EUR,
						new DateTime(2024, 02, 15, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("DE0001102333")],
						1.74m,
						new Money(Currency.EUR, 1.74m),
						"Trade_Republic_single_interest_bond.pdf")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleRepayBond_Converted()
		{
			// Arrange
			var parser = new TradeRepublicParser(new PdfToWordsParser(), SubParsers);

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/EN/CashTransactions/single_repay_bond.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBondRepay(
						Currency.EUR,
						new DateTime(2024, 02, 14, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("DE0001102333")],
						new Money(Currency.EUR, 99.47m),
						new Money(Currency.EUR, 99.47m),
						"Trade_Republic_single_repay_bond.pdf")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFilesBulk_Converted()
		{
			// Arrange
			var parser = new TradeRepublicParser(new PdfToWordsParser(), SubParsers);
			var files = Directory.GetFiles("./TestFiles/TradeRepublic/EN/BuyOrders", "*.pdf", SearchOption.AllDirectories)
				.Union(Directory.GetFiles("./TestFiles/TradeRepublic/EN/CashTransactions", "*.pdf", SearchOption.AllDirectories));

			// Act
			foreach (var file in files)
			{
				await parser.ParseActivities(file, activityManager, account.Name);
			}

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 10, 06, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("DE0001102333")],
						99m,
						new Money(Currency.EUR, 0.9939m),
						new Money(Currency.EUR, 98.40m),
						"Trade_Republic_single_buy_bond.pdf"),
				 PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 10, 06, 0, 0, 0, DateTimeKind.Utc),
						1.12m,
						new Money(Currency.EUR, 1.12m),
						"Trade_Republic_single_buy_bond.pdf"),
				 PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 10, 06, 0, 0, 0, DateTimeKind.Utc),
						1m,
						new Money(Currency.EUR, 1m),
						"Trade_Republic_single_buy_bond.pdf"),
				 PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 10, 06, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US2546871060")],
						0.3247m,
						new Money(Currency.EUR, 77.39m),
						new Money(Currency.EUR, 25.13m),
						"Trade_Republic_single_buy_stock.pdf"),
				 PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 12, 18, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US2546871060")],
						0.058377m,
						new Money(Currency.EUR, 85.65m),
						new Money(Currency.EUR, 5m),
						"Trade_Republic_single_savingsplan_stock.pdf"),
				 PartialActivity.CreateDividend(
						Currency.USD,
						new DateTime(2024, 01, 09, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US2546871060")],
						0.1m,
						new Money(Currency.USD, 0.1m),
						"Trade_Republic_single_dividend.pdf"),
				 PartialActivity.CreateFee(
						Currency.USD,
						new DateTime(2024, 01, 09, 0, 0, 0, DateTimeKind.Utc),
						0.02m,
						new Money(Currency.USD, 0.02m),
						"Trade_Republic_single_dividend.pdf"),
				 PartialActivity.CreateDividend(
						Currency.EUR,
						new DateTime(2024, 02, 15, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("DE0001102333")],
						1.74m,
						new Money(Currency.EUR, 1.74m),
						"Trade_Republic_single_interest_bond.pdf"),
				 PartialActivity.CreateBondRepay(
						Currency.EUR,
						new DateTime(2024, 02, 14, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("DE0001102333")],
						new Money(Currency.EUR, 99.47m),
						new Money(Currency.EUR, 99.47m),
						"Trade_Republic_single_repay_bond.pdf"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleAccountStatement_Converted()
		{
			// Arrange
			var parser = new TradeRepublicParser(new PdfToWordsParser(), SubParsers);

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/EN/Statements/account_statement.pdf", activityManager, account.Name);

			// Debug, log all activities to easily identify which ones are missing in case of a failed test
			foreach (var activity in activityManager.PartialActivities)
			{
				output.WriteLine(activity.ToString());
			}

			// Assert
			activityManager.PartialActivities.Should().HaveCount(24 * 2);
			activityManager.PartialActivities.Should().ContainEquivalentOf(
				PartialActivity.CreateGift(
						Currency.EUR,
						new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
						15m,
						new Money(Currency.EUR, 15m),
						"Trade_Republic_account_statement.pdf_20260101_TEWgmTKV73qgBYa5JCxtMLRCg90DrYBm6ySwU0fFZSM=")
			);
			activityManager.PartialActivities.Should().ContainEquivalentOf(
				PartialActivity.CreateInterest(
						Currency.EUR,
						new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
						14.61m,
						"Interest payment",
						new Money(Currency.EUR, 14.61m),
						"Trade_Republic_account_statement.pdf_20260101_/UBJ93awt7TW9mubFaZVde05b8PkqrAvnhN+pHri05Q=")
			);
			activityManager.PartialActivities.Should().ContainEquivalentOf(
				PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2026, 01, 02, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("IE000U9ODG19")],
						3.158958m,
						new Money(Currency.EUR, 7.9140020221857967089147750619M),
						new Money(Currency.EUR, 25m),
						"Trade_Republic_account_statement.pdf_20260102_ElyCuSZgMtkZNmFq0CfcIHw+GjPvAGCmMyl7qi66lO8=")
			);
			activityManager.PartialActivities.Should().ContainEquivalentOf(
				PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2026, 01, 02, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("LU1931974262")],
						1.494098m,
						new Money(Currency.EUR, 33.465006980800456194975162272M),
						new Money(Currency.EUR, 50m),
						"Trade_Republic_account_statement.pdf_20260102_i9NGrsLA9M4Yc8LTczbClBBBHO2r4YcxG1UDBAjicEA=")
			);
			activityManager.PartialActivities.Should().ContainEquivalentOf(
				PartialActivity.CreateSell(
						Currency.EUR,
						new DateTime(2026, 01, 02, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US2546871060")],
						1.640151m,
						new Money(Currency.EUR, 96.28991477004251437824931973m),
						new Money(Currency.EUR, 157.93m),
						"Trade_Republic_account_statement.pdf_20260102_fnY+eEtVd1YEmowbzWqh9fdtE8lq8PDMbpOAT/C//4g="
				)
			);
			// Dividend 2026-01-15 00:00:00:+00:00 0,90 EUR US2546871060 1 EUR 0,90 EUR Trade_Republic_account_statement.pdf_20260115_DwAFiDoZuh56qKc4jjsCuOculON8eQn28tJuWrsLXoA=
			activityManager.PartialActivities.Should().ContainEquivalentOf(
				PartialActivity.CreateDividend(
						Currency.EUR,
						new DateTime(2026, 01, 15, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("US2546871060")],
						0.90m,
						new Money(Currency.EUR, 0.9m),
						"Trade_Republic_account_statement.pdf_20260115_DwAFiDoZuh56qKc4jjsCuOculON8eQn28tJuWrsLXoA="
				)
			);
		}
	}
}
