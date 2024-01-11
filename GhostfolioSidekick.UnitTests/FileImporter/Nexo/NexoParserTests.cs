using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.FileImporter.Nexo;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.Nexo
{
	public class NexoParserTests
	{
		readonly Mock<IGhostfolioAPI> api;
		private readonly Mock<IApplicationSettings> cs;

		public NexoParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
			cs = new Mock<IApplicationSettings>();
			cs.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance());
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			var parser = new NexoParser(cs.Object, api.Object);

			foreach (var file in Directory.GetFiles("./FileImporter/TestFiles/Nexo/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(new[] { file });

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange
			var parser = new NexoParser(cs.Object, api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/CashTransactions/single_deposit.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 150, new DateTime(2023, 08, 25, 14, 44, 44, DateTimeKind.Utc)));
			account.Activities.Should().BeEmpty();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuy_Converted()
		{
			// Arrange
			var parser = new NexoParser(cs.Object, api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Model.SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier(new string[] { "USD Coin", "USDC" }, It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset1);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/BuyOrders/single_buy.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, -161.9M, new DateTime(2023, 08, 25, 14, 44, 46, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[]
			{ new Activity(
				ActivityType.Buy,
				asset1,
				new DateTime(2023, 08, 25, 14, 44, 46, DateTimeKind.Utc),
				161.90485771M,
				new Money(asset1.Currency, 0.999969996514813032906620872M, new DateTime(2023, 08, 25, 14, 44, 46, DateTimeKind.Utc)),
				Enumerable.Empty<Money>(),
				"Transaction Reference: [NXTyPxhiopNL3] (Details: asset USDC)",
				"NXTyPxhiopNL3"
				)
			});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleConvert_Converted()
		{
			// Arrange
			var parser = new NexoParser(cs.Object, api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Model.SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var asset2 = fixture.Build<Model.SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier(new string[] { "USD Coin", "USDC" }, It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset1);
			api.Setup(x => x.FindSymbolByIdentifier(new string[] { "Bitcoin", "BTC" }, It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset2);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/BuyOrders/single_convert.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 0M, new DateTime(2023, 10, 08, 19, 54, 20, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[]
			{ new Activity(
				ActivityType.Sell,
				asset1,
				new DateTime(2023, 10, 08, 19, 54, 20, DateTimeKind.Utc),
				200M,
				new Money(asset1.Currency, 0.9988M, new DateTime(2023, 10, 08, 19, 54, 20, DateTimeKind.Utc)),
				Enumerable.Empty<Money>(),
				 "Transaction Reference: [NXTVDI4DJFWqB63pTcCuTpgc] (Details: asset USDC)",
				 "NXTVDI4DJFWqB63pTcCuTpgc"
				),
			new Activity(
				ActivityType.Buy,
				asset2,
				new DateTime(2023, 10, 08, 19, 54, 20, DateTimeKind.Utc),
				0.00716057M,
				new Money(asset2.Currency, 27897.220472671868300987211912M, new DateTime(2023, 10, 08, 19, 54, 20, DateTimeKind.Utc)),
				Enumerable.Empty<Money>(),
				"Transaction Reference: [NXTVDI4DJFWqB63pTcCuTpgc_2] (Details: asset BTC)",
				"NXTVDI4DJFWqB63pTcCuTpgc_2"
				)
			});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleCashbackCrypto_Converted()
		{
			// Arrange
			var parser = new NexoParser(cs.Object, api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier(new string[] { "Bitcoin", "BTC" }, It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/Specials/single_cashback_crypto.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 0M, new DateTime(0001, 01, 01, 00, 00, 00, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[]
			{ new Activity(
				ActivityType.Receive,
				asset,
				new DateTime(2023,10,12,10,44,32, DateTimeKind.Utc),
				0.00000040M,
				new Money(asset.Currency, 26811.1M, new DateTime(2023,10,12,10,44,32, DateTimeKind.Utc)),
				Enumerable.Empty < Money >(),
				"Transaction Reference: [NXT2yQdOutpLLE1Lz51xXt6uW] (Details: asset BTC)",
				"NXT2yQdOutpLLE1Lz51xXt6uW"
				)
			});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleCashbackFiat_Converted()
		{
			// Arrange
			var parser = new NexoParser(cs.Object, api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/Specials/single_cashback_fiat.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 0M, new DateTime(0001, 01, 01, 00, 00, 00, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[]
			{ new Activity(
				ActivityType.Receive,
				null,
				new DateTime(2023,10,8,20,5,12, DateTimeKind.Utc),
				0.06548358M,
				new Money(DefaultCurrency.EUR, 1M, new DateTime(2023,10,8,20,5,12, DateTimeKind.Utc)),
				Enumerable.Empty < Money >(),
				"Transaction Reference: [NXT6asbYnZqniNoTss0nyuIxM] (Details: asset EURX)",
				"NXT6asbYnZqniNoTss0nyuIxM"
				)
			});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleReferralBonusPending_Converted()
		{
			// Arrange
			var parser = new NexoParser(cs.Object, api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Model.SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("BTC", It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset1);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/Specials/single_referralbonus_pending.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 0, new DateTime(0001, 01, 01, 0, 0, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEmpty();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleReferralBonusApproved_Converted()
		{
			// Arrange
			var parser = new NexoParser(cs.Object, api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Model.SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier(new string[] { "Bitcoin", "BTC" }, It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset1);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/Specials/single_referralbonus_approved.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 0, new DateTime(0001, 01, 01, 0, 0, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[]
			{ new Activity(
				ActivityType.Receive,
				asset1,
				new DateTime(2023,08,25,16,43,55, DateTimeKind.Utc),
				0.00096332M,
				new Money(asset1.Currency, 25951.855302495536270398206204M, new DateTime(2023,08,25,16,43,55, DateTimeKind.Utc)),
				Enumerable.Empty < Money >(),
				"Transaction Reference: [NXTk6FBYyxOqH] (Details: asset BTC)",
				"NXTk6FBYyxOqH"
				)
			});
		}
	}
}