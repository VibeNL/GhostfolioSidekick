using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.Bunq;

namespace GhostfolioSidekick.Parsers.UnitTests.Bunq
{
	public class BunqParserTests
	{
		readonly BunqParser parser;
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		public BunqParserTests()
		{
			parser = new BunqParser();

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
			foreach (var file in Directory.GetFiles("./TestFiles/Bunq/", "*.csv", SearchOption.AllDirectories))
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
			await parser.ParseActivities("./TestFiles/Bunq/CashTransactions/single_deposit.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateCashDeposit(
					Currency.EUR,
					new DateTime(2023, 07, 20, 0, 0, 0, DateTimeKind.Utc),
					1000,
					new Money(Currency.EUR, 1000),
					"2023-07-20 00:00:00:+00:00_1")]
				);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Bunq/CashTransactions/single_withdrawal.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateCashWithdrawal(
					Currency.EUR,
					new DateTime(2023, 07, 20, 0, 0, 0, DateTimeKind.Utc),
					100,
					new Money(Currency.EUR, 100),
					"2023-07-20 00:00:00:+00:00_1")]
				);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleInterest_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Bunq/CashTransactions/single_interest.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateInterest(
					Currency.EUR,
					new DateTime(2023, 07, 27, 0, 0, 0, DateTimeKind.Utc),
					3.5M,
					"bunq Payday 2023-07-27 EUR",
					new Money(Currency.EUR, 3.5M),
					"2023-07-27 00:00:00:+00:00_1")]
				);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestMultipleDepositsOn1Day_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Bunq/CashTransactions/multiple_deposits.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(
						Currency.EUR,
						new DateTime(2023, 07, 20, 0, 0, 0, DateTimeKind.Utc),
						1000,
						new Money(Currency.EUR, 1000),
						"2023-07-20 00:00:00:+00:00_1"),
					PartialActivity.CreateCashDeposit(
						Currency.EUR,
						new DateTime(2023, 07, 20, 0, 0, 0, DateTimeKind.Utc),
						1000,
						new Money(Currency.EUR, 1000),
						"2023-07-20 00:00:00:+00:00_2"),
					PartialActivity.CreateCashDeposit(
						Currency.EUR,
						new DateTime(2023, 07, 20, 0, 0, 0, DateTimeKind.Utc),
						1000,
						new Money(Currency.EUR, 1000),
						"2023-07-20 00:00:00:+00:00_3")]
				);
		}
	}
}