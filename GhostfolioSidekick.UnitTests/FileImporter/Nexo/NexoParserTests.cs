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

			var asset1 = fixture.Build<Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var asset2 = fixture.Build<Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("USDC", It.IsAny<Func<IEnumerable<Asset>, Asset>>())).ReturnsAsync(asset1);
			api.Setup(x => x.FindSymbolByIdentifier("BTC", It.IsAny<Func<IEnumerable<Asset>, Asset>>())).ReturnsAsync(asset2);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/Example1/Example1.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, -11.9M, new DateTime(2023, 08, 25, 14, 44, 46, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[]
			{ new Activity {
				Asset = asset1,
				Comment = "Transaction Reference: [NXTyPxhiopNL3] (Details: asset USDC)",
				Date = new DateTime(2023,8,25,14,44,46, DateTimeKind.Utc),
				Fee = null,
				Quantity = 161.90485771M,
				ActivityType = ActivityType.Buy,
				UnitPrice = new Money(asset1.Currency, 0.999969996514813032906620872M, new DateTime(2023,8,25,14,44,46, DateTimeKind.Utc)),
				ReferenceCode = "NXTyPxhiopNL3"
			}, new Activity {
				Asset = asset2,
				Comment = "Transaction Reference: [NXTyVJeCwg6Og] (Details: asset BTC)",
				Date = new DateTime(2023,8,26, 13,30,38, DateTimeKind.Utc),
				Quantity = 0.00445142M,
				ActivityType = ActivityType.Receive,
				UnitPrice = new Money(asset1.Currency, 26028.386478921332967906870167M, new DateTime(2023,8,26, 13,30,38, DateTimeKind.Utc)),
				ReferenceCode = "NXTyVJeCwg6Og"
			} });
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileMultipleOrders_ReferalApproved_Converted()
		{
			// Arrange
			var parser = new NexoParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var asset2 = fixture.Build<Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("USDC", It.IsAny<Func<IEnumerable<Asset>, Asset>>())).ReturnsAsync(asset1);
			api.Setup(x => x.FindSymbolByIdentifier("BTC", It.IsAny<Func<IEnumerable<Asset>, Asset>>())).ReturnsAsync(asset2);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/Example2/Example2.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, -11.90000000000000000000000000M, new DateTime(2023, 08, 25, 14, 44, 46, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[]
			{ new Activity {
				Asset = asset1,
				Comment = "Transaction Reference: [NXTyPxhiopNL3] (Details: asset USDC)",
				Date = new DateTime(2023,8,25,14,44,46, DateTimeKind.Utc),
				Fee = null,
				Quantity = 161.90485771M,
				ActivityType = ActivityType.Buy,
				UnitPrice = new Money(asset1.Currency, 0.999969996514813032906620872M, new DateTime(2023,8,25,14,44,46, DateTimeKind.Utc)),
				ReferenceCode = "NXTyPxhiopNL3"
			}, new Activity {
				Asset = asset2,
				Comment = "Transaction Reference: [NXTk6FBYyxOqH] (Details: asset BTC)",
				Date = new DateTime(2023,08,25,16,43,55, DateTimeKind.Utc),
				Fee = null,
				Quantity = 0.00096332M,
				ActivityType = ActivityType.Receive,
				UnitPrice = new Money(asset1.Currency, 25951.855302495536270398206204M, new DateTime(2023,08,25,16,43,55, DateTimeKind.Utc)),
				ReferenceCode = "NXTk6FBYyxOqH"
			}, new Activity {
				Asset = asset2,
				Comment = "Transaction Reference: [NXTyVJeCwg6Og] (Details: asset BTC)",
				Date = new DateTime(2023,8,26, 13,30,38, DateTimeKind.Utc),
				Fee = null,
				Quantity = 0.00445142M,
				ActivityType = ActivityType.Receive,
				UnitPrice = new Money(asset1.Currency, 26028.386478921332967906870167M, new DateTime(2023,8,26, 13,30,38, DateTimeKind.Utc)),
				ReferenceCode = "NXTyVJeCwg6Og"
			}});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestCashback_Converted()
		{
			// Arrange
			var parser = new NexoParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("BTC", It.IsAny<Func<IEnumerable<Asset>, Asset>>())).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/Example3/Cashback.csv" });

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
			},
			new Activity {
				Asset = null,
				Comment = "Transaction Reference: [NXT6asbYnZqniNoTss0nyuIxM] (Details: asset EURX)",
				Date = new DateTime(2023,10,8,20,5,12, DateTimeKind.Utc),
				Fee = null,
				Quantity = 0.06548358M,
				ActivityType = ActivityType.Receive,
				UnitPrice = new Money("EURX", 1M, new DateTime(2023,10,8,20,5,12, DateTimeKind.Utc)),
				ReferenceCode = "NXT6asbYnZqniNoTss0nyuIxM"
			}});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestExchangeCoins_Converted()
		{
			// Arrange
			var parser = new NexoParser(api.Object);
			var fixture = new Fixture();

			var asset1 = fixture.Build<Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var asset2 = fixture.Build<Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("USDC", It.IsAny<Func<IEnumerable<Asset>, Asset>>())).ReturnsAsync(asset1);
			api.Setup(x => x.FindSymbolByIdentifier("BTC", It.IsAny<Func<IEnumerable<Asset>, Asset>>())).ReturnsAsync(asset2);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Nexo/Example4/ExchangeCoins.csv" });

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
	}
}