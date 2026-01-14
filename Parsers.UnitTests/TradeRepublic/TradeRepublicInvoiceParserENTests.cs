using AutoFixture;
using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using GhostfolioSidekick.Parsers.TradeRepublic;
using GhostfolioSidekick.Parsers.TradeRepublic.EN;

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
			new EnglishBondRepaymentInvoiceParser()
			];

		public TradeRepublicInvoiceParserENTests()
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
	}
}
