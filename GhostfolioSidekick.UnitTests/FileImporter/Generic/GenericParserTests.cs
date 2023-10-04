using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.Generic;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.Generic
{
	public class GenericParserTests
	{
		readonly Mock<IGhostfolioAPI> api;

		public GenericParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanParseActivities_TestFileSingleOrder_True()
		{
			// Arrange
			var parser = new GenericParser(api.Object);

			// Act
			var canParse = await parser.CanParseActivities(new[] { "./FileImporter/TestFiles/Generic/Example1/Example1.csv" });

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleOrder_Converted()
		{
			// Arrange
			var parser = new GenericParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var account = fixture.Create<Model.Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("US67066G1040", null)).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Generic/Example1/Example1.csv" });

			// Assert
			account.Activities.Should().BeEquivalentTo(new[] { new Model.Activity {
				Asset = asset,
				Comment = "Transaction Reference: [Buy_US67066G1040_2023-08-07]",
				Date = new DateTime(2023,08,7, 0,0,0, DateTimeKind.Utc),
				Fee = new Money(DefaultCurrency.USD, 0.02M),
				Quantity = 0.0267001M,
				Type = Model.ActivityType.Buy,
				UnitPrice = new Money(DefaultCurrency.USD, 453.33M),
				ReferenceCode = "Buy_US67066G1040_2023-08-07"
			} });
		}
	}
}