using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter.Trading212;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.Trading212
{
	public class Trading212Tests
	{
		readonly Mock<IGhostfolioAPI> api;

		public Trading212Tests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);

			foreach (var file in Directory.GetFiles("./FileImporter/TestFiles/Trading212/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			var activities = await parser.ConvertToActivities("./FileImporter/TestFiles/Trading212/CashTransactions/single_deposit.csv",account.Balance);

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 100, new DateTime(2023, 08, 07, 19, 56, 01, DateTimeKind.Utc)));
			activities.Should().BeEmpty();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			var activities = await parser.ConvertToActivities("./FileImporter/TestFiles/Trading212/CashTransactions/single_withdrawal.csv", account.Balance);

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, -1000, new DateTime(2023, 11, 17, 05, 49, 12, 337, DateTimeKind.Utc)));
			activities.Should().BeEmpty();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleInterest_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			var activities = await parser.ConvertToActivities("./FileImporter/TestFiles/Trading212/CashTransactions/single_interest.csv", account.Balance);

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 0.01M, new DateTime(2023, 08, 11, 21, 8, 18, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] {
				new Activity(
					ActivityType.Interest,
					null,
					new DateTime(2023,08,11, 21,08,18, DateTimeKind.Utc),
					1M,
					new Money(DefaultCurrency.EUR,0.01M, new DateTime(2023,08,11, 21,08,18, DateTimeKind.Utc)),
					Enumerable.Empty < Money >(),
					"Transaction Reference: [82f82014-23a3-4ddf-bc09-658419823f4c]",
					"82f82014-23a3-4ddf-bc09-658419823f4c"
				)
			});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyUSD_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("US67066G1040", It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset);

			// Act
			var activities = await parser.ConvertToActivities("./FileImporter/TestFiles/Trading212/BuyOrders/single_buy_usd.csv", account.Balance);

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, -12.123956333000M, new DateTime(2023, 08, 07, 19, 56, 2, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] { new Activity(
				ActivityType.Buy,
				asset,
				new DateTime(2023,08,7, 19,56,2, DateTimeKind.Utc),
				0.0267001M,
				new Money(DefaultCurrency.USD,453.33M, new DateTime(2023,08,7, 19,56,2, DateTimeKind.Utc)),
				new[] { new Money(DefaultCurrency.EUR, 0.02M, new DateTime(2023, 08, 7, 19, 56, 2, DateTimeKind.Utc)) },
				"Transaction Reference: [EOF3219953148] (Details: asset US67066G1040)",
				"EOF3219953148"
				)
			});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleOrderEuroUkTaxes_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<SymbolProfile>().With(x => x.Currency, DefaultCurrency.GBX).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("GB0007188757", It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset);

			// Act
			var activities = await parser.ConvertToActivities("./FileImporter/TestFiles/Trading212/BuyOrders/single_buy_euro_uk_taxes.csv", account.Balance);

			// Assert
			account.Activities.Should().BeEquivalentTo(new[] { new Activity(
				ActivityType.Buy,
				asset,
				new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc),
				0.18625698M,
				new Money(DefaultCurrency.GBX,4947.00M, new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc)),
				new[] {
					new Money(DefaultCurrency.EUR, 0.05M, new DateTime(2023, 08, 9, 15, 25, 8, DateTimeKind.Utc)) ,
					new Money(DefaultCurrency.EUR, 0.02M, new DateTime(2023, 08, 9, 15, 25, 8, DateTimeKind.Utc))
				},
				"Transaction Reference: [EOF3224031549] (Details: asset GB0007188757)",
				"EOF3224031549"
				)
			});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDividend_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("US0378331005", It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset);

			// Act
			var activities = await parser.ConvertToActivities("./FileImporter/TestFiles/Trading212/CashTransactions/single_dividend.csv", account.Balance);

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 0.025583540000M, new DateTime(2023, 08, 17, 10, 49, 49, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] { new Activity(
				ActivityType.Dividend,
				asset,
				new DateTime(2023,08,17, 10,49,49, DateTimeKind.Utc),
				0.1279177000M,
				new Money(DefaultCurrency.USD, 0.20M, new DateTime(2023,08,17, 10,49,49, DateTimeKind.Utc)),
				Enumerable.Empty<Money>(),
				"Transaction Reference: [Dividend_US0378331005_2023-08-17] (Details: asset US0378331005)",
				"Dividend_US0378331005_2023-08-17"
				)
			});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyGBP_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<SymbolProfile>().With(x => x.Currency, DefaultCurrency.GBX).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("GB0007188757", It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset);

			// Act
			var activities = await parser.ConvertToActivities("./FileImporter/TestFiles/Trading212/BuyOrders/single_buy_gbp.csv", account.Balance);

			// Assert
			account.Activities.Should().BeEquivalentTo(new[] { new Activity(
				ActivityType.Buy,
				asset,
				new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc),
				0.18625698M,
				new Money(DefaultCurrency.GBX,4947.00M, new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc)),
				new[] { new Money(DefaultCurrency.GBP, 0.05M, new DateTime(2023, 08, 9, 15, 25, 8, DateTimeKind.Utc)) },
				"Transaction Reference: [EOF3224031549] (Details: asset GB0007188757)",
				"EOF3224031549"
				)
			});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleConvertCurrencies_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			var activities = await parser.ConvertToActivities("./FileImporter/TestFiles/Trading212/CashTransactions/single_convert_currencies.csv", account.Balance);

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 0.00M, new DateTime(2023, 09, 25, 17, 31, 38, 897, DateTimeKind.Utc)));
			activities.Should().BeEmpty();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuyEuroFrenchTaxes_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("FR0010828137", It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset);

			// Act
			var activities = await parser.ConvertToActivities("./FileImporter/TestFiles/Trading212/BuyOrders/single_buy_euro_french_taxes.csv", account.Balance);

			// Assert
			account.Activities.Should().BeEquivalentTo(new[] { new Activity(
				ActivityType.Buy,
				asset,
				new DateTime(2023,10,9, 14,28,20, DateTimeKind.Utc),
				14.7252730000M,
				new Money(DefaultCurrency.EUR,13.88M, new DateTime(2023,10,9, 14,28,20, DateTimeKind.Utc)),
				new[] { new Money(DefaultCurrency.EUR, 0.61M, new DateTime(2023, 10, 9, 14, 28, 20, DateTimeKind.Utc)) },
				"Transaction Reference: [EOF4500547227] (Details: asset FR0010828137)",
				"EOF4500547227"
				)
			});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSellEuro_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<SymbolProfile>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var account = fixture.Build<Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByIdentifier("US7561091049", It.IsAny<Currency>(), It.IsAny<AssetClass[]>(), It.IsAny<AssetSubClass[]>(), true, false)).ReturnsAsync(asset);

			// Act
			var activities = await parser.ConvertToActivities("./FileImporter/TestFiles/Trading212/SellOrders/single_sell_euro.csv", account.Balance);

			// Assert
			account.Activities.Should().BeEquivalentTo(new[] { new Activity(
				ActivityType.Sell,
				asset,
				new DateTime(2023,10,9, 14,26,43, DateTimeKind.Utc),
				0.2534760000M,
				new Money(DefaultCurrency.USD,50.38M, new DateTime(2023,10,9, 14,26,43, DateTimeKind.Utc)),
				new[] { new Money(DefaultCurrency.EUR, 0.02M, new DateTime(2023, 10, 9, 14, 26, 43, DateTimeKind.Utc)) },
				"Transaction Reference: [EOF4500546889] (Details: asset US7561091049)",
				"EOF4500546889"
				)
			});
		}
	}
}