using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.DeGiro;

namespace GhostfolioSidekick.Parsers.UnitTests.DeGiro
{
	public class DeGiroParserENTests
	{
		private readonly DeGiroParserEN parser;
		private readonly Account account;
		private readonly TestHoldingsCollection holdingsAndAccountsCollection;

		public DeGiroParserENTests()
		{
			parser = new DeGiroParserEN(DummyCurrencyMapper.Instance);

			var fixture = new Fixture();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, new Balance(DateTime.Today, new Money(Currency.EUR, 0)))
				.Create();
			holdingsAndAccountsCollection = new TestHoldingsCollection(account);
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
			await parser.ParseActivities("./TestFiles/DeGiro/EN/BuyOrders/single_buy_usd.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			var partialActivities = holdingsAndAccountsCollection.PartialActivities.Where(x => x.ActivityType != PartialActivityType.KnownBalance).ToList();

			var currencyConversion = PartialActivity.CreateCurrencyConvert(
						new DateTime(2023, 11, 6, 15, 33, 0, DateTimeKind.Utc),
						new Money(Currency.GBP, 82.8M),
						new Money(Currency.USD, 106.55M),
						new Money(Currency.GBP, 0.43M),
						"dbe4ec4d-6a6e-4315-b661-820dd1f1d58d");
			IEnumerable<PartialActivity> expectation = [
								PartialActivity.CreateBuy(
						Currency.USD,
						new DateTime(2023, 11, 6, 15, 33, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("US40434L1052")],
						5,
						21.31m,
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
			partialActivities.Should().BeEquivalentTo(
				expectation.Union(currencyConversion));
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDividend_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/EN/CashTransactions/single_dividend.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			var transactionId = holdingsAndAccountsCollection.PartialActivities.Single(x => x.ActivityType == PartialActivityType.Dividend).TransactionId;
			transactionId.Should().NotBeNullOrWhiteSpace();
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
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
						[PartialSymbolIdentifier.CreateStockAndETF("US40434L1052")],
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
	}
}