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
				var canParse = await parser.CanParseActivities(file);

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
			var activities = await parser.ConvertToActivities("./FileImporter/TestFiles/ScalableCapital/CashTransactions/single_known_saldo.csv", account.Balance);

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 21.68M, new DateTime(2023, 04, 11, 00, 00, 00, DateTimeKind.Utc)));
			activities.Should().BeEmpty();
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
			api.Setup(x => x.FindSymbolByIdentifier("US92343V1044", It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset);

			// Act
			var activities = await parser.ConvertToActivities("./FileImporter/TestFiles/ScalableCapital/CashTransactions/single_dividend.csv", account.Balance);

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 7.0799999999999999999999999998M, new DateTime(2023, 08, 1, 0, 0, 0, DateTimeKind.Utc)));
			activities.Should().BeEquivalentTo(new[] { new Activity
			(
				ActivityType.Dividend,
				asset,
				new DateTime(2023,8,1, 0,0,0, DateTimeKind.Utc),
				14,
				new Money(DefaultCurrency.EUR, 0.5057142857142857142857142857M, new DateTime(2023,8,1, 0,0,0, DateTimeKind.Utc)),
				Enumerable.Empty < Money >(),
				"Transaction Reference: [WWEK 16100100] (Details: asset ISIN US92343V1044)",
				"WWEK 16100100")
			});
		}

		[Fact]
		public void CanParseActivities_SingleBuy_CorrectlyParsed()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<SymbolProfile>().With(x => x.Currency, DefaultCurrency.EUR).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("IE00077FRP95", It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset);

			// Act
			var activities = new Activity[0];/*await parser.ConvertToActivities(account.Name, new[] {
				"./FileImporter/TestFiles/ScalableCapital/BuyOrders/SingleBuy/rkk.csv",
				"./FileImporter/TestFiles/ScalableCapital/BuyOrders/SingleBuy/wum.csv"
			});*/

			// Assert
			activities.Should().BeEquivalentTo(new[] { new Activity
				(
					ActivityType.Buy,
					asset,
					new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc),
					5,
					new Money(DefaultCurrency.EUR, 8.685M, new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc)),
					new[] { new Money(DefaultCurrency.EUR, 0.99M, new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc)) },
					"Transaction Reference: [SCALQbWiZnN9DtQ] (Details: asset IE00077FRP95)",
					"SCALQbWiZnN9DtQ"
				)
			});
		}

		[Fact]
		public void CanParseActivities_SingleSell_CorrectlyParsed()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<SymbolProfile>().With(x => x.Currency, DefaultCurrency.EUR).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("IE00077FRP95", It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset);

			// Act
			var activities = new Activity[0];/*await parser.ConvertToActivities(account.Name, new[] {
				"./FileImporter/TestFiles/ScalableCapital/SellOrders/SingleSell/rkk.csv",
				"./FileImporter/TestFiles/ScalableCapital/SellOrders/SingleSell/wum.csv"
			});*/

			// Assert
			activities.Should().BeEquivalentTo(new[] { new Activity(
				ActivityType.Sell,
				asset,
				new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc),
				5,
				new Money(DefaultCurrency.EUR, 8.685M, new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc)),
				new[] { new Money(DefaultCurrency.EUR, 0.99M, new DateTime(2023,8,3, 14,43,17, 650, DateTimeKind.Utc)) },
				"Transaction Reference: [SCALQbWiZnN9DtQ] (Details: asset IE00077FRP95)",
				"SCALQbWiZnN9DtQ"
				)
			});
		}
	}
}
