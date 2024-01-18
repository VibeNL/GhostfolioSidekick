using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.NIBC;

namespace Parsers.UnitTests.NIBC
{
	public class NIBCParserTests
	{
		private NIBCParser parser;
		private Account account;
		private TestHoldingsAndAccountsCollection holdingsAndAccountsCollection;

		public NIBCParserTests()
		{
			parser = new NIBCParser();

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
			foreach (var file in Directory.GetFiles("./TestFiles/NIBC/", "*.csv", SearchOption.AllDirectories))
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
			await parser.ParseActivities("./TestFiles/NIBC/CashTransactions/single_deposit.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(Currency.EUR, new DateTime(2020, 01, 27, 0, 0, 0, 0, DateTimeKind.Utc), 250M, "C0A27XM003000782")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/NIBC/CashTransactions/single_withdrawal.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashWithdrawal(Currency.EUR, new DateTime(2022, 01, 06, 0, 0, 0, 0, DateTimeKind.Utc), 10000M, "C2A06CW1G00A044D")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleInterest_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/NIBC/CashTransactions/single_interest.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateInterest(Currency.EUR, new DateTime(2021, 09, 30, 0, 0, 0, 0, DateTimeKind.Utc), 0.51M, "C1I30IN0000A000Q")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBonusInterest_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/NIBC/CashTransactions/single_bonus_interest.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateInterest(Currency.EUR, new DateTime(2021, 6, 30, 0, 0, 0, DateTimeKind.Utc), 1.1M, "C1F30IN0000A000Q")
				]);
		}
	}
}