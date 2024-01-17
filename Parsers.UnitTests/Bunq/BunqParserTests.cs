using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.Bunq;

namespace Parsers.UnitTests.Bunq
{
	public class BunqParserTests
	{
		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			var parser = new BunqParser();

			foreach (var file in Directory.GetFiles("./TestFiles/Bunq/", "*.csv", SearchOption.AllDirectories))
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
			var parser = new BunqParser();
			var fixture = new Fixture();
			var account = fixture
				.Build<Account>()
				.With(x => x.Balance, new Balance(Currency.EUR))
				.Create();
			var holdingsAndAccountsCollection = new TestHoldingsAndAccountsCollection(account);

			// Act
			await parser.ParseActivities("./TestFiles/Bunq/CashTransactions/single_deposit.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateCashDeposit(Currency.EUR, new DateTime(2023, 07, 20, 0, 0, 0, DateTimeKind.Utc), 1000, null)]
				);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange
			var parser = new BunqParser();
			var fixture = new Fixture();
			var account = fixture
				.Build<Account>()
				.With(x => x.Balance, new Balance(Currency.EUR))
				.Create();
			var holdingsAndAccountsCollection = new TestHoldingsAndAccountsCollection(account);

			// Act
			await parser.ParseActivities("./TestFiles/Bunq/CashTransactions/single_withdrawal.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateCashWithdrawal(Currency.EUR, new DateTime(2023, 07, 20, 0, 0, 0, DateTimeKind.Utc), 100, null)]
				);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleInterest_Converted()
		{
			// Arrange
			var parser = new BunqParser();
			var fixture = new Fixture();
			var account = fixture
				.Build<Account>()
				.With(x => x.Balance, new Balance(Currency.EUR))
				.Create();
			var holdingsAndAccountsCollection = new TestHoldingsAndAccountsCollection(account);

			// Act
			await parser.ParseActivities("./TestFiles/Bunq/CashTransactions/single_interest.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateInterest(Currency.EUR, new DateTime(2023, 07, 27, 0, 0, 0, DateTimeKind.Utc), 3.5M, null)]
				);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestMultipleDepositsOn1Day_Converted()
		{
			// Arrange
			var parser = new BunqParser();
			var fixture = new Fixture();
			var account = fixture
				.Build<Account>()
				.With(x => x.Balance, new Balance(Currency.EUR))
				.Create();
			var holdingsAndAccountsCollection = new TestHoldingsAndAccountsCollection(account);

			// Act
			await parser.ParseActivities("./TestFiles/Bunq/CashTransactions/multiple_deposits.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateCashDeposit(Currency.EUR, new DateTime(2023, 07, 20, 0, 0, 0, DateTimeKind.Utc), 1000, null),
					PartialActivity.CreateCashDeposit(Currency.EUR, new DateTime(2023, 07, 20, 0, 0, 0, DateTimeKind.Utc), 1000, null),
					PartialActivity.CreateCashDeposit(Currency.EUR, new DateTime(2023, 07, 20, 0, 0, 0, DateTimeKind.Utc), 1000, null)]
				);
		}
	}
}