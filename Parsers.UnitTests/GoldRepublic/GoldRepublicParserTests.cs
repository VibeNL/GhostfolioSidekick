using AutoFixture;
using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.GoldRepublic;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using GhostfolioSidekick.Parsers.UnitTests.TradeRepublic;

namespace GhostfolioSidekick.Parsers.UnitTests.GoldRepublic
{
	public class GoldRepublicParserTests
	{
		readonly GoldRepublicParser parser;
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		public GoldRepublicParserTests()
		{
			parser = new GoldRepublicParser(new PdfToWordsParser());

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

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/GoldRepublic/year_overview.pdf", activityManager, account.Name);

			// Assert

			// Default deposit
			activityManager.PartialActivities.Should().ContainEquivalentOf(
				PartialActivity.CreateCashDeposit(
					Currency.EUR,
					new DateTime(2023, 05, 17, 0, 0, 0, DateTimeKind.Utc),
					110m,
					new Money(Currency.EUR, 110),
					"Deposit 17-05-2023 Account deposit ( ) - €110.00 €110.01"));

			// Savings plan
			activityManager.PartialActivities.Should().ContainEquivalentOf(
				PartialActivity.CreateCashDeposit(
					Currency.EUR,
					new DateTime(2023, 07, 03, 0, 0, 0, DateTimeKind.Utc),
					50m,
					new Money(Currency.EUR, 50m),
					"Direct Debit 03-07-2023 Deposit for savingsplan Basic - €50.00 €55.01"));

			// Single buy
			activityManager.PartialActivities.Should().ContainEquivalentOf(
				PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 06, 09, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateGeneric("Gold")],
						0.001744m,
						new Money(Currency.EUR, 59610.09174311926605504587156m),
						new Money(Currency.EUR, 103.96m),
						"Market Order 09-06-2023 Processing Product Date Execution Action Transaction Fee Volume Total Submitted Date Value order 571659 Gold, 17-05-2023 17-05-2023 Buy €103.96 €1.04 1.744 €105.00 Amsterdam 10:49:38 10:49:38 Gold €-105.00 €5.01"));

			activityManager.PartialActivities.Should().ContainEquivalentOf(
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 06, 09, 0, 0, 0, DateTimeKind.Utc),
						1.04M,
						new Money(Currency.EUR, 1.04M),
						"Market Order 09-06-2023 Processing Product Date Execution Action Transaction Fee Volume Total Submitted Date Value order 571659 Gold, 17-05-2023 17-05-2023 Buy €103.96 €1.04 1.744 €105.00 Amsterdam 10:49:38 10:49:38 Gold €-105.00 €5.01"));

			// Single sell
			activityManager.PartialActivities.Should().ContainEquivalentOf(
				PartialActivity.CreateSell(
						Currency.EUR,
						new DateTime(2023, 07, 26, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateGeneric("Gold")],
						0.001744m,
						new Money(Currency.EUR, 56662.844036697247706422018349m),
						new Money(Currency.EUR, 98.82m),
						"Market Order 26-07-2023 Processing Product Date Execution Action Transaction Fee Volume Total Submitted Date Value order 608527 Gold, 26-07-2023 26-07-2023 Sell €98.82 €0.99 1.744 €97.83 Amsterdam 08:06:00 08:06:00 Gold €97.83 €102.78"));

			activityManager.PartialActivities.Should().ContainEquivalentOf(
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 07, 26, 0, 0, 0, DateTimeKind.Utc),
						0.99m,
						new Money(Currency.EUR, 0.99m),
						"Market Order 26-07-2023 Processing Product Date Execution Action Transaction Fee Volume Total Submitted Date Value order 608527 Gold, 26-07-2023 26-07-2023 Sell €98.82 €0.99 1.744 €97.83 Amsterdam 08:06:00 08:06:00 Gold €97.83 €102.78"));

			// Single Fee
			activityManager.PartialActivities.Should().ContainEquivalentOf(
				PartialActivity.CreateFee(
					Currency.EUR,
					new DateTime(2023, 07, 17, 0, 0, 0, DateTimeKind.Utc),
					0.06m,
					new Money(Currency.EUR, 0.06m),
					"Cost Order 17-07-2023 Opslagkosten juni 2023 - €-0.06 €54.95"));
		}
	}
}