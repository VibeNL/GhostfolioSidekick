using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.ScalableCaptial;

namespace GhostfolioSidekick.Parsers.UnitTests.ScalableCapital
{
	public class ScalableCapitalPrimeParserTests
	{
		private readonly ScalableCapitalPrimeParser parser;
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		public ScalableCapitalPrimeParserTests()
		{
			parser = new ScalableCapitalPrimeParser(DummyCurrencyMapper.Instance);

			var fixture = new Fixture();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, [new Balance(DateTime.Today, new Money(Currency.EUR, 0))])
				.Create();
			activityManager = new TestActivityManager();
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/ScalableCapital/Prime/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ParseActivities_SingleBuy_CorrectlyParsed()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/Prime/BuyOrders/single_buy_euro.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2021,11,20, 2,0,0, 0, DateTimeKind.Utc),
						0.99M,
						new Money(Currency.EUR, 0.99M),
						"abcde"),
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2021,11,20, 2,0,0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("US5949181045")],
						2,
						227.85M,
						new Money(Currency.EUR, 455.7M),
						"abcde")
				]);
		}

		[Fact]
		public async Task ParseActivities_SingleSavingsPlan_CorrectlyParsed()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/Prime/BuyOrders/single_savingsplan.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 06, 11, 11, 29, 06, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("IE00BJ0KDQ92")],
						4.981M,
						100.38M,
						new Money(Currency.EUR, 499.99278M),
						"abcde")
				]);
		}

		[Fact]
		public async Task ParseActivities_SingleSell_CorrectlyParsed()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/Prime/SellOrders/single_sell_euro.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2021, 11, 20, 02, 00, 00, 0, DateTimeKind.Utc),
						0.99M,
						new Money(Currency.EUR, 0.99M),
						"abcde"),
					PartialActivity.CreateSell(
						Currency.EUR,
						new DateTime(2021, 11, 20, 02, 00, 00, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("US5949181045")],
						2,
						227.85M,
						new Money(Currency.EUR, 455.7M),
						"abcde")
				]);
		}

		[Fact]
		public async Task ParseActivities_SingleDeposit_CorrectlyParsed()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/Prime/CashTransactions/single_deposit.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(
						Currency.EUR,
						new DateTime(2022, 06, 22, 16, 52, 13, 0, DateTimeKind.Utc),
						2500,
						new Money(Currency.EUR, 2500),
						"abcde")
				]);
		}

		[Fact]
		public async Task ParseActivities_SingleDividend_CorrectlyParsed()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/Prime/CashTransactions/single_dividend.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateDividend(
						Currency.EUR,
						new DateTime(2022, 11, 12, 11, 40, 50, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("US5949181045")],
						1.08M,
						new Money(Currency.EUR, 1.08M),
						"abcde")
				]);
		}

		[Fact]
		public async Task ParseActivities_SingleWithdrawal_CorrectlyParsed()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/Prime/CashTransactions/single_withdrawal.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashWithdrawal(
						Currency.EUR,
						new DateTime(2022, 06, 22, 16, 52, 13, 0, DateTimeKind.Utc),
						2500,
						new Money(Currency.EUR, 2500),
						"abcde")
				]);
		}

		[Fact]
		public async Task ParseActivities_SingleBuyPending_CorrectlyParsed()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/Prime/BuyOrders/single_buy_euro_pending.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEmpty();
		}

		[Fact]
		public async Task ParseActivities_SingleBuyWithTax_CorrectlyParsed()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/Prime/BuyOrders/single_buy_euro_tax.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2021,11,20, 2,0,0, 0, DateTimeKind.Utc),
						0.99M,
						new Money(Currency.EUR, 0.99M),
						"abcde"),
						PartialActivity.CreateTax(
						Currency.EUR,
						new DateTime(2021,11,20, 2,0,0, 0, DateTimeKind.Utc),
						2,
						new Money(Currency.EUR, 2),
						"abcde"),
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2021,11,20, 2,0,0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("US5949181045")],
						2,
						227.85M,
						new Money(Currency.EUR, 455.7M),
						"abcde")
				]);
		}

		[Fact]
		public async Task ParseActivities_Invalid_ThrowsException()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/Prime/Invalid/invalid_type.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEmpty();
		}
	}
}
