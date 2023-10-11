using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.Bunq;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.Bunq
{
	public class BunqParserTests
	{
		readonly Mock<IGhostfolioAPI> api;

		public BunqParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanParseActivities_TestFileSingleOrder_True()
		{
			// Arrange
			var parser = new BunqParser(api.Object);

			// Act
			var canParse = await parser.CanParseActivities(new[] { "./FileImporter/TestFiles/Bunq/Example1/Example1.csv" });

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileLifecycle_Converted()
		{
			// Arrange
			var parser = new BunqParser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Bunq/Example1/Example1.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 903.5M, new DateTime(2023, 08, 18, 0, 0, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] { new Activity {
				Asset = null,
				Comment = "Transaction Reference: [Interest_2023-07-27]",
				Date = new DateTime(2023,07,27, 0,0,0, DateTimeKind.Utc),
				Fee = null,
				Quantity = 1m,
				ActivityType = ActivityType.Interest,
				UnitPrice = new Money(DefaultCurrency.EUR, 3.5M, new DateTime(2023,7,27, 0,0,0, DateTimeKind.Utc)),
				ReferenceCode = "Interest_2023-07-27"
			} });
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
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Bunq/Example2/Example2.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 3000M, new DateTime(2023, 07, 20, 0, 0, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEmpty();
		}
	}
}