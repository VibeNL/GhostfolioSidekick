using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.DeGiro;

namespace Parsers.UnitTests.DeGiro
{
	public class DeGiroParserNLTests
	{
		private DeGiroParserNL parser;
		private Account account;
		private TestHoldingsAndAccountsCollection holdingsAndAccountsCollection;

		public DeGiroParserNLTests()
		{
			parser = new DeGiroParserNL();

			var fixture = new Fixture();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, new Balance(Currency.EUR))
				.Create();
			holdingsAndAccountsCollection = new TestHoldingsAndAccountsCollection(account);
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/DeGiro/NL//", "*.csv", SearchOption.AllDirectories))
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
			await parser.ParseActivities("./TestFiles/DeGiro/NL//CashTransactions/single_deposit.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(Currency.EUR, new DateTime(2023, 12, 28, 04, 51, 0, DateTimeKind.Utc), 42.92M, null),
					PartialActivity.CreateCashDeposit(Currency.EUR, new DateTime(2023, 12, 28, 04, 51, 0, DateTimeKind.Utc), 1000, string.Empty)
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyEuro_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL//BuyOrders/single_buy_euro.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(Currency.EUR, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), 21.70M, null),
					PartialActivity.CreateKnownBalance(Currency.EUR, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), 21.70M, null),
					PartialActivity.CreateBuy(Currency.EUR, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), "IE00B3XXRP09", 1, 77.30M, "b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a"),
					PartialActivity.CreateFee(Currency.EUR, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), 1M, "b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyEuroWholeNumber_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL//BuyOrders/single_buy_euro_whole_number.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(Currency.EUR, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), 21.70M, null),
					PartialActivity.CreateKnownBalance(Currency.EUR, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), 21.70M, null),
					PartialActivity.CreateBuy(Currency.EUR, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), "IE00B3XXRP09", 1, 77M, "b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a"),
					PartialActivity.CreateFee(Currency.EUR, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), 1M, "b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyUSD_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL//BuyOrders/single_buy_usd.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(Currency.USD, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), 21.70M, null),
					PartialActivity.CreateKnownBalance(Currency.USD, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), 21.70M, null),
					PartialActivity.CreateBuy(Currency.USD, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), "IE00B3XXRP09", 1, 77.3M, "b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a"),
					PartialActivity.CreateFee(Currency.USD, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), 1M, "b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSellEuro_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL//SellOrders/single_sell_euro.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(Currency.EUR, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), 21.70M, null),
					PartialActivity.CreateKnownBalance(Currency.EUR, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), 21.70M, null),
					PartialActivity.CreateSell(Currency.EUR, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), "IE00B3XXRP09", 1, 77.3M, "b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a"),
					PartialActivity.CreateFee(Currency.EUR, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), 1M, "b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSellUSD()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL//SellOrders/single_sell_usd.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(Currency.USD, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), 21.70M, null),
					PartialActivity.CreateKnownBalance(Currency.USD, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), 21.70M, null),
					PartialActivity.CreateSell(Currency.USD, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), "IE00B3XXRP09", 1, 77.3M, "b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a"),
					PartialActivity.CreateFee(Currency.USD, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc), 1M, "b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyEuroMultipart_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL//BuyOrders/single_buy_euro_multipart.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(Currency.EUR, new DateTime(2023, 11, 10, 17, 10, 0, DateTimeKind.Utc), 9.77M, null),
					PartialActivity.CreateKnownBalance(Currency.EUR, new DateTime(2023, 11, 10, 17, 10, 0, DateTimeKind.Utc), 12.77M, null),
					PartialActivity.CreateKnownBalance(Currency.EUR, new DateTime(2023, 11, 10, 17, 10, 0, DateTimeKind.Utc), 926.69M, null),
					PartialActivity.CreateBuy(Currency.EUR, new DateTime(2023, 11, 10, 17, 10, 0, DateTimeKind.Utc), "NL0011794037", 34, 26.88M, "35d4345a-467c-42bd-848c-f6087737dd36"),
					PartialActivity.CreateBuy(Currency.EUR, new DateTime(2023, 11, 10, 17, 10, 0, DateTimeKind.Utc), "NL0011794037", 4, 26.88M, "35d4345a-467c-42bd-848c-f6087737dd36"),
					PartialActivity.CreateFee(Currency.EUR, new DateTime(2023, 11, 10, 17, 10, 0, DateTimeKind.Utc), 3M, "35d4345a-467c-42bd-848c-f6087737dd36")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDividend_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL//CashTransactions/single_dividend.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(Currency.EUR, new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc), 33.96M, null),
					PartialActivity.CreateKnownBalance(Currency.EUR, new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc), 24.39M, null),
					PartialActivity.CreateDividend(Currency.EUR, new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc), "NL0009690239", 9.57M, string.Empty),
					PartialActivity.CreateTax(Currency.EUR, new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc), 1.44M, string.Empty)
				]);
			/*account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR,
				24.39M, new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[]
			{
				new Activity(
					ActivityType.Dividend,
					asset,
					new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc),
					1,
					new Money(DefaultCurrency.EUR, 8.13M, new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc)),
					Enumerable.Empty<Money>(),
					"Transaction Reference: [Dividend_2023-09-14_06:32_NL0009690239] (Details: asset NL0009690239)",
					"Dividend_2023-09-14_06:32_NL0009690239"
					)
			});*/
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDividendNoTax_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/DeGiro/NL//CashTransactions/single_dividend_notax.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(Currency.EUR, new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc), 33.96M, null),
					PartialActivity.CreateDividend(Currency.EUR, new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc), "NL0009690239", 9.57M, string.Empty),
				]);
			/*account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR,
				33.96M, new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[]
			{
				new Activity(
					ActivityType.Dividend,
					asset,
					new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc),
					1,
					new Money(DefaultCurrency.EUR, 9.57M, new DateTime(2023, 09, 14, 6, 32, 0, DateTimeKind.Utc)),
					Enumerable.Empty<Money>(),
					"Transaction Reference: [Dividend_2023-09-14_06:32_NL0009690239] (Details: asset NL0009690239)",
					"Dividend_2023-09-14_06:32_NL0009690239"
					)
			});*/
		}
	}
}