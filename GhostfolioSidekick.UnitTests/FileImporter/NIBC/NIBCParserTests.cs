using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.NIBC;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.NIBC
{
	public class NIBCParserTests
	{
		readonly Mock<IGhostfolioAPI> api;

		public NIBCParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			var parser = new NIBCParser(api.Object);

			foreach (var file in Directory.GetFiles("./FileImporter/TestFiles/NIBC/", "*.csv", SearchOption.AllDirectories))
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
			var parser = new NIBCParser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			await parser.ConvertToActivities("./FileImporter/TestFiles/NIBC/CashTransactions/single_deposit.csv", account.Balance);

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 250M, new DateTime(2020, 01, 27, 0, 0, 0, DateTimeKind.Utc)));
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange
			var parser = new NIBCParser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			await parser.ConvertToActivities("./FileImporter/TestFiles/NIBC/CashTransactions/single_withdrawal.csv", account.Balance);

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, -10000M, new DateTime(2022, 01, 06, 0, 0, 0, DateTimeKind.Utc)));
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleInterest_Converted()
		{
			// Arrange
			var parser = new NIBCParser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			var activities = await parser.ConvertToActivities("./FileImporter/TestFiles/NIBC/CashTransactions/single_interest.csv", account.Balance);

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 0.51M, new DateTime(2021, 09, 30, 0, 0, 0, DateTimeKind.Utc)));
			activities.Should().BeEquivalentTo(new[] { new Activity(
				ActivityType.Interest,
				null,
				new DateTime(2021, 09, 30, 0,0,0, DateTimeKind.Utc),
				1m,
				new Money(DefaultCurrency.EUR, 0.51M, new DateTime(2021, 09, 30, 0,0,0, DateTimeKind.Utc)),
				Enumerable.Empty<Money>(),
				"Transaction Reference: [C1I30IN0000A000Q]",
				"C1I30IN0000A000Q"
				)
			});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBonusInterest_Converted()
		{
			// Arrange
			var parser = new NIBCParser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			var activities = await parser.ConvertToActivities("./FileImporter/TestFiles/NIBC/CashTransactions/single_bonus_interest.csv", account.Balance);

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 1.1M, new DateTime(2021, 6, 30, 0, 0, 0, DateTimeKind.Utc)));
			activities.Should().BeEquivalentTo(new[] { new Activity(
				ActivityType.Interest,
				null,
				new DateTime(2021,6,30, 0,0,0, DateTimeKind.Utc),
				1m,
				new Money(DefaultCurrency.EUR, 1.1M, new DateTime(2021,6,30, 0,0,0, DateTimeKind.Utc)),
				Enumerable.Empty<Money>(),
				"Transaction Reference: [C1F30IN0000A000QBonus]",
				"C1F30IN0000A000QBonus"
				)
			});
		}
	}
}