using AutoFixture;
using AwesomeAssertions;
using CsvHelper;
using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.CentraalBeheer;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.UnitTests.CentraalBeheer
{
	public class CentraalBeheerParserTests
	{
		private readonly CentraalBeheerParser parser;
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		public CentraalBeheerParserTests()
		{
			parser = new CentraalBeheerParser();

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
			foreach (var file in Directory.GetFiles("./TestFiles/CentraalBeheer/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file} cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/CentraalBeheer/single_deposit.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(
						Currency.EUR,
						new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc),
						150M,
						new Money(Currency.EUR, 150M),
						"CB-20251229-1")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SinglePurchase_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/CentraalBeheer/single_purchase.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2025, 12, 30, 0, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("Mixfonds Ambitieus")],
						1.6597M,
						new Money(Currency.EUR, 45.05M),
						new Money(Currency.EUR, 74.77M),
						"CB-20251230-1"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2025, 12, 30, 0, 0, 0, 0, DateTimeKind.Utc),
						0.23m,
						new Money(Currency.EUR, 0.23m),
						"CB-20251230-1")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSell_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/CentraalBeheer/single_sell.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSell(
						Currency.EUR,
						new DateTime(2025, 12, 30, 0, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("Mixfonds Ambitieus")],
						1.6597M,
						new Money(Currency.EUR, 45.05M),
						new Money(Currency.EUR, 74.77M),
						"CB-20251230-1"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2025, 12, 30, 0, 0, 0, 0, DateTimeKind.Utc),
						0.23m,
						new Money(Currency.EUR, 0.23m),
						"CB-20251230-1")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDividend_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/CentraalBeheer/single_dividend.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateDividend(
						Currency.EUR,
						new DateTime(2025, 06, 12, 0, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("Mixfonds Zeer Ambitieus")],
						46.94M,
						new Money(Currency.EUR, 46.94M),
						"CB-20250612-1"),
					PartialActivity.CreateTax(
						Currency.EUR,
						new DateTime(2025, 06, 12, 0, 0, 0, 0, DateTimeKind.Utc),
						7.04M,
						new Money(Currency.EUR, 7.04M),
						"CB-20250612-1")
				]);
		}
	}
}