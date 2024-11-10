﻿using AutoFixture;
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
			foreach (var file in Directory.GetFiles("./TestFiles/TradeRepublic/DE", "*.pdf", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		// BuyOrders
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
						new DateTime(2024, 08, 01, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US67066G1040")],
						1m,
						101.50m,
						new Money(Currency.EUR, 101.50m),
						"Trade_Republic_US67066G1040_2024-08-01"),
				PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2024, 08, 01, 0, 0, 0, DateTimeKind.Utc),
						1m,
						new Money(Currency.EUR, 1m),
						"Trade_Republic_US67066G1040_2024-08-01")
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
						new DateTime(2024, 08, 01, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US0079031078")],
						0.410846m,
						121.70m,
						new Money(Currency.EUR, 50.00m),
						"Trade_Republic_US0079031078_2024-08-01"),
				PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2024, 08, 01, 0, 0, 0, DateTimeKind.Utc),
						1m,
						new Money(Currency.EUR, 1m),
						"Trade_Republic_US0079031078_2024-08-01")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuySavingsplan_Converted()
		{
			// Arrange
			var parser = new TradeRepublicInvoiceParserDE(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/DE/BuyOrders/single_buy_savingsplan_etf.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2024, 09, 02, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("IE00B52VJ196")],
						0.694251m,
						72.02m,
						new Money(Currency.EUR, 50.00m),
						"Trade_Republic_IE00B52VJ196_2024-09-02")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleLimitBuyStock_Converted()
		{
			// Arrange
			var parser = new TradeRepublicInvoiceParserDE(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/DE/BuyOrders/single_limit_buy_stock.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2024, 08, 02, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("JP3756600007")],
						1m,
						48.95m,
						new Money(Currency.EUR, 48.95m),
						"Trade_Republic_JP3756600007_2024-08-02"),
				PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2024, 08, 02, 0, 0, 0, DateTimeKind.Utc),
						1m,
						new Money(Currency.EUR, 1m),
						"Trade_Republic_JP3756600007_2024-08-02")
				]);
		}

		// SellOrders
		// TODO sell-orders don't seem to be implemented in the parser yet
		/*[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleSellStock_Converted()
		{
			// Arrange
			var parser = new TradeRepublicInvoiceParserDE(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/DE/SellOrders/single_sell_stock.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateSell(
						Currency.EUR,
						new DateTime(2024, 08, 13, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US0079031078")],
						1m,
						127.88m,
						new Money(Currency.EUR, 127.88m),
						"Trade_Republic_US0079031078_2024-08-13"),
				PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2024, 08, 13, 0, 0, 0, DateTimeKind.Utc),
						1m,
						new Money(Currency.EUR, 1m),
						"Trade_Republic_US0079031078_2024-08-13")
				]);
		}*/

		// CashTransactions
		// TODO the dividend document in the german TR strangely uses dots (.) for the amounts
		// whereas all other documents use commas...
		// also my example contains a currency exchange, leading to the wrong value being read
		/*[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleDividend_Converted()
		{
			// Arrange
			var parser = new TradeRepublicInvoiceParserDE(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/TradeRepublic/DE/CashTransactions/single_dividend_stock.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateDividend(
						Currency.USD,
						new DateTime(2024, 10, 03, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockBondAndETF("US67066G1040")],
						0.02m,
						new Money(Currency.USD, 0.2m),
						"Trade_Republic_US67066G1040_2024-10-03"),
				 PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2024, 10, 03, 0, 0, 0, DateTimeKind.Utc),
						0.01m,
						new Money(Currency.EUR, 0.01m),
						"Trade_Republic_US67066G1040_2024-10-03")
				]);
		}*/
	}
}
