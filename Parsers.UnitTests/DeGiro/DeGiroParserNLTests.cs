using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.DeGiro;

namespace GhostfolioSidekick.Parsers.UnitTests.DeGiro
{
	public class DeGiroParserNLTests
	{
		private readonly DeGiroParserNL parser;
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		public DeGiroParserNLTests()
		{
			parser = new DeGiroParserNL(DummyCurrencyMapper.Instance);

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
			foreach (var file in Directory.GetFiles("./TestFiles/DeGiro/NL/", "*.csv", SearchOption.AllDirectories))
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
			await parser.ParseActivities("./TestFiles/DeGiro/NL/CashTransactions/single_deposit.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(
						Currency.EUR,
						new DateTime(2023, 12, 28, 04, 51, 0, DateTimeKind.Utc),
						42.92M,
						1),
					PartialActivity.CreateCashDeposit(
						Currency.EUR,
						new DateTime(2023, 12, 28, 04, 51, 0, DateTimeKind.Utc),
						1000,
						new Money(Currency.EUR, 1000M),
						"CashDeposit_2023-12-28 04:51:00:+00:00___EUR")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL/CashTransactions/single_withdrawal.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(
						Currency.EUR,
						new DateTime(2023, 12, 28, 04, 51, 0, DateTimeKind.Utc),
						42.92M,
						1),
					PartialActivity.CreateCashWithdrawal(
						Currency.EUR,
						new DateTime(2023, 12, 28, 04, 51, 0, DateTimeKind.Utc),
						1000,
						new Money(Currency.EUR, 1000),
						"CashWithdrawal_2023-12-28 04:51:00:+00:00___EUR")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyEuro_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL/BuyOrders/single_buy_euro.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(
						Currency.EUR,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						21.70M,
						1),
					PartialActivity.CreateKnownBalance(
						Currency.EUR,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						21.70M,
						2),
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("IE00B3XXRP09")],
						1,
						77.30M,
						new Money(Currency.EUR, 77.3M),
						"b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						1M,
						new Money(Currency.EUR, 1),
						"b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyEuroWholeNumber_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL/BuyOrders/single_buy_euro_whole_number.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(
						Currency.EUR,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						21.70M,
						1),
					PartialActivity.CreateKnownBalance(
						Currency.EUR,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						21.70M,
						2),
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("IE00B3XXRP09")],
						1,
						77M,
						new Money(Currency.EUR, 77.3M),
						"b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						1M,
						new Money(Currency.EUR, 1),
						"b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyUSD_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL/BuyOrders/single_buy_usd.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(
						Currency.USD,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						21.70M,
						1),
					PartialActivity.CreateKnownBalance(
						Currency.USD,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						21.70M,
						2),
					PartialActivity.CreateBuy(
						Currency.USD,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("IE00B3XXRP09")],
						1,
						77.3M,
						new Money(Currency.USD, 77.3M),
						"b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a"),
					PartialActivity.CreateFee(
						Currency.USD,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						1M,
						new Money(Currency.USD, 1),
						"b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSellEuro_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL/SellOrders/single_sell_euro.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(
						Currency.EUR,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						21.70M,
						1),
					PartialActivity.CreateKnownBalance(
						Currency.EUR,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						21.70M,
						2),
					PartialActivity.CreateSell(
						Currency.EUR,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("IE00B3XXRP09")],
						1,
						77.3M,
						new Money(Currency.EUR, 77.3M),
						"b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						1M,
						new Money(Currency.EUR, 1M),
						"b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSellUSD()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL/SellOrders/single_sell_usd.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(
						Currency.USD,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						21.70M,
						1),
					PartialActivity.CreateKnownBalance(
						Currency.USD,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						21.70M,
						2),
					PartialActivity.CreateSell(
						Currency.USD,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("IE00B3XXRP09")],
						1,
						77.3M,
						new Money(Currency.USD, 77.3M),
						"b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a"),
					PartialActivity.CreateFee(
						Currency.USD,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						1M,
						new Money(Currency.USD, 1),
						"b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyEuroMultipart_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL/BuyOrders/single_buy_euro_multipart.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(
						Currency.EUR,
						new DateTime(2023, 11, 10, 17, 10, 0, DateTimeKind.Utc),
						9.77M,
						1),
					PartialActivity.CreateKnownBalance(
						Currency.EUR,
						new DateTime(2023, 11, 10, 17, 10, 0, DateTimeKind.Utc),
						12.77M,
						2),
					PartialActivity.CreateKnownBalance(
						Currency.EUR,
						new DateTime(2023, 11, 10, 17, 10, 0, DateTimeKind.Utc),
						926.69M,
						3),
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 11, 10, 17, 10, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("NL0011794037")],
						34,
						26.88M,
						new Money(Currency.EUR, 913.92M),
						"35d4345a-467c-42bd-848c-f6087737dd36"),
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 11, 10, 17, 10, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("NL0011794037")],
						4,
						26.88M,
						new Money(Currency.EUR, 107.52M),
						"35d4345a-467c-42bd-848c-f6087737dd36"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 11, 10, 17, 10, 0, DateTimeKind.Utc),
						3M,
						new Money(Currency.EUR, 3M),
						"35d4345a-467c-42bd-848c-f6087737dd36")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDividend_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL/CashTransactions/single_dividend.csv", activityManager, account.Name);

			// Assert
			var transactionId = activityManager.PartialActivities.Single(x => x.ActivityType == PartialActivityType.Dividend).TransactionId;
			transactionId.Should().NotBeNullOrWhiteSpace();
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(
						Currency.EUR,
						new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc),
						33.96M,
						1),
					PartialActivity.CreateKnownBalance(
						Currency.EUR,
						new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc),
						24.39M,
						2),
					PartialActivity.CreateDividend(
						Currency.EUR,
						new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("NL0009690239")],
						9.57M,
						new Money(Currency.EUR, 9.57M),
						transactionId!),
					PartialActivity.CreateTax(
						Currency.EUR,
						new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc),
						1.44M,
						new Money(Currency.EUR, 1.44M),
						transactionId!)
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDividendNoTax_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL//CashTransactions/single_dividend_notax.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(
						Currency.EUR,
						new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc),
						33.96M,
						1),
					PartialActivity.CreateDividend(
						Currency.EUR,
						new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("NL0009690239")],
						9.57M,
						new Money(Currency.EUR, 9.57M),
						"Dividend_2023-09-14 06:32:00:+00:00_VANECK GLOBAL REAL ESTATE UCITS ETF_NL0009690239_Dividend"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_InvalidNoDescription_OnlyKnownBalance()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL/Invalid/no_description.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(Currency.EUR, new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc), 33.96M, 1),
				]);
		}
	}
}