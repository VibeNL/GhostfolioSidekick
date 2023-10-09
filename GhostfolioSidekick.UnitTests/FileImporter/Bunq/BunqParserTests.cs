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

			var account = fixture.Build<Model.Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Bunq/Example1/Example1.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 1015.26M, new DateTime(2023, 08, 24, 0, 0, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEmpty();
		}
	}
}