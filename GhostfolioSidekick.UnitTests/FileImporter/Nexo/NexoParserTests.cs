using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.Nexo;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
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
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			var parser = new NexoParser(api.Object);

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
			var parser = new NexoParser(api.Object);
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
			var parser = new NexoParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("USDC", It.IsAny<Currency>(), It.IsAny<AssetClass?[]>(), It.IsAny<AssetSubClass?[]>())).ReturnsAsync(asset1);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/BuyORders/single_buy.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, -161.9M, new DateTime(2023, 08, 25, 14, 44, 46, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[]
			{ new Activity {
				Asset = asset1,
				Comment = "Transaction Reference: [NXTyPxhiopNL3] (Details: asset USDC)",
				Date = new DateTime(2023, 08, 25, 14, 44, 46, DateTimeKind.Utc),
				Fee = null,
				Quantity = 161.90485771M,
				ActivityType = ActivityType.Buy,
				UnitPrice = new Money(asset1.Currency, 0.999969996514813032906620872M, new DateTime(2023, 08, 25, 14, 44, 46, DateTimeKind.Utc)),
				ReferenceCode = "NXTyPxhiopNL3"
			}});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleConvert_Converted()
		{
			// Arrange
			var parser = new NexoParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var asset2 = fixture.Build<Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("USDC", It.IsAny<Currency>(), It.IsAny<AssetClass?[]>(), It.IsAny<AssetSubClass?[]>())).ReturnsAsync(asset1);
			api.Setup(x => x.FindSymbolByIdentifier("BTC", It.IsAny<Currency>(), It.IsAny<AssetClass?[]>(), It.IsAny<AssetSubClass?[]>())).ReturnsAsync(asset2);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/BuyOrders/single_convert.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 0M, new DateTime(2023, 10, 08, 19, 54, 20, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[]
			{ new Activity {
				Asset = asset1,
				Comment = "Transaction Reference: [NXTVDI4DJFWqB63pTcCuTpgc] (Details: asset USDC)",
				Date = new DateTime(2023, 10, 08, 19, 54, 20, DateTimeKind.Utc),
				Fee = null,
				Quantity = 200M,
				ActivityType = ActivityType.Sell,
				UnitPrice = new Money(asset1.Currency, 0.9988M, new DateTime(2023, 10, 08, 19, 54, 20, DateTimeKind.Utc)),
				ReferenceCode = "NXTVDI4DJFWqB63pTcCuTpgc"
			},
			new Activity {
				Asset = asset2,
				Comment = "Transaction Reference: [NXTVDI4DJFWqB63pTcCuTpgc_2] (Details: asset BTC)",
				Date = new DateTime(2023, 10, 08, 19, 54, 20, DateTimeKind.Utc),
				Fee = null,
				Quantity = 0.00716057M,
				ActivityType = ActivityType.Buy,
				UnitPrice = new Money(asset2.Currency, 27897.220472671868300987211912M, new DateTime(2023, 10, 08, 19, 54, 20, DateTimeKind.Utc)),
				ReferenceCode = "NXTVDI4DJFWqB63pTcCuTpgc_2"
			}});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleCashbackCrypto_Converted()
		{
			// Arrange
			var parser = new NexoParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("BTC", It.IsAny<Currency>(), It.IsAny<AssetClass?[]>(), It.IsAny<AssetSubClass?[]>())).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/Specials/single_cashback_crypto.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 0M, new DateTime(0001, 01, 01, 00, 00, 00, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[]
			{ new Activity {
				Asset = asset,
				Comment = "Transaction Reference: [NXT2yQdOutpLLE1Lz51xXt6uW] (Details: asset BTC)",
				Date = new DateTime(2023,10,12,10,44,32, DateTimeKind.Utc),
				Fee = null,
				Quantity = 0.00000040M,
				ActivityType = ActivityType.Receive,
				UnitPrice = new Money(asset.Currency, 26811.1M, new DateTime(2023,10,12,10,44,32, DateTimeKind.Utc)),
				ReferenceCode = "NXT2yQdOutpLLE1Lz51xXt6uW"
			}});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleCashbackFiat_Converted()
		{
			// Arrange
			var parser = new NexoParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/Specials/single_cashback_fiat.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 0.06548358M, new DateTime(2023, 10, 8, 20, 5, 12, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[]
			{ new Activity {
				Asset = null,
				Comment = "Transaction Reference: [NXT6asbYnZqniNoTss0nyuIxM] (Details: asset EURX)",
				Date = new DateTime(2023,10,8,20,5,12, DateTimeKind.Utc),
				Fee = null,
				Quantity = 0.06548358M,
				ActivityType = ActivityType.Gift,
				UnitPrice = new Money("EURX", 1M, new DateTime(2023,10,8,20,5,12, DateTimeKind.Utc)),
				ReferenceCode = "NXT6asbYnZqniNoTss0nyuIxM"
			}});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleReferralBonusPending_Converted()
		{
			// Arrange
			var parser = new NexoParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("BTC", It.IsAny<Currency>(), It.IsAny<AssetClass?[]>(), It.IsAny<AssetSubClass?[]>())).ReturnsAsync(asset1);

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
			var parser = new NexoParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("BTC", It.IsAny<Currency>(), It.IsAny<AssetClass?[]>(), It.IsAny<AssetSubClass?[]>())).ReturnsAsync(asset1);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/Specials/single_referralbonus_approved.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 0, new DateTime(0001, 01, 01, 0, 0, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[]
			{ new Activity {
				Asset = asset1,
				Comment = "Transaction Reference: [NXTk6FBYyxOqH] (Details: asset BTC)",
				Date = new DateTime(2023,08,25,16,43,55, DateTimeKind.Utc),
				Fee = null,
				Quantity = 0.00096332M,
				ActivityType = ActivityType.Receive,
				UnitPrice = new Money(asset1.Currency, 25951.855302495536270398206204M, new DateTime(2023,08,25,16,43,55, DateTimeKind.Utc)),
				ReferenceCode = "NXTk6FBYyxOqH"} });
		}



	}
}