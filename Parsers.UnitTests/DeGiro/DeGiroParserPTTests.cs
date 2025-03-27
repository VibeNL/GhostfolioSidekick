using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.DeGiro;

namespace GhostfolioSidekick.Parsers.UnitTests.DeGiro
{
	public class DeGiroParserPTTests
	{
		private readonly DeGiroParser parser;
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		public DeGiroParserPTTests()
		{
			parser = new DeGiroParser(DummyCurrencyMapper.Instance);

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
			foreach (var file in Directory.GetFiles("./TestFiles/DeGiro/PT/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyEuro_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/PT/BuyOrders/single_buy_euro.csv", activityManager, account.Name);

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
		public async Task ConvertActivitiesForAccount_SingleBuyGBX_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/PT/BuyOrders/single_buy_GBX.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(
						Currency.GBP,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						-49.35M,
						1),
					PartialActivity.CreateBuy(
						Currency.GBX,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("AU000000GBP6")],
						1,
						235M,
						new Money(Currency.GBP, 49.35M),
						"b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSellEuro_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/PT/SellOrders/single_sell_euro.csv", activityManager, account.Name);

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
						new Money(Currency.EUR, 1),
						"b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/PT/CashTransactions/single_deposit.csv", activityManager, account.Name);

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
						new Money(Currency.EUR, 1000),
						"CashDeposit_2023-12-28 04:51:00:+00:00___EUR")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleFee_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/PT/CashTransactions/single_fee.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(
						Currency.EUR,
						new DateTime(2023, 1, 3, 14, 6, 0, 0, DateTimeKind.Utc),
						102.18M,
						1),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 1, 3, 14, 6, 0, DateTimeKind.Utc),
						2.5M,
						new Money(Currency.EUR, 2.5M),
						"Fee_2023-01-03 14:06:00:+00:00___EUR")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleInterest_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/PT/CashTransactions/single_interest.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(
						Currency.EUR,
						new DateTime(2024, 01, 02, 16, 51, 00, DateTimeKind.Utc),
						0,
						1),
					PartialActivity.CreateInterest(
						Currency.EUR,
						new DateTime(2024, 01, 02, 16, 51, 00, DateTimeKind.Utc),
						1M,
						"Flatex Interest Income",
						new Money(Currency.EUR, 1),
						"Interest_2024-01-02 16:51:00:+00:00___EUR")
				]);
		}
	}
}