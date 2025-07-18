using AutoFixture;
using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.Trading212;

namespace GhostfolioSidekick.Parsers.UnitTests.Trading212
{
	public class Trading212Tests
	{
		private readonly Trading212Parser parser;
		private readonly Account account;
		private readonly TestActivityManager activityManager;


		public Trading212Tests()
		{
			parser = new Trading212Parser(DummyCurrencyMapper.Instance);

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
			foreach (var file in Directory.GetFiles("./TestFiles/Trading212/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/CashTransactions/single_deposit.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(
						Currency.EUR,
						new DateTime(2023, 08, 07, 19, 56, 01, DateTimeKind.Utc),
						100,
						new Money(Currency.EUR, 100),
						"6b706aa8-780c-4acf-85a0-b329506931dc")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/CashTransactions/single_withdrawal.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashWithdrawal(
						Currency.EUR,
						new DateTime(2023, 11, 17, 05, 49, 12, 337, DateTimeKind.Utc),
						1000,
						new Money(Currency.EUR, 1000),
						"5d72520a-388c-428a-90bf-6d9fcff55534")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleCardDebit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/CashTransactions/single_card_debit.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashWithdrawal(
						Currency.EUR,
						new DateTime(2024, 10, 27, 14, 20, 26, 0, DateTimeKind.Utc),
						4.30m,
						new Money(Currency.EUR, 4.30m),
						"5477a704-5b84-4d9b-a1bd-a41094dfb544")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleCardCredit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/CashTransactions/single_card_credit.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(
						Currency.EUR,
						new DateTime(2024, 10, 27, 14, 20, 26, 0, DateTimeKind.Utc),
						4.30m,
						new Money(Currency.EUR, 4.30m),
						"5477a704-5b84-4d9b-a1bd-a41094dfb544")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleCashback_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/CashTransactions/single_cashback.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(
						Currency.EUR,
						new DateTime(2024, 10, 28, 23, 59, 59, DateTimeKind.Utc),
						1.74m,
						new Money(Currency.EUR, 1.74m),
						"87a1a7ee-5b69-47b6-b4c2-c32584751178")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleInterest_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/CashTransactions/single_interest.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateInterest(
						Currency.EUR,
						new DateTime(2023, 08, 11, 21, 08, 18, DateTimeKind.Utc),
						0.01M,
						"Interest on cash",
						new Money(Currency.EUR, 0.01M),
						"82f82014-23a3-4ddf-bc09-658419823f4c")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleLendingShares_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/CashTransactions/single_lending_shares.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateInterest(
						Currency.EUR,
						new DateTime(2023, 08, 11, 21, 08, 18, DateTimeKind.Utc),
						0.01M,
						"Lending interest",
						new Money(Currency.EUR, 0.01M),
						"82f82014-23a3-4ddf-bc09-658419823f4c")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyUSD_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/BuyOrders/single_buy_usd.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.USD,
						new DateTime(2023, 08, 7, 19, 56, 2, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("US67066G1040")],
						0.0267001M,
						453.33M,
						new Money(Currency.EUR, 11.02M),
						"EOF3219953148"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 08, 7, 19, 56, 2, DateTimeKind.Utc),
						0.02M,
						new Money(Currency.EUR, 0),
						"EOF3219953148"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleLimitBuyUSD_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/BuyOrders/single_limitbuy_usd.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.USD,
						new DateTime(2023, 08, 7, 19, 56, 2, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("US67066G1040")],
						0.0267001M,
						453.33M,
						new Money(Currency.EUR, 11.02M),
						"EOF3219953148"),
					PartialActivity.CreateFee
						(Currency.EUR,
						new DateTime(2023, 08, 7, 19, 56, 2, DateTimeKind.Utc),
						0.02M,
						new Money(Currency.EUR, 0),
						"EOF3219953148"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleOrderEuroUkTaxes_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/BuyOrders/single_buy_euro_uk_taxes.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.GBX,
						new DateTime(2023, 08, 9, 15, 25, 8, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("GB0007188757")],
						0.18625698M,
						4947.00M,
						new Money(Currency.EUR, 10.75M),
						"EOF3224031549"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 08, 9, 15, 25, 8, DateTimeKind.Utc),
						0.02M,
						new Money(Currency.EUR, 0),
						"EOF3224031549"),
					PartialActivity.CreateTax(
						Currency.EUR,
						new DateTime(2023, 08, 9, 15, 25, 8, DateTimeKind.Utc),
						0.05M,
						new Money(Currency.EUR, 0),
						"EOF3224031549"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDividend_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/CashTransactions/single_dividend.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateDividend(
						Currency.USD,
						new DateTime(2023, 08, 17, 10, 49, 49, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("US0378331005")],
						0.025583540000M,
						new Money(Currency.EUR, 0.02M),
						"Dividend (Dividends paid by us corporations)_US0378331005_2023-08-17_0.02_USD")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDividendGbp_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/CashTransactions/single_dividend_gbp.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateDividend(
						Currency.GBX,
						new DateTime(2024, 01, 12, 13, 25, 07, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("GG00BYZSSY63")],
						478.496796400000M,
						new Money(Currency.EUR, 5.57M),
						"Dividend (Dividend)_GG00BYZSSY63_2024-01-12_5.57_GBX")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyGBP_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/BuyOrders/single_buy_gbp.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.GBX,
						new DateTime(2023, 08, 9, 15, 25, 8, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("GB0007188757")],
						0.18625698M,
						4947.00M,
						new Money(Currency.EUR, 10.75M),
						"EOF3224031549"),
					PartialActivity.CreateTax(
						Currency.GBP,
						new DateTime(2023, 08, 9, 15, 25, 8, DateTimeKind.Utc),
						0.05M,
						new Money(Currency.GBP, 0),
						"EOF3224031549"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleConvertCurrencies_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/CashTransactions/single_convert_currencies.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(
						Currency.EUR,
						new DateTime(2023, 09, 25, 17, 31, 38, 897, DateTimeKind.Utc),
						0.01M,
						new Money(Currency.GBP, 0),
						"RBLF1WQUEL4OG5D3"),
					PartialActivity.CreateCashWithdrawal(
						Currency.GBP,
						new DateTime(2023, 09, 25, 17, 31, 38, 897, DateTimeKind.Utc),
						0.01M,
						new Money(Currency.GBP, 0),
						"RBLF1WQUEL4OG5D3")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyEuroFrenchTaxes_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/BuyOrders/single_buy_euro_french_taxes.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 10, 9, 14, 28, 20, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("FR0010828137")],
						14.7252730000M,
						13.88M,
						new Money(Currency.EUR, 205),
						"EOF4500547227"),
					PartialActivity.CreateTax(
						Currency.EUR,
						new DateTime(2023, 10, 9, 14, 28, 20, DateTimeKind.Utc),
						0.61M,
						new Money(Currency.EUR, 0),
						"EOF4500547227")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyEuroFinraFee_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/BuyOrders/single_buy_euro_finra_fee.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 10, 9, 14, 28, 20, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("FR0010828137")],
						14.7252730000M,
						13.88M,
						new Money(Currency.EUR, 205),
						"EOF4500547227"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 10, 9, 14, 28, 20, DateTimeKind.Utc),
						0.61M,
						new Money(Currency.EUR, 0),
						"EOF4500547227")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSellEuro_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/SellOrders/single_sell_euro.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSell(
						Currency.USD,
						new DateTime(2023, 10, 9, 14, 26, 43, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("US7561091049")],
						0.2534760000M,
						50.38M,
						new Money(Currency.EUR, 12.08M),
						"EOF4500546889"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 10, 9, 14, 26, 43, DateTimeKind.Utc),
						0.02M,
						new Money(Currency.EUR, 0),
						"EOF4500546889"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleLimitSellEuro_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/SellOrders/single_limitsell_euro.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSell(
						Currency.USD,
						new DateTime(2023, 10, 9, 14, 26, 43, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("US7561091049")],
						0.2534760000M,
						50.38M,
						new Money(Currency.EUR, 12.08M),
						"EOF4500546889"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 10, 9, 14, 26, 43, DateTimeKind.Utc),
						0.02M,
						new Money(Currency.EUR, 0),
						"EOF4500546889"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleStopSellEuro_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/SellOrders/single_stopsell_euro.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSell(
						Currency.USD,
						new DateTime(2023, 10, 9, 14, 26, 43, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("US7561091049")],
						0.2534760000M,
						50.38M,
						new Money(Currency.EUR, 12.08M),
						"EOF4500546889"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 10, 9, 14, 26, 43, DateTimeKind.Utc),
						0.02M,
						new Money(Currency.EUR, 0),
						"EOF4500546889"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleStockDividend_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trading212/Specials/single_stock_dividend.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.USD,
						new DateTime(2024, 11, 07, 13, 20, 27, 947, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("US84265V1052")],
						0.0119387200M,
						0M,
						new Money(Currency.EUR, 0M),
						"EOF23150970942"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_InvalidAction_ThrowNotSupported()
		{
			// Arrange

			// Act
			Func<Task> a = () => parser.ParseActivities("./TestFiles/Trading212/Invalid/invalid_action.csv", activityManager, account.Name);

			// Assert
			await a.Should().ThrowAsync<NotSupportedException>();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_InvalidNote_ThrowNotSupported()
		{
			// Arrange

			// Act
			Func<Task> a = () => parser.ParseActivities("./TestFiles/Trading212/Invalid/invalid_note.csv", activityManager, account.Name);

			// Assert
			await a.Should().ThrowAsync<NotSupportedException>().WithMessage("Conversion without Notes");
		}
	}
}