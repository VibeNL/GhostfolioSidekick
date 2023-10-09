using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.ScalableCaptial;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.ScalableCapital
{
	public class ScalableCapitalParserTests
	{
		readonly Mock<IGhostfolioAPI> api;

		public ScalableCapitalParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanParseActivities_WUMExample1_True()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);

			// Act
			var canParse = await parser.CanParseActivities(new[] { "./FileImporter/TestFiles/ScalableCapital/Example1/WUMExample1.csv" });

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task CanParseActivities_RKKExample1_True()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);

			// Act
			var canParse = await parser.CanParseActivities(new[] { "./FileImporter/TestFiles/ScalableCapital/Example1/RKKExample1.csv" });

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_Example1_OrderOnly()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.EUR).Create();
			var account = fixture.Build<Model.Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("IE00077FRP95", null)).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/ScalableCapital/Example1/WUMExample1.csv" });

			// Assert
			account.Activities.Should().BeEquivalentTo(new[] { new Model.Activity {
				Asset = asset,
				Comment = "Transaction Reference: [SCALQbWiZnN9DtQ]",
				Date = new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc),
				Fee = null,
				Quantity = 5,
				ActivityType = Model.ActivityType.Buy,
				UnitPrice = new Money(DefaultCurrency.EUR, 8.685M, new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc)),
				ReferenceCode = "SCALQbWiZnN9DtQ"
			} });
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_Example1_DividendOnly()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.EUR).Create();
			var account = fixture.Build<Model.Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("US92343V1044", null)).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/ScalableCapital/Example1/RKKExample1.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 696.85M, new DateTime(2023, 08, 2, 0, 0, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] { new Model.Activity {
				Asset = asset,
				Comment = "Transaction Reference: [WWEK 16100100]",
				Date = new DateTime(2023,8,1, 0,0,0, DateTimeKind.Utc),
				Fee = null,
				Quantity = 14,
				ActivityType = Model.ActivityType.Dividend,
				UnitPrice = new Money(DefaultCurrency.EUR, 0.5057142857142857142857142857M, new DateTime(2023,8,1, 0,0,0, DateTimeKind.Utc)),
				ReferenceCode = "WWEK 16100100"
			} });
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_Example1_Both()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.EUR).Create();
			var asset2 = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.EUR).Create();
			var account = fixture.Build<Model.Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("IE00077FRP95", null)).ReturnsAsync(asset1);
			api.Setup(x => x.FindSymbolByISIN("US92343V1044", null)).ReturnsAsync(asset2);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] {
				"./FileImporter/TestFiles/ScalableCapital/Example1/WUMExample1.csv",
				"./FileImporter/TestFiles/ScalableCapital/Example1/RKKExample1.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 696.85M, new DateTime(2023, 08, 2, 0, 0, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] {
			new Model.Activity {
				Asset = asset1,
				Comment = "Transaction Reference: [SCALQbWiZnN9DtQ]",
				Date = new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc),
				Fee = new Money(DefaultCurrency.EUR, 0.99M, new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc)),
				Quantity = 5,
				ActivityType = Model.ActivityType.Buy,
				UnitPrice = new Money(DefaultCurrency.EUR, 8.685M, new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc)),
				ReferenceCode = "SCALQbWiZnN9DtQ"
			},
			new Model.Activity {
				Asset = asset2,
				Comment = "Transaction Reference: [WWEK 16100100]",
				Date = new DateTime(2023,8,1, 0,0,0, DateTimeKind.Utc),
				Fee =  null,
				Quantity = 14,
				ActivityType = Model.ActivityType.Dividend,
				UnitPrice = new Money(DefaultCurrency.EUR, 0.5057142857142857142857142857M, new DateTime(2023,8,1, 0,0,0, DateTimeKind.Utc)),
				ReferenceCode = "WWEK 16100100"
			} });
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_Example2_NotDuplicateFeesAndDividend()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.EUR).Create();
			var asset2 = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.EUR).Create();
			var account = fixture.Build<Model.Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("IE00077FRP95", null)).ReturnsAsync(asset1);
			api.Setup(x => x.FindSymbolByISIN("US92343V1044", null)).ReturnsAsync(asset2);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] {
				"./FileImporter/TestFiles/ScalableCapital/Example2/WUMExample1.csv",
				"./FileImporter/TestFiles/ScalableCapital/Example2/RKKExample1.csv",
				"./FileImporter/TestFiles/ScalableCapital/Example2/RKKExample2.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 696.85M, new DateTime(2023, 08, 2, 0, 0, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] {
			new Model.Activity {
				Asset = asset1,
				Comment = "Transaction Reference: [SCALQbWiZnN9DtQ]",
				Date = new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc),
				Fee = new Money(DefaultCurrency.EUR, 0.99M, new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc)),
				Quantity = 5,
				ActivityType = Model.ActivityType.Buy,
				UnitPrice = new Money(DefaultCurrency.EUR, 8.685M, new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc)),
				ReferenceCode = "SCALQbWiZnN9DtQ"
			},
			new Model.Activity {
				Asset = asset2,
				Comment = "Transaction Reference: [WWEK 16100100]",
				Date = new DateTime(2023,8,1, 0,0,0, DateTimeKind.Utc),
				Fee = null,
				Quantity = 14,
				ActivityType = Model.ActivityType.Dividend,
				UnitPrice = new Money(DefaultCurrency.EUR, 0.5057142857142857142857142857M, new DateTime(2023,8,1, 0,0,0, DateTimeKind.Utc)),
				ReferenceCode = "WWEK 16100100"
			} });
		}
	}
}
