using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.DeGiro;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.DeGiro
{
	public class DeGiroParserPTTests
	{
		readonly Mock<IGhostfolioAPI> api;

		public DeGiroParserPTTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			var parser = new DeGiroParserPT(api.Object);

			foreach (var file in Directory.GetFiles("./FileImporter/TestFiles/DeGiro/PT/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(new[] { file });

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyEuro_Converted()
		{
			// Arrange
			var parser = new DeGiroParserPT(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.SymbolProfile>().With(x => x.Currency, DefaultCurrency.EUR).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("IE00B3XXRP09", It.IsAny<Currency>(), It.IsAny<AssetClass?[]>(),
				It.IsAny<AssetSubClass?[]>(), true)).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name,
				new[] { "./FileImporter/TestFiles/DeGiro/PT/BuyOrders/single_buy_euro.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR,
				21.70M, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[]
			{
				new Activity
				{
					Asset = asset,
					Comment =
						"Transaction Reference: [b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a] (Details: asset IE00B3XXRP09)",
					Date = new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc),
					Fees = new[] { new Money(DefaultCurrency.EUR, 1, new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc)) },
					Quantity = 1,
					ActivityType = ActivityType.Buy,
					UnitPrice = new Money(DefaultCurrency.EUR, 77.30M,
						new DateTime(2023, 07, 6, 9, 39, 0, DateTimeKind.Utc)),
					ReferenceCode = "b7ab0494-1b46-4e2f-9bd2-f79e6c87cb5a"
				}
			});
		}
	}
}