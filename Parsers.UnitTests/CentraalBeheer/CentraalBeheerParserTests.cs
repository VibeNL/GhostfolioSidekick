using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.CentraalBeheer;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.UnitTests.CentraalBeheer
{
	public class CentraalBeheerParserTests
	{
		readonly CentraalBeheerParser parser;
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		public CentraalBeheerParserTests()
		{
			parser = new CentraalBeheerParser(new PdfToWordsParser());

			var fixture = new Fixture();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, [new Balance(DateTime.Today, new Money(Currency.EUR, 0))])
				.Create();
			activityManager = new TestActivityManager();
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/CentraalBeheer/", "*.pdf", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/CentraalBeheer/deposit.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateCashDeposit(
					Currency.EUR,
					new DateTime(2023, 9, 9, 0, 0, 0, DateTimeKind.Utc),
					1000,
					new Money(Currency.EUR, 1000),
					"Centraal_Beheer_CashDeposit_2023-09-09")]
				);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuy_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/CentraalBeheer/buy_order.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 09, 12, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("Centraal Beheer Mixfonds Ambitieus")],
						29.3667M,
						33.95M,
						new Money(Currency.EUR, 1000M),
						"Centraal_Beheer_Buy_Centraal Beheer Mixfonds Ambitieus_2023-09-12"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 09, 12, 0, 0, 0, DateTimeKind.Utc),
						3M,
						new Money(Currency.EUR, 3M),
						"Centraal_Beheer_Buy_Centraal Beheer Mixfonds Ambitieus_2023-09-12")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleSell_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/CentraalBeheer/sell_order.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSell(
						Currency.EUR,
						new DateTime(2023, 7, 26, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("Centraal Beheer Mixfonds Voorzichtig")],
						1.9314M,
						26.28M,
						new Money(Currency.EUR,  50.76M),
						"Centraal_Beheer_Buy_Centraal Beheer Mixfonds Voorzichtig_2023-07-26")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileDividends_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/CentraalBeheer/dividends.pdf", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateDividend(
						Currency.EUR,
						new DateTime(2024, 6, 7, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("Centraal Beheer Mixfonds Zeer Ambitieus")],
						25.20m,
						new Money(Currency.EUR,  25.20m),
						"Centraal_Beheer_Dividend_Centraal Beheer Mixfonds Zeer Ambitieus_2024-06-07"),
					PartialActivity.CreateTax(
						Currency.EUR,
						new DateTime(2024, 6, 7, 0, 0, 0, DateTimeKind.Utc),
						3.78m,
						new Money(Currency.EUR, 3.78m),
						"Centraal_Beheer_Dividend_Centraal Beheer Mixfonds Zeer Ambitieus_2024-06-07"),
					PartialActivity.CreateDividend(
						Currency.EUR,
						new DateTime(2024, 6, 7, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("Centraal Beheer Mixfonds Ambitieus")],
						27.54m,
						new Money(Currency.EUR,  27.54m),
						"Centraal_Beheer_Dividend_Centraal Beheer Mixfonds Ambitieus_2024-06-07"),
					PartialActivity.CreateTax(
						Currency.EUR,
						new DateTime(2024, 6, 7, 0, 0, 0, DateTimeKind.Utc),
						4.13m,
						new Money(Currency.EUR, 4.13m),
						"Centraal_Beheer_Dividend_Centraal Beheer Mixfonds Ambitieus_2024-06-07")
				]);
		}
	}
}