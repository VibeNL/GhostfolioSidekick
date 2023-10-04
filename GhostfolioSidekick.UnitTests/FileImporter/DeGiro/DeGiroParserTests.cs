using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.DeGiro;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.DeGiro
{
	public class DeGiroParserTests
	{
		readonly Mock<IGhostfolioAPI> api;

		public DeGiroParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanParseActivities_TestFileSingleOrder_True()
		{
			// Arrange
			var parser = new DeGiroParser(api.Object);

			// Act
			var canParse = await parser.CanParseActivities(new[] { "./FileImporter/TestFiles/DeGiro/Example1/TestFileSingleOrder.csv" });

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task CanParseActivities_TestFileMissingField_False()
		{
			// Arrange
			var parser = new DeGiroParser(api.Object);

			// Act
			var canParse = await parser.CanParseActivities(new[] { "./FileImporter/TestFiles/DeGiro/Example2/TestFileMissingField.csv" });

			// Assert
			canParse.Should().BeFalse();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleOrder_Converted()
		{
			// Arrange
			var parser = new DeGiroParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.EUR).Create();
			var account = fixture.Create<Model.Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("IE00B3XXRP09", null)).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/DeGiro/Example1/TestFileSingleOrder.csv" });

			// Assert
			account.Activities.Should().BeEquivalentTo(new[] { new Model.Activity {
				Asset = asset,
				Comment = "Transaction Reference: [b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5b]",
				Date = new DateTime(2023,07,6, 9, 39,0, DateTimeKind.Utc),
				Fee = new Money(DefaultCurrency.EUR, 1),
				Quantity = 1,
				Type = Model.ActivityType.Buy,
				UnitPrice = new Money(DefaultCurrency.EUR, 77.30M),
				ReferenceCode = "b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5b"
			} });
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileMultipleOrders_Converted()
		{
			// Arrange
			var parser = new DeGiroParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.EUR).Create();
			var asset2 = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.EUR).Create();

			var account = fixture.Create<Model.Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("IE00B3XXRP09", null)).ReturnsAsync(asset1);
			api.Setup(x => x.FindSymbolByISIN("NL0009690239", null)).ReturnsAsync(asset2);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/DeGiro/Example3/TestFileMultipleOrders.csv" });

			// Assert
			account.Activities.Should().BeEquivalentTo(new[]
			{ new Model.Activity {
				Asset = asset1,
				Comment = "Transaction Reference: [b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5b]",
				Date = new DateTime(2023,07,6,9,39,0, DateTimeKind.Utc),
				Fee = new Money(DefaultCurrency.EUR, 1),
				Quantity = 1,
				Type = Model.ActivityType.Buy,
				UnitPrice = new Money(DefaultCurrency.EUR, 77.30M),
				ReferenceCode = "b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5b"
			}, new Model.Activity {
				Asset = asset2,
				Comment = "Transaction Reference: [67e39ca1-2f10-4f82-8365-1baad98c398f]",
				Date = new DateTime(2023,07,11, 9,33,0, DateTimeKind.Utc),
				Fee = new Money(DefaultCurrency.EUR, 1),
				Quantity = 29,
				Type = Model.ActivityType.Buy,
				UnitPrice = new Money(DefaultCurrency.EUR, 34.375M),
				ReferenceCode = "67e39ca1-2f10-4f82-8365-1baad98c398f"
			} });
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileDividend_WithTax_Converted()
		{
			// Arrange
			var parser = new DeGiroParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.EUR).Create();
			var account = fixture.Create<Model.Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("NL0009690239", null)).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/DeGiro/Example4/TestFileDividend.csv" });

			// Assert
			account.Activities.Should().BeEquivalentTo(new[] { new Model.Activity {
				Asset = asset,
				Comment = "Transaction Reference: [Dividend_14-09-2023_06:32_NL0009690239]",
				Date = new DateTime(2023,09,14,6, 32,0, DateTimeKind.Utc),
				Fee = new Money(DefaultCurrency.EUR, 0),
				Quantity = 1,
				Type = Model.ActivityType.Dividend,
				UnitPrice = new Money(DefaultCurrency.EUR, 8.13M),
				ReferenceCode = "Dividend_14-09-2023_06:32_NL0009690239"
			} });
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileDividend_NoTax_Converted()
		{
			// Arrange
			var parser = new DeGiroParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.EUR).Create();
			var account = fixture.Create<Model.Account>();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("NL0009690239", null)).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/DeGiro/Example5/TestFileDividendNoTax.csv" });

			// Assert
			account.Activities.Should().BeEquivalentTo(new[] { new Model.Activity {
				Asset = asset,
				Comment = "Transaction Reference: [Dividend_14-09-2023_06:32_NL0009690239]",
				Date = new DateTime(2023,09,14,6, 32,0, DateTimeKind.Utc),
				Fee = new Money(DefaultCurrency.EUR, 0),
				Quantity = 1,
				Type = Model.ActivityType.Dividend,
				UnitPrice = new Money(DefaultCurrency.EUR, 9.57M),
				ReferenceCode = "Dividend_14-09-2023_06:32_NL0009690239"
			} });
		}
	}
}