using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.Generic;

namespace GhostfolioSidekick.Parsers.UnitTests.Generic
{
	public class StockSplitParserTests
	{
		private StockSplitParser parser;
		private Account account;
		private TestHoldingsCollection holdingsAndAccountsCollection;

		public StockSplitParserTests()
		{
			parser = new StockSplitParser();

			var fixture = new Fixture();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, new Balance(new Money(Currency.EUR, 0)))
				.Create();
			holdingsAndAccountsCollection = new TestHoldingsCollection(account);
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/GenericStockSplit/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuy_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/GenericStockSplit/single_split.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateStockSplit(new DateTime(2023, 08, 7, 0, 0, 0, DateTimeKind.Utc), [PartialSymbolIdentifier.CreateGeneric("US67066G1040")], 2, 6, "StockSplit_US67066G1040_2023-08-07_2_6"),
				]);
		}
	}
}