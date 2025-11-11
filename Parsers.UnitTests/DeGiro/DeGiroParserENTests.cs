using AutoFixture;
using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.DeGiro;

namespace GhostfolioSidekick.Parsers.UnitTests.DeGiro
{
	public class DeGiroParserENTests
	{
		private readonly DeGiroParser parser;
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		public DeGiroParserENTests()
		{
			parser = new DeGiroParser(DummyCurrencyMapper.Instance);

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
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/DeGiro/EN/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyUSD_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/EN/BuyOrders/single_buy_usd.csv", activityManager, account.Name);

			// Assert
			var partialActivities = activityManager.PartialActivities.Where(x => x.ActivityType != PartialActivityType.KnownBalance).ToList();

			IEnumerable<PartialActivity> expectation = [
						PartialActivity.CreateBuy(
							Currency.USD,
							new DateTime(2023, 11, 6, 15, 33, 0, DateTimeKind.Utc),
							PartialSymbolIdentifier.CreateStockAndETF("US40434L1052", "HP INC"),
							5,
							new Money(Currency.USD, 21.31m),
							new Money(Currency.USD, 106.55M),
							"dbe4ec4d-6a6e-4315-b661-820dd1f1d58d"),
						PartialActivity.CreateFee(
							Currency.GBP,
							new DateTime(2023, 11, 6, 15, 33, 0, DateTimeKind.Utc),
							0.02M,
							new Money(Currency.GBP, 0.02M),
							"dbe4ec4d-6a6e-4315-b661-820dd1f1d58d"),
						PartialActivity.CreateFee(
							Currency.GBP,
							new DateTime(2023, 11, 6, 15, 33, 0, DateTimeKind.Utc),
							0.43M,
							new Money(Currency.GBP, 0.43M),
							"dbe4ec4d-6a6e-4315-b661-820dd1f1d58d"),
				];
			partialActivities.Should().BeEquivalentTo(expectation);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyMarketFund_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/EN/BuyOrders/single_buy_marketfund.csv", activityManager, account.Name);

			// Assert
			var partialActivities = activityManager.PartialActivities.Where(x => x.ActivityType != PartialActivityType.KnownBalance).ToList();

			IEnumerable<PartialActivity> expectation = [
						PartialActivity.CreateBuy(
							Currency.GBP,
							new DateTime(2024, 08, 09, 16, 10, 0, DateTimeKind.Utc),
							PartialSymbolIdentifier.CreateStockAndETF("LU0904784781", "MORGAN STANLEY GBP LIQUIDITY FUND"),
							0.5m,
							new Money(Currency.GBP, 1m),
							new Money(Currency.GBP, 0.5M),
							"Buy_2024-08-09 16:10:00:+00:00_MORGAN STANLEY GBP LIQUIDITY FUND_LU0904784781_")
				];
			partialActivities.Should().BeEquivalentTo(expectation);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSellMarketFund_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/EN/SellOrders/single_sell_marketfund.csv", activityManager, account.Name);

			// Assert
			var partialActivities = activityManager.PartialActivities.Where(x => x.ActivityType != PartialActivityType.KnownBalance).ToList();

			IEnumerable<PartialActivity> expectation = [
						PartialActivity.CreateSell(
							Currency.GBP,
							new DateTime(2024, 08, 07, 15, 30, 0, DateTimeKind.Utc),
							PartialSymbolIdentifier.CreateStockAndETF("LU0904784781", "MORGAN STANLEY GBP LIQUIDITY FUND"),
							0.02m,
							new Money(Currency.GBP, 1m),
							new Money(Currency.GBP, 0.02M),
							"Sell_2024-08-07 15:30:00:+00:00_MORGAN STANLEY GBP LIQUIDITY FUND_LU0904784781_")
				];
			partialActivities.Should().BeEquivalentTo(expectation);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDividend_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/EN/CashTransactions/single_dividend.csv", activityManager, account.Name);

			// Assert
			var transactionId = activityManager.PartialActivities.Single(x => x.ActivityType == PartialActivityType.Dividend).TransactionId;
			transactionId.Should().NotBeNullOrWhiteSpace();
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(
						Currency.USD,
						new DateTime(2024, 07, 04, 7, 40, 0, DateTimeKind.Utc),
						1.17M,
						1),
					PartialActivity.CreateKnownBalance(
						Currency.USD,
						new DateTime(2024, 07, 04, 7, 40, 0, DateTimeKind.Utc),
						1.38M,
						2),
					PartialActivity.CreateDividend(
						Currency.USD,
						new DateTime(2024, 07, 04, 7, 40, 0, DateTimeKind.Utc),
						PartialSymbolIdentifier.CreateStockAndETF("US40434L1052", "HP INC"),
						1.38M,
						new Money(Currency.USD, 1.38M),
						transactionId!),
					PartialActivity.CreateTax(
						Currency.USD,
						new DateTime(2024, 07, 04, 7, 40, 0, DateTimeKind.Utc),
						0.21M,
						new Money(Currency.USD, 0.21M),
						transactionId!)
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDividendMarketFund_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/EN/CashTransactions/single_dividend_marketfund.csv", activityManager, account.Name);

			// Assert
			var partialActivities = activityManager.PartialActivities.Where(x => x.ActivityType != PartialActivityType.KnownBalance).ToList();

			IEnumerable<PartialActivity> expectation = [
						PartialActivity.CreateDividend(
							Currency.GBP,
							new DateTime(2024, 08, 08, 15, 27, 0, DateTimeKind.Utc),
							PartialSymbolIdentifier.CreateStockAndETF("LU0904784781", "MORGAN STANLEY GBP LIQUIDITY FUND"),
							0.5m,
							new Money(Currency.GBP, 0.5M),
							"Dividend_2024-08-08 15:27:00:+00:00_MORGAN STANLEY GBP LIQUIDITY FUND_LU0904784781_GBP")
				];
			partialActivities.Should().BeEquivalentTo(expectation);
		}
	}
}