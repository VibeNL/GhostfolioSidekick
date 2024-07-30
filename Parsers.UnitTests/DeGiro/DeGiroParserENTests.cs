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
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
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