using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.Generic;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.Generic
{
	public class GenericParserTests
	{
		readonly Mock<IGhostfolioAPI> api;

		public GenericParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			var parser = new GenericParser(api.Object);

			foreach (var file in Directory.GetFiles("./FileImporter/TestFiles/Generic/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(new[] { file });

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleOrder_Converted()
		{
			// Arrange
			var parser = new GenericParser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.USD)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("US67066G1040", It.IsAny<Currency>(), It.IsAny<AssetClass?[]>(), It.IsAny<AssetSubClass?[]>())).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Generic/BuyOrders/single_buy.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.USD, -12.123956333000M, new DateTime(2023, 08, 7, 0, 0, 0, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] { new Activity {
				Asset = asset,
				Comment = "Transaction Reference: [Buy_US67066G1040_2023-08-07] (Details: asset US67066G1040)",
				Date = new DateTime(2023,08,7, 0,0,0, DateTimeKind.Utc),
				Fees = new[] { new Money(DefaultCurrency.USD, 0.02M, new DateTime(2023,08,7, 0,0,0, DateTimeKind.Utc)) },
				Quantity = 0.0267001M,
				ActivityType = ActivityType.Buy,
				UnitPrice = new Money(DefaultCurrency.USD, 453.33M, new DateTime(2023,08,7, 0,0,0, DateTimeKind.Utc)),
				ReferenceCode = "Buy_US67066G1040_2023-08-07"
			} });
		}


		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange
			var parser = new GenericParser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.USD)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Generic/CashTransactions/single_deposit.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.USD, 1000, new DateTime(2023, 08, 6, 0, 0, 0, DateTimeKind.Utc)));
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange
			var parser = new GenericParser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.USD)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Generic/CashTransactions/single_withdrawal.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.USD, -10, new DateTime(2023, 08, 8, 0, 0, 0, DateTimeKind.Utc)));
		}
	}
}