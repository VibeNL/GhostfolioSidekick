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
		public async Task CanParseActivities_TestFileSingleOrder_True()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);

			// Act
			var canParse = await parser.CanParseActivities(new[] { "./FileImporter/TestFiles/Trading212/Example1/TestFileSingleOrder.csv" });

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleOrder_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var account = fixture.Build<Model.Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("US67066G1040", null)).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Trading212/Example1/TestFileSingleOrder.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 98.906043667000M, new DateTime(2023, 08, 11, 21, 8, 18, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] { new Model.Activity {
				Asset = asset,
				Comment = "Transaction Reference: [EOF3219953148]",
				Date = new DateTime(2023,08,7, 19,56,2, DateTimeKind.Utc),
				Fee = new Money(DefaultCurrency.EUR, 0.02M, new DateTime(2023,08,7, 19,56,2, DateTimeKind.Utc)),
				Quantity = 0.0267001M,
				ActivityType = Model. ActivityType.Buy,
				UnitPrice = new Money(DefaultCurrency.USD,453.33M, new DateTime(2023,08,7, 19,56,2, DateTimeKind.Utc)),
				ReferenceCode = "EOF3219953148"
			},
			new Model.Activity {
				Asset = null,
				Comment = "Transaction Reference: [82f82014-23a3-4ddf-bc09-658419823f4c]",
				Date = new DateTime(2023,08,11, 21,08,18, DateTimeKind.Utc),
				Fee = null,
				Quantity = 1M,
				ActivityType = Model. ActivityType.Interest,
				UnitPrice = new Money(DefaultCurrency.EUR,0.01M, new DateTime(2023,08,11, 21,08,18, DateTimeKind.Utc)),
				ReferenceCode = "82f82014-23a3-4ddf-bc09-658419823f4c"
			}});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileMultipleOrdersUS_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var account = fixture.Build<Model.Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("US67066G1040", null)).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Trading212/Example2/TestFileMultipleOrdersUS.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, -13.232829008000M, new DateTime(2023, 08, 9, 15, 25, 08, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] {
			new Model.Activity {
				Asset = asset,
				Comment = "Transaction Reference: [EOF3219953148]",
				Date = new DateTime(2023,08,7, 19,56,2, DateTimeKind.Utc),
				Fee = new Money(DefaultCurrency.EUR,0.02M, new DateTime(2023,08,7, 19,56,2, DateTimeKind.Utc)),
				Quantity = 0.0267001M,
				ActivityType = Model.ActivityType.Buy,
				UnitPrice = new Money(DefaultCurrency.USD,453.33M, new DateTime(2023,08,7, 19,56,2, DateTimeKind.Utc)),
				ReferenceCode = "EOF3219953148"
			},
			new Model.Activity {
				Asset = asset,
				Comment = "Transaction Reference: [EOF3224031567]",
				Date = new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc),
				Fee = null,
				Quantity = 0.0026199M,
				ActivityType = Model.ActivityType.Buy,
				UnitPrice = new Money(DefaultCurrency.USD,423.25M, new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc)),
				ReferenceCode = "EOF3224031567"
			}});
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleOrderUK_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.GBX).Create();
			var account = fixture.Build<Model.Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("GB0007188757", null)).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Trading212/Example3/TestFileSingleOrderUK.csv" });

			// Assert
			account.Activities.Should().BeEquivalentTo(new[] { new Model.Activity {
				Asset = asset,
				Comment = "Transaction Reference: [EOF3224031549]",
				Date = new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc),
				Fee = new Money(DefaultCurrency.EUR,0.07M, new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc)),
				Quantity = 0.18625698M,
				ActivityType = Model.ActivityType.Buy,
				UnitPrice = new Money(DefaultCurrency.GBX,4947.00M, new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc)),
				ReferenceCode = "EOF3224031549"
			} });
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleDividend_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var account = fixture.Build<Model.Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("US0378331005", null)).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Trading212/Example4/TestFileSingleDividend.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 0.025583540000M, new DateTime(2023, 08, 17, 10, 49, 49, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] { new Model.Activity {
				Asset = asset,
				Comment = "Transaction Reference: [Dividend_US0378331005_2023-08-17]",
				Date = new DateTime(2023,08,17, 10,49,49, DateTimeKind.Utc),
				Fee = null,
				Quantity = 0.1279177000M,
				ActivityType = Model.ActivityType.Dividend,
				UnitPrice = new Money(DefaultCurrency.USD, 0.20M, new DateTime(2023,08,17, 10,49,49, DateTimeKind.Utc)),
				ReferenceCode = "Dividend_US0378331005_2023-08-17"
			} });
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleOrderUKNativeCurrency_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.GBX).Create();
			var account = fixture.Build<Model.Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("GB0007188757", null)).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Trading212/Example5/TestFileSingleOrderUKNativeCurrency.csv" });

			// Assert
			account.Activities.Should().BeEquivalentTo(new[] { new Model.Activity {
				Asset = asset,
				Comment = "Transaction Reference: [EOF3224031549]",
				Date = new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc),
				Fee = new Money(DefaultCurrency.GBP,0.05M, new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc)),
				Quantity = 0.18625698M,
				ActivityType = Model.ActivityType.Buy,
				UnitPrice = new Money(DefaultCurrency.GBX,4947.00M, new DateTime(2023,08,9, 15,25,8, DateTimeKind.Utc)),
				ReferenceCode = "EOF3224031549"
			} });
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleOrderMultipleTimes_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var asset = fixture.Build<Model.Asset>().With(x => x.Currency, DefaultCurrency.USD).Create();
			var account = fixture.Build<Model.Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);
			api.Setup(x => x.FindSymbolByISIN("US67066G1040", null)).ReturnsAsync(asset);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] {
				"./FileImporter/TestFiles/Trading212/Example6/TestFileSingleOrder.csv",
				"./FileImporter/TestFiles/Trading212/Example6/TestFileSingleOrder2.csv",
				"./FileImporter/TestFiles/Trading212/Example6/TestFileSingleOrder3.csv"
			});

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 98.896043667000M, new DateTime(2023, 08, 7, 19, 56, 02, DateTimeKind.Utc)));
			account.Activities.Should().BeEquivalentTo(new[] { new Model. Activity {
				Asset = asset,
				Comment = "Transaction Reference: [EOF3219953148]",
				Date = new DateTime(2023,08,7, 19,56,2, DateTimeKind.Utc),
				Fee = new Money(DefaultCurrency.EUR,0.02M, new DateTime(2023,08,7, 19,56,2, DateTimeKind.Utc)),
				Quantity = 0.0267001M,
				ActivityType = Model.ActivityType.Buy,
				UnitPrice = new Money(DefaultCurrency.USD,453.33M, new DateTime(2023,08,7, 19,56,2, DateTimeKind.Utc)),
				ReferenceCode = "EOF3219953148"
			}});
		}


		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileCurrencyConversion_Converted()
		{
			// Arrange
			var parser = new Trading212Parser(api.Object);
			var fixture = new Fixture();

			var account = fixture.Build<Model.Account>().With(x => x.Balance, Balance.Empty(DefaultCurrency.EUR)).Create();

			api.Setup(x => x.GetAccountByName(account.Name)).ReturnsAsync(account);

			// Act
			account = await parser.ConvertActivitiesForAccount(account.Name, new[] { "./FileImporter/TestFiles/Trading212/Example7/TestFileCurrencyConversion.csv" });

			// Assert
			account.Balance.Current(DummyPriceConverter.Instance).Should().BeEquivalentTo(new Money(DefaultCurrency.EUR, 0.00M, new DateTime(2023, 09, 25, 17, 31, 38, 897, DateTimeKind.Utc)));
			account.Activities.Should().BeEmpty();
		}
	}
}