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
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);

			foreach (var file in Directory.GetFiles("./FileImporter/TestFiles/ScalableCapital/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(new[] { file });

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task CanParseActivities_SingleKnownSaldo_CorrectlyParsed()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] {
				"./FileImporter/TestFiles/ScalableCapital/CashTransactions/single_known_saldo.csv"
			});

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 21.68M, new DateTime(2023, 04, 11, 00, 00, 00, DateTimeKind.Utc)));
			account.Activities.Should().BeEmpty();
		}

		[Fact]
		public async Task CanParseActivities_SingleKnownDividend_CorrectlyParsed()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<SymbolProfile>().With(x => x.Currency, DefaultCurrency.EUR).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("US92343V1044", It.IsAny<Currency>(), It.IsAny<AssetClass?[]>(), It.IsAny<AssetSubClass?[]>())).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] {
				"./FileImporter/TestFiles/ScalableCapital/CashTransactions/single_dividend.csv"
			});

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 7.0799999999999999999999999998M, new DateTime(2023, 08, 1, 0, 0, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] { new Activity {
				Asset = asset,
				Comment = "Transaction Reference: [WWEK 16100100] (Details: asset ISIN US92343V1044)",
				Date = new DateTime(2023,8,1, 0,0,0, DateTimeKind.Utc),
				Fees = Enumerable.Empty < Money >(),
				Quantity = 14,
				ActivityType = ActivityType.Dividend,
				UnitPrice = new Money(DefaultCurrency.EUR, 0.5057142857142857142857142857M, new DateTime(2023,8,1, 0,0,0, DateTimeKind.Utc)),
				ReferenceCode = "WWEK 16100100"
			} });
		}

		[Fact]
		public async Task CanParseActivities_SingleBuy_CorrectlyParsed()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<SymbolProfile>().With(x => x.Currency, DefaultCurrency.EUR).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("IE00077FRP95", It.IsAny<Currency>(), It.IsAny<AssetClass?[]>(), It.IsAny<AssetSubClass?[]>())).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] {
				"./FileImporter/TestFiles/ScalableCapital/BuyOrders/SingleBuy/rkk.csv",
				"./FileImporter/TestFiles/ScalableCapital/BuyOrders/SingleBuy/wum.csv"
			});

			// Assert
			account.Activities.Should().BeEquivalentTo(new[] { new Activity {
				Asset = asset,
				Comment = "Transaction Reference: [SCALQbWiZnN9DtQ] (Details: asset IE00077FRP95)",
				Date = new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc),
				Fees = new[] { new Money(DefaultCurrency.EUR, 0.99M, new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc)) },
				Quantity = 5,
				ActivityType = ActivityType.Buy,
				UnitPrice = new Money(DefaultCurrency.EUR, 8.685M, new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc)),
				ReferenceCode = "SCALQbWiZnN9DtQ"
			} });
		}

		[Fact]
		public async Task CanParseActivities_SingleSell_CorrectlyParsed()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<SymbolProfile>().With(x => x.Currency, DefaultCurrency.EUR).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("IE00077FRP95", It.IsAny<Currency>(), It.IsAny<AssetClass?[]>(), It.IsAny<AssetSubClass?[]>())).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] {
				"./FileImporter/TestFiles/ScalableCapital/SellOrders/SingleSell/rkk.csv",
				"./FileImporter/TestFiles/ScalableCapital/SellOrders/SingleSell/wum.csv"
			});

			// Assert
			account.Activities.Should().BeEquivalentTo(new[] { new Activity {
				Asset = asset,
				Comment = "Transaction Reference: [SCALQbWiZnN9DtQ] (Details: asset IE00077FRP95)",
				Date = new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc),
				Fees = new[] { new Money(DefaultCurrency.EUR, 0.99M, new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc)) },
				Quantity = 5,
				ActivityType = ActivityType.Sell,
				UnitPrice = new Money(DefaultCurrency.EUR, 8.685M, new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc)),
				ReferenceCode = "SCALQbWiZnN9DtQ"
			} });
		}
	}
}
