using AutoFixture;
using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.GoldRepublic;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.UnitTests.GoldRepublic
{
	public class GoldRepublicParserTests
	{
		readonly GoldRepublicParser parser;
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		public GoldRepublicParserTests()
		{
			parser = new GoldRepublicParser(new PdfToText());

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
			foreach (var file in Directory.GetFiles("./TestFiles/GoldRepublic/", "*.pdf", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact(Skip = "TODO")]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/GoldRepublic/year_overview.pdf", activityManager, account.Name);

			// Assert

			// Default deposit
			activityManager.PartialActivities.Should().Contain(
				[PartialActivity.CreateCashDeposit(
					Currency.EUR,
					new DateTime(2023, 05, 17, 0, 0, 0, DateTimeKind.Utc),
					110m,
					new Money(Currency.EUR, 110),
					"???")]
				);

			// Savings plan
			activityManager.PartialActivities.Should().Contain(
				[PartialActivity.CreateCashDeposit(
					Currency.EUR,
					new DateTime(2023, 07, 03, 0, 0, 0, DateTimeKind.Utc),
					50m,
					new Money(Currency.EUR, 50m),
					"???")]
				);

			// Single buy
			activityManager.PartialActivities.Should().Contain(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 06, 09, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("Gold(KG)")],
						0.001744m,
						new Money(Currency.EUR, 59610.0917m),
						new Money(Currency.EUR, 103.96m),
						"???"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 06, 09, 0, 0, 0, DateTimeKind.Utc),
						1.04M,
						new Money(Currency.EUR, 1.04M),
						"???")]
				);

			// Single sell
			activityManager.PartialActivities.Should().Contain(
				[PartialActivity.CreateSell(
						Currency.EUR,
						new DateTime(2023, 07, 26, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("Gold(KG)")],
						0.001744m,
						new Money(Currency.EUR, 59610.0917m),
						new Money(Currency.EUR, 98.82m),
						"???"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 07, 26, 0, 0, 0, DateTimeKind.Utc),
						0.99m,
						new Money(Currency.EUR, 0.99m),
						"???")]
				);

			// Single Fee
			activityManager.PartialActivities.Should().Contain(
				[PartialActivity.CreateFee(
					Currency.EUR,
					new DateTime(2023, 07, 17, 0, 0, 0, DateTimeKind.Utc),
					0.06m,
					new Money(Currency.EUR, 0.06m),
					"???")]
				);


		}
	}
}