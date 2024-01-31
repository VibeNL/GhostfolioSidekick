using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.Generic;

namespace GhostfolioSidekick.Parsers.UnitTests.Generic
{
	public class GenericParserTests
	{
		private GenericParser parser;
		private Account account;
		private TestHoldingsCollection holdingsAndAccountsCollection;

		public GenericParserTests()
		{
			parser = new GenericParser();

			var fixture = new Fixture();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, new Balance(new Money(Currency.EUR, 0)))
				.Create();
			holdingsAndAccountsCollection = new TestHoldingsCollection(account);
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/Generic/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleOrder_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/BuyOrders/single_buy.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(Currency.USD, new DateTime(2023, 08, 7, 0, 0, 0, DateTimeKind.Utc), [PartialSymbolIdentifier.CreateGeneric("US67066G1040")], 0.0267001000M, 453.33M, "Buy_US67066G1040_2023-08-07_0.0267001000_USD_0.02"),
					PartialActivity.CreateFee(Currency.USD, new DateTime(2023, 08, 7, 0, 0, 0, DateTimeKind.Utc), 0.02M, "Buy_US67066G1040_2023-08-07_0.0267001000_USD_0.02")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleOrderTypeValuable_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/BuyOrders/single_valuable.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateValuable(Currency.EUR, new DateTime(2023, 08, 7, 0, 0, 0, DateTimeKind.Utc), "Giftcard", 250M, "Valuable_Giftcard_2023-08-07_1_EUR_0"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/CashTransactions/single_deposit.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(Currency.USD, new DateTime(2023, 08, 6, 0, 0, 0, 0, DateTimeKind.Utc), 1000, "CashDeposit_EUR_2023-08-06_1_USD_0")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/CashTransactions/single_withdrawal.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashWithdrawal(Currency.USD, new DateTime(2023, 08, 8, 0, 0, 0, DateTimeKind.Utc), 10, "CashWithdrawal_EUR_2023-08-08_1_USD_0")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleFee_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/CashTransactions/single_fee.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateFee(Currency.USD, new DateTime(2023, 08, 8, 0, 0, 0, DateTimeKind.Utc), 0.25M, "Fee_USD_2023-08-08_1_USD_0.25"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDividend_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Generic/CashTransactions/single_dividend.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateDividend(Currency.EUR, new DateTime(2023, 08, 8, 0, 0, 0, DateTimeKind.Utc), [PartialSymbolIdentifier.CreateGeneric("US2546871060")], (decimal)(0.3247 * 0.27), "Dividend_US2546871060_2023-08-08_0.3247_EUR_0"),
					PartialActivity.CreateTax(Currency.EUR, new DateTime(2023, 08, 8, 0, 0, 0, DateTimeKind.Utc), 0.02M, "Dividend_US2546871060_2023-08-08_0.3247_EUR_0")
				]);
		}
	}
}