using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.Nexo;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.Bitvavo
{
	public class BitvavoParserTests
	{
		readonly Mock<IGhostfolioAPI> api;

		public BitvavoParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			var parser = new BitvavoParser(api.Object);

			foreach (var file in Directory.GetFiles("./FileImporter/TestFiles/Bitvavo/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(new[] { file });

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuy_Converted()
		{
			// Arrange
			var parser = new BitvavoParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.USD)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier(new string?[] { "Storj", "STORJ" }, It.IsAny<Currency>(), It.IsAny<AssetClass?[]>(), It.IsAny<AssetSubClass?[]>())).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Bitvavo/BuyOrders/single_buy.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.USD, -25M, new DateTime(2023, 12, 13, 14, 39, 02, 473, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] { new Activity {
				Asset = asset,
				Comment = "Transaction Reference: [16eed6ae-65f9-4a9d-8f19-bd66a75fc745] (Details: asset STORJ)",
				Date = new DateTime(2023, 12, 13, 14, 39, 02, 473, DateTimeKind.Utc),
				Fees = new[] { new Money(DefaultCurrency.EUR, 0.0623441398262M, new DateTime(2023, 12, 13, 14, 39, 02, 473, DateTimeKind.Utc)) },
				Quantity = 34.75825253M,
				ActivityType = ActivityType.Buy,
				UnitPrice = new Money(DefaultCurrency.EUR, 0.71746M, new DateTime(2023, 12, 13, 14, 39, 02, 473, DateTimeKind.Utc)),
				ReferenceCode = "16eed6ae-65f9-4a9d-8f19-bd66a75fc745"
			} });
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleReceive_Converted()
		{
			// Arrange
			var parser = new BitvavoParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.USD)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier(new string?[] { "Cosmos", "ATOM" }, It.IsAny<Currency>(), It.IsAny<AssetClass?[]>(), It.IsAny<AssetSubClass?[]>())).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Bitvavo/Receive/single_receive.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.USD, 0, new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] { new Activity {
				Asset = asset,
				Comment = "Transaction Reference: [af86c3d8-ff57-4866-b6ce-7a549db31eda] (Details: asset ATOM)",
				Date = new DateTime(2023, 10, 13, 22, 38, 36, DateTimeKind.Utc),
				Fees = Array.Empty<Money>(),
				Quantity = 15.586311M,
				ActivityType = ActivityType.Receive,
				UnitPrice = new Money(DefaultCurrency.EUR, 0, new DateTime(2023, 10, 13, 22, 38, 36, DateTimeKind.Utc)),
				ReferenceCode = "af86c3d8-ff57-4866-b6ce-7a549db31eda"
			} });
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange
			var parser = new BitvavoParser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.USD)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Bitvavo/CashTransactions/single_deposit.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.USD, 1, new DateTime(2023, 04, 21, 08, 48, 55, DateTimeKind.Utc)));
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange
			var parser = new BitvavoParser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.USD)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Bitvavo/CashTransactions/single_withdrawal.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.USD, -101.88M, new DateTime(2023, 10, 24, 21, 23, 37, DateTimeKind.Utc)));
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSell_Converted()
		{
			// Arrange
			var parser = new BitvavoParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.USD)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier(new string?[] { "Cardano", "ADA" }, It.IsAny<Currency>(), It.IsAny<AssetClass?[]>(), It.IsAny<AssetSubClass?[]>())).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Bitvavo/SellOrders/single_sell.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.USD, 25.93000000000M, new DateTime(2023, 12, 13, 14, 45, 51, 803, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] { new Activity {
				Asset = asset,
				Comment = "Transaction Reference: [14ae873a-4fce-4a12-ba0f-387522c67d46] (Details: asset ADA)",
				Date = new DateTime(2023, 12, 13, 14, 45, 51, 803, DateTimeKind.Utc),
				Fees = new[] { new Money(DefaultCurrency.EUR, 0.04645763986M, new DateTime(2023, 12, 13, 14, 45, 51, 803, DateTimeKind.Utc)) },
				Quantity = 45.802549M,
				ActivityType = ActivityType.Sell,
				UnitPrice = new Money(DefaultCurrency.EUR, 0.56714M, new DateTime(2023, 12, 13, 14, 45, 51, 803, DateTimeKind.Utc)),
				ReferenceCode = "14ae873a-4fce-4a12-ba0f-387522c67d46"
			} });
		}
	}
}