using AutoFixture;
using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.ScalableCaptial;

namespace GhostfolioSidekick.Parsers.UnitTests.ScalableCapital
{
	public class ScalableCapitalWUMParserTests
	{
		private readonly ScalableCapitalWUMParser parser;
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		public ScalableCapitalWUMParserTests()
		{
			parser = new ScalableCapitalWUMParser(DummyCurrencyMapper.Instance);

			var fixture = CustomFixture.New();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, [new Balance(DateOnly.FromDateTime(DateTime.Today), new Money(Currency.EUR, 0))])
				.Create();
			activityManager = new TestActivityManager();
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/ScalableCapital/BaaderBank/", "wum.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ParseActivities_SingleBuy_CorrectlyParsed()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/BaaderBank/BuyOrders/SingleBuy/wum.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 8, 3, 14, 43, 17, 650, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("IE00077FRP95")],
						5,
						new Money(Currency.EUR, 8.685M),
						new Money(Currency.EUR, 43.43M),
						"SCALQbWiZnN9DtQ")
				]);
		}

		[Fact]
		public async Task ParseActivities_SingleSell_CorrectlyParsed()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/BaaderBank/SellOrders/SingleSell/wum.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSell(
						Currency.EUR,
						new DateTime(2023, 8, 3, 14, 43, 17, 650, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("IE00077FRP95")],
						5,
						new Money(Currency.EUR, 8.685M),
						new Money(Currency.EUR, 43.43M),
						"SCALQbWiZnN9DtQ")
				]);
		}

		[Fact]
		public async Task ParseActivities_Invalid_ThrowsException()
		{
			// Arrange

			// Act
			Func<Task> a = () => parser.ParseActivities("./TestFiles/ScalableCapital/BaaderBank/Invalid/invalid_action.csv", activityManager, account.Name);

			// Assert
			await a.Should().ThrowAsync<NotSupportedException>();
		}
	}
}
