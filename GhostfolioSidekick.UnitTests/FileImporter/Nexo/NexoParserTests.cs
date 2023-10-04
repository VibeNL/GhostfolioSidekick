using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.Nexo;
using GhostfolioSidekick.Ghostfolio.API;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.Nexo
{
	public class NexoParserTests
	{
		readonly Mock<IGhostfolioAPI> api;

		public NexoParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanParseActivities_TestFileSingleOrder_True()
		{
			// Arrange
			var parser = new NexoParser(api.Object);

			// Act
			var canParse = await parser.CanParseActivities(new[] { "./FileImporter/TestFiles/Nexo/Example1/Example1.csv" });

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileMultipleOrders_ReferalPending_Converted()
		{
			// Arrange
			var parser = new NexoParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var asset2 = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Create<Model.Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("USDC", It.IsAny<Func<IEnumerable<Model.Asset>, Model.Asset>>())).ReturnsAsync(asset1);
			api.Setup(x => x.FindSymbolByISIN("BTC", It.IsAny<Func<IEnumerable<Model.Asset>, Model.Asset>>())).ReturnsAsync(asset2);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/Example1/Example1.csv" });

			// Assert
			account.Activities.Should().BeEquivalentTo(new[]
			{ new Model.Activity {
				Asset = asset1,
				Comment = "Transaction Reference: [NXTyPxhiopNL3]",
				Date = new DateTime(2023,8,25,14,44,46, DateTimeKind.Utc),
				Fee = null,
				Quantity = 161.90485771M,
				Type = Model.ActivityType.Buy,
				UnitPrice = new Model.Money(asset1.Currency, 0.999969996514813032906620872M),
				ReferenceCode = "NXTyPxhiopNL3"
			}, new Model.Activity {
				Asset = asset2,
				Comment = "Transaction Reference: [NXTyVJeCwg6Og]",
				Date = new DateTime(2023,8,26, 13,30,38, DateTimeKind.Utc),
				Quantity = 0.00445142M,
				Type = Model.ActivityType.Buy,
				UnitPrice = new Model.Money(asset1.Currency, 26028.386478921332967906870167M),
				ReferenceCode = "NXTyVJeCwg6Og"
			} });
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileMultipleOrders_ReferalApproved_Converted()
		{
			// Arrange
			var parser = new NexoParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var asset2 = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Create<Model.Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("USDC", It.IsAny<Func<IEnumerable<Model.Asset>, Model.Asset>>())).ReturnsAsync(asset1);
			api.Setup(x => x.FindSymbolByISIN("BTC", It.IsAny<Func<IEnumerable<Model.Asset>, Model.Asset>>())).ReturnsAsync(asset2);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/Example2/Example2.csv" });

			// Assert
			account.Activities.Should().BeEquivalentTo(new[]
			{ new Model.Activity {
				Asset = asset1,
				Comment = "Transaction Reference: [NXTyPxhiopNL3]",
				Date = new DateTime(2023,8,25,14,44,46, DateTimeKind.Utc),
				Fee = null,
				Quantity = 161.90485771M,
				Type = Model.ActivityType.Buy,
				UnitPrice = new Model.Money(asset1.Currency, 0.999969996514813032906620872M),
				ReferenceCode = "NXTyPxhiopNL3"
			} , new Model.Activity {
				Asset = asset2,
				Comment = "Transaction Reference: [NXTk6FBYyxOqH]",
				Date = new DateTime(2023,08,25,16,43,55, DateTimeKind.Utc),
				Fee = null,
				Quantity = 0.00096332M,
				Type = Model.ActivityType.Buy,
				UnitPrice = new Model.Money(asset1.Currency, 25951.855302495536270398206204M),
				ReferenceCode = "NXTk6FBYyxOqH"
			} , new Model.Activity {
				Asset = asset2,
				Comment = "Transaction Reference: [NXTyVJeCwg6Og]",
				Date = new DateTime(2023,8,26, 13,30,38, DateTimeKind.Utc),
				Fee = null,
				Quantity = 0.00445142M,
				Type = Model.ActivityType.Buy,
				UnitPrice = new Model.Money(asset1.Currency, 26028.386478921332967906870167M),
				ReferenceCode = "NXTyVJeCwg6Og"
			} });
		}
	}
}