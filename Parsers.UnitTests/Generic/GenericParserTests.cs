using AutoFixture;
using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.Generic;

namespace GhostfolioSidekick.Parsers.UnitTests.Generic
{
	public class GenericParserTests
	{
		private readonly GenericParser parser;
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		public GenericParserTests()
		{
			parser = new GenericParser(DummyCurrencyMapper.Instance);

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
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/Generic/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuy_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/BuyOrders/single_buy.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.USD,
						new DateTime(2023, 08, 7, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateGeneric("US67066G1040")],
						0.0267001000M,
						453.33M,
						new Money(Currency.USD,  12.103956333M),
						"Buy_US67066G1040_2023-08-07_0.0267001000_USD_0.02"),
					PartialActivity.CreateFee(
						Currency.USD,
						new DateTime(2023, 08, 7, 0, 0, 0, DateTimeKind.Utc),
						0.02M,
						new Money(Currency.USD, 0.02M),
						"Buy_US67066G1040_2023-08-07_0.0267001000_USD_0.02")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleSell_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/SellOrders/single_sell.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSell(
						Currency.USD,
						new DateTime(2023, 08, 7, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateGeneric("US67066G1040")],
						0.0267001000M,
						453.33M,
						new Money(Currency.USD, 12.103956333M),
						"Sell_US67066G1040_2023-08-07_0.0267001000_USD_0.02"),
					PartialActivity.CreateFee(
						Currency.USD,
						new DateTime(2023, 08, 7, 0, 0, 0, DateTimeKind.Utc),
						0.02M,
						new Money(Currency.USD, 0.02M),
						"Sell_US67066G1040_2023-08-07_0.0267001000_USD_0.02")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleReceive_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/Receive/single_receive.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateReceive(
						new DateTime(2023, 08, 7, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateGeneric("US67066G1040")],
						0.0267001000M,
						"Receive_US67066G1040_2023-08-07_0.0267001000_USD_0.02"),
					PartialActivity.CreateFee(
						Currency.USD,
						new DateTime(2023, 08, 7, 0, 0, 0, DateTimeKind.Utc),
						0.02M,
						new Money(Currency.USD, 0.02M),
						"Receive_US67066G1040_2023-08-07_0.0267001000_USD_0.02")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleSend_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/Send/single_send.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSend(
						new DateTime(2023, 08, 7, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateGeneric("US67066G1040")],
						0.0267001000M,
						"Send_US67066G1040_2023-08-07_0.0267001000_USD_0.02"),
					PartialActivity.CreateFee(
						Currency.USD,
						new DateTime(2023, 08, 7, 0, 0, 0, DateTimeKind.Utc),
						0.02M,
						new Money(Currency.USD, 0.02M),
						"Send_US67066G1040_2023-08-07_0.0267001000_USD_0.02")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleOrderTypeValuable_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/BuyOrders/single_valuable.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateValuable(
						Currency.EUR,
						new DateTime(2023, 08, 7, 0, 0, 0, DateTimeKind.Utc),
						"Giftcard",
						250M,
						new Money(Currency.EUR, 250M),
						"Valuable_Giftcard_2023-08-07_1_EUR_0"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleOrderTypeLiability_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/BuyOrders/single_liability.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateLiability(
						Currency.EUR,
						new DateTime(2023, 08, 7, 0, 0, 0, DateTimeKind.Utc),
						"Giftcard",
						250M,
						new Money(Currency.EUR, 250M),
						"Liability_Giftcard_2023-08-07_1_EUR_0"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/CashTransactions/single_deposit.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(
						Currency.USD,
						new DateTime(2023, 08, 6, 0, 0, 0, 0, DateTimeKind.Utc),
						1000,
						new Money(Currency.USD, 1000),
						"CashDeposit_EUR_2023-08-06_1_USD_0")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/CashTransactions/single_withdrawal.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashWithdrawal(
						Currency.USD,
						new DateTime(2023, 08, 8, 0, 0, 0, DateTimeKind.Utc),
						10,
						new Money(Currency.USD, 10),
						"CashWithdrawal_EUR_2023-08-08_1_USD_0")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleInterest_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/CashTransactions/single_interest.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateInterest(
						Currency.USD,
						new DateTime(2023, 08, 8, 0, 0, 0, DateTimeKind.Utc),
						3.3M,
						"Interest",
						new Money(Currency.USD, 3.3M),
						"Interest_USD_2023-08-08_1_USD_0"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleFee_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/CashTransactions/single_fee.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateFee(
						Currency.USD,
						new DateTime(2023, 08, 8, 0, 0, 0, DateTimeKind.Utc),
						0.25M,
						new Money(Currency.USD, 0.25M),
						"Fee_USD_2023-08-08_1_USD_0.25"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_DoubleFee_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/CashTransactions/double_fee.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateFee(
						Currency.USD,
						new DateTime(2023, 08, 8, 0, 0, 0, DateTimeKind.Utc),
						3M,
						new Money(Currency.USD, 3M),
						"Fee_USD_2023-08-08_1_USD_0.25"),
					PartialActivity.CreateFee(
						Currency.USD,
						new DateTime(2023, 08, 8, 0, 0, 0, DateTimeKind.Utc),
						0.25M,
						new Money(Currency.USD, 0.25M),
						"Fee_USD_2023-08-08_1_USD_0.25"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleTax_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/CashTransactions/single_tax.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateTax(
						Currency.USD,
						new DateTime(2023, 08, 8, 0, 0, 0, DateTimeKind.Utc),
						0.25M,
						new Money(Currency.USD, 0.25M),
						"Tax_USD_2023-08-08_1_USD_"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_DoubleTax_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/CashTransactions/double_tax.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateTax(
						Currency.USD,
						new DateTime(2023, 08, 8, 0, 0, 0, DateTimeKind.Utc),
						3M,
						new Money(Currency.USD, 3M),
						"Tax_USD_2023-08-08_1_USD_"),
					PartialActivity.CreateTax(
						Currency.USD,
						new DateTime(2023, 08, 8, 0, 0, 0, DateTimeKind.Utc),
						0.25M,
						new Money(Currency.USD, 0.25M),
						"Tax_USD_2023-08-08_1_USD_"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDividend_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/CashTransactions/single_dividend.csv", activityManager, account.Name);

			// Assert
			const decimal Amount = (decimal)(0.3247 * 0.27);
			activityManager.PartialActivities.Should().BeEquivalentTo<PartialActivity>(
				[
					PartialActivity.CreateDividend(
						Currency.EUR,
						new DateTime(2023, 08, 8, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateGeneric("US2546871060")],
						Amount,
						new Money(Currency.EUR, Amount),
						"Dividend_US2546871060_2023-08-08_0.3247_EUR_0"),
					PartialActivity.CreateTax(
						Currency.EUR,
						new DateTime(2023, 08, 8, 0, 0, 0, DateTimeKind.Utc),
						0.02M,
						new Money(Currency.EUR, 0.02M),
						"Dividend_US2546871060_2023-08-08_0.3247_EUR_0")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleGiftFiat_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/Specials/single_gift_fiat.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateGift(
						Currency.USD,
						new DateTime(2023, 08, 7, 0, 0, 0, DateTimeKind.Utc),
						25M,
						new Money(Currency.USD, 25M),
						"GiftFiat_EUR_2023-08-07_1_USD_"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleGiftStock_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/Specials/single_gift_stock.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateGift(
						new DateTime(2023, 10, 6, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateGeneric("US2546871060")],
						0.3247M,
						"GiftAsset_US2546871060_2023-10-06_0.3247_EUR_"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleInvalid_NotConverted()
		{
			// Arrange

			// Act
			Func<Task> a = () => parser.ParseActivities("./TestFiles/Generic/Invalid/invalid.csv", activityManager, account.Name);

			// Assert
			await a.Should().ThrowAsync<NotSupportedException>();
		}
	}
}