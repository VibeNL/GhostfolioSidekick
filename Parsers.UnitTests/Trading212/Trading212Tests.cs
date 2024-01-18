using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.Trading212;

namespace Parsers.UnitTests.Trading212
{
	public class Trading212Tests
	{
		private Trading212Parser parser;
		private Account account;
		private TestHoldingsCollection holdingsAndAccountsCollection;


		public Trading212Tests()
		{
			parser = new Trading212Parser();

			var fixture = new Fixture();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, new Balance(Currency.EUR))
				.Create();
			holdingsAndAccountsCollection = new TestHoldingsCollection(account);
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/Trading212/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/CashTransactions/single_deposit.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(Currency.EUR, new DateTime(2023, 08, 07, 19, 56, 01, DateTimeKind.Utc), 100, "6b706aa8-780c-4acf-85a0-b329506931dc")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/CashTransactions/single_withdrawal.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashWithdrawal(Currency.EUR, new DateTime(2023, 11, 17, 05, 49, 12, 337, DateTimeKind.Utc), 1000, "5d72520a-388c-428a-90bf-6d9fcff55534")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleInterest_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/CashTransactions/single_interest.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateInterest(Currency.EUR, new DateTime(2023, 08, 11, 21, 08, 18, DateTimeKind.Utc), 0.01M, "82f82014-23a3-4ddf-bc09-658419823f4c")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyUSD_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/BuyOrders/single_buy_usd.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(Currency.USD, new DateTime(2023, 08, 7, 19, 56, 2, DateTimeKind.Utc), ["US67066G1040"], 0.0267001M, 453.33M, "EOF3219953148"),
					PartialActivity.CreateFee(Currency.EUR, new DateTime(2023, 08, 7, 19, 56, 2, DateTimeKind.Utc), 0.02M, "EOF3219953148"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleOrderEuroUkTaxes_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/BuyOrders/single_buy_euro_uk_taxes.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			/*account.Activities.Should().BeEquivalentTo(new[] { new Activity(
				ActivityType.Buy,
				asset,
				new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc),
				0.18625698M,
				new Money(DefaultCurrency.GBX,4947.00M, new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc)),
				new[] {
					new Money(DefaultCurrency.EUR, 0.05M, new DateTime(2023, 08, 9, 15, 25, 8, DateTimeKind.Utc)) ,
					new Money(DefaultCurrency.EUR, 0.02M, new DateTime(2023, 08, 9, 15, 25, 8, DateTimeKind.Utc))
				},
				"Transaction Reference: [EOF3224031549] (Details: asset GB0007188757)",
				"EOF3224031549"
				)
			});*/
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDividend_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/CashTransactions/single_dividend.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateDividend(Currency.USD, new DateTime(2023, 08, 17, 10, 49, 49, DateTimeKind.Utc), ["US0378331005"], 0.02M, "Dividend (Dividends paid by us corporations)_US0378331005_2023-08-17_0.02_USD")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyGBP_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/BuyOrders/single_buy_gbp.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(Currency.GBp, new DateTime(2023, 08, 9, 15, 25, 8, DateTimeKind.Utc), ["GB0007188757"], 0.18625698M, 4947.00M, "EOF3224031549"),
					PartialActivity.CreateTax(Currency.GBP, new DateTime(2023, 08, 9, 15, 25, 8, DateTimeKind.Utc), 0.05M, "EOF3224031549"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleConvertCurrencies_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/CashTransactions/single_convert_currencies.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(Currency.EUR, new DateTime(2023, 09, 25, 17, 31, 38, 897, DateTimeKind.Utc), 0.01M, "RBLF1WQUEL4OG5D3"),
					PartialActivity.CreateCashWithdrawal(Currency.GBP, new DateTime(2023, 09, 25, 17, 31, 38, 897, DateTimeKind.Utc), 0.01M, "RBLF1WQUEL4OG5D3")
				]);
			/*account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 0.00M, new DateTime(2023, 09, 25, 17, 31, 38, 897, DateTimeKind.Utc)));
			account.Activities.Should().BeEmpty();*/
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyEuroFrenchTaxes_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/BuyOrders/single_buy_euro_french_taxes.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(Currency.EUR, new DateTime(2023, 10, 9, 14, 28, 20, DateTimeKind.Utc), ["FR0010828137"], 14.7252730000M, 13.88M, "EOF4500547227"),
					PartialActivity.CreateTax(Currency.EUR, new DateTime(2023, 10, 9, 14, 28, 20, DateTimeKind.Utc), 0.61M, "EOF4500547227")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSellEuro_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/SellOrders/single_sell_euro.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSell(Currency.USD, new DateTime(2023, 10, 9, 14, 26, 43, DateTimeKind.Utc), ["US7561091049"], 0.2534760000M, 50.38M, "EOF4500546889"),
					PartialActivity.CreateFee(Currency.EUR, new DateTime(2023, 10, 9, 14, 26, 43, DateTimeKind.Utc), 0.02M, "EOF4500546889"),
				]);
		}
	}
}