using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.NIBC;

namespace GhostfolioSidekick.Parsers.UnitTests.NIBC
{
	public class NIBCParserTests
	{
		private readonly NIBCParser parser;
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		public NIBCParserTests()
		{
			parser = new NIBCParser(DummyCurrencyMapper.Instance);

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
			foreach (var file in Directory.GetFiles("./TestFiles/NIBC/", "*.csv", SearchOption.AllDirectories))
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
			await parser.ParseActivities("./TestFiles/NIBC/CashTransactions/single_deposit.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(
						Currency.EUR,
						new DateTime(2020, 01, 27, 0, 0, 0, 0, DateTimeKind.Utc),
						250M,
						new Money(Currency.EUR, 250M),
						"C0A27XM003000782")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/NIBC/CashTransactions/single_withdrawal.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashWithdrawal(
						Currency.EUR,
						new DateTime(2022, 01, 06, 0, 0, 0, 0, DateTimeKind.Utc),
						10000M,
						new Money(Currency.EUR, 10000),
						"C2A06CW1G00A044D")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleInterest_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/NIBC/CashTransactions/single_interest.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateInterest(
						Currency.EUR,
						new DateTime(2021, 09, 30, 0, 0, 0, 0, DateTimeKind.Utc),
						0.51M,
						"Renteuitkering",
						new Money(Currency.EUR, 0.51M),
						"C1I30IN0000A000Q")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBonusInterest_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/NIBC/CashTransactions/single_bonus_interest.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateInterest(
						Currency.EUR,
						new DateTime(2021, 6, 30, 0, 0, 0, DateTimeKind.Utc),
						1.1M,
						"Bonusrente",
						new Money(Currency.EUR, 1.1M),
						"C1F30IN0000A000Q")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_InvalidDescription_Empty()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/NIBC/Invalid/invalid_description.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEmpty();
		}
	}
}