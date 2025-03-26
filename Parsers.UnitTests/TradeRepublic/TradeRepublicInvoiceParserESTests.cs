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
			var parser = new TradeRepublicInvoiceParserES(new PdfToWordsParser());
			foreach (var file in Directory.GetFiles("./TestFiles/TradeRepublic/ES/BuyOrders", "*.pdf", SearchOption.AllDirectories)
						  .Union(Directory.GetFiles("./TestFiles/TradeRepublic/ES/CashTransactions", "*.pdf", SearchOption.AllDirectories)))
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
			var parser = new TradeRepublicInvoiceParserES(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/ES/BuyOrders/single_buy_bond.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2024, 06, 03, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("DE0001104909")],
						99m,
						0.99345m,
						new Money(Currency.EUR, 98.35m),
						"Trade_Republic_DE0001104909_2024-06-03"),
				 PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2024, 06, 03, 0, 0, 0, DateTimeKind.Utc),
						1,
						new Money(Currency.EUR, 1),
						"Trade_Republic_DE0001104909_2024-06-03"),
				 PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2024, 06, 03, 0, 0, 0, DateTimeKind.Utc),
						1.05m,
						new Money(Currency.EUR, 1.05m),
						"Trade_Republic_DE0001104909_2024-06-03"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuyStock_Converted()
		{
			// Arrange
			var parser = new TradeRepublicInvoiceParserES(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/ES/BuyOrders/single_buy_stock.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2025, 01, 30, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US4581401001")],
						133,
						18.702m,
						new Money(Currency.EUR, 2487.37m),
						"Trade_Republic_US4581401001_2025-01-30"),
				PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2025, 01, 30, 0, 0, 0, DateTimeKind.Utc),
						1m,
						new Money(Currency.EUR, 1m),
						"Trade_Republic_US4581401001_2025-01-30"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuySavingsplan_Converted()
		{
			// Arrange
			var parser = new TradeRepublicInvoiceParserES(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/ES/BuyOrders/single_buy_savingsplan_etf.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2025, 02, 03, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("IE00B5BMR087")],
						0.081275m,
						615.19m,
						new Money(Currency.EUR, 50m),
						"Trade_Republic_IE00B5BMR087_2025-02-03")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleDividend_Converted()
		{
			// Arrange
			var parser = new TradeRepublicInvoiceParserES(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/ES/CashTransactions/single_dividend_stock.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateDividend(
						Currency.USD,
						new DateTime(2025, 03, 17, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US02079K3059")],
						4.54m,
						new Money(Currency.USD, 4.54m),
						"Trade_Republic_US02079K3059_2025-03-17")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleInterestBond_Converted()
		{
			// Arrange
			var parser = new TradeRepublicInvoiceParserES(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/ES/CashTransactions/single_interest_bond.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateDividend(
						Currency.EUR,
						new DateTime(2024, 12, 12, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("DE0001104909")],
						2.19m,
						new Money(Currency.EUR, 2.19m),
						"Trade_Republic_DE0001104909_2024-12-12")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleRepayBond_Converted()
		{
			// Arrange
			var parser = new TradeRepublicInvoiceParserES(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/ES/CashTransactions/single_repayment_bond.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBondRepay(
						Currency.EUR,
						new DateTime(2024, 02, 14, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("DE0001104909")],
						99.6m,
						new Money(Currency.EUR, 99.6m),
						"Trade_Republic_DE0001104909_2024-12-12")
				]);
		}
	}
}
