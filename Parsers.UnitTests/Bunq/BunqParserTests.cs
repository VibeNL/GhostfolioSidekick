using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.Bunq;
using Moq;

namespace Parsers.UnitTests.Bunq
{
	public class BunqParserTests
	{
		private string accountName = "ABC";
		private Account account;

		public BunqParserTests()
		{
			var fixture = new Fixture();
			account = fixture.Build<Account>().With(x => x.Balance, new Balance(Currency.EUR)).Create();
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			var parser = new BunqParser();

			foreach (var file in Directory.GetFiles("./FileImporter/TestFiles/Bunq/", "*.csv", SearchOption.AllDirectories))
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
			var holdingsAndAccountsCollection = new TestHoldingsAndAccountsCollection(account);

			// Act
			await parser.ParseActivities("./FileImporter/TestFiles/Bunq/CashTransactions/single_deposit.csv", holdingsAndAccountsCollection, accountName);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				new PartialActivity()
				{

				}
				);

			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 1000M, new DateTime(2023, 07, 20, 0, 0, 0, DateTimeKind.Utc)));
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange
			var parser = new BunqParser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Bunq/CashTransactions/single_withdrawal.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, -100M, new DateTime(2023, 07, 20, 0, 0, 0, DateTimeKind.Utc)));
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleInterest_Converted()
		{
			// Arrange
			var parser = new BunqParser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Bunq/CashTransactions/single_interest.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 3.5M, new DateTime(2023, 07, 27, 0, 0, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] { new Activity(
				ActivityType.Interest,
				null,
				new DateTime(2023,07,27, 0,0,0, DateTimeKind.Utc),
				1m,
				new Money(DefaultCurrency.EUR, 3.5M, new DateTime(2023,7,27, 0,0,0, DateTimeKind.Utc)),
				Enumerable.Empty<Money>(),
				"Transaction Reference: [Interest_2023-07-27]",
				"Interest_2023-07-27"
				)
			});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestMultipleDepositsOn1Day_Converted()
		{
			// Arrange
			var parser = new BunqParser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Bunq/CashTransactions/multiple_deposits.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 3000M, new DateTime(2023, 07, 20, 0, 0, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEmpty();
		}
	}
}