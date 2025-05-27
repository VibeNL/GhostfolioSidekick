using AwesomeAssertions;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.UnitTests.Symbols
{
	public class SymbolProfileTests
	{
		[Fact]
		public void PropertyTests()
		{
			// Arrange
			List<string> identifiers = ["id1", "id2"];
			var symbolProfile = new SymbolProfile("symbol", "name", identifiers, Currency.USD, "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);
			var newCurrency = Currency.EUR;
			var newScraperConfiguration = new ScraperConfiguration();

			// Act
			symbolProfile.Currency = newCurrency;
			symbolProfile.Symbol = "newSymbol";
			symbolProfile.Name = "newName";
			symbolProfile.DataSource = "newDataSource";
			symbolProfile.AssetClass = AssetClass.FixedIncome;
			symbolProfile.AssetSubClass = AssetSubClass.Stock;
			symbolProfile.ISIN = "newISIN";
			symbolProfile.Comment = "Known Identifiers: [id1,id2]";

			// Assert
			symbolProfile.Currency.Should().Be(newCurrency);
			symbolProfile.Symbol.Should().Be("newSymbol");
			symbolProfile.Name.Should().Be("newName");
			symbolProfile.DataSource.Should().Be("newDataSource");
			symbolProfile.AssetClass.Should().Be(AssetClass.FixedIncome);
			symbolProfile.AssetSubClass.Should().Be(AssetSubClass.Stock);
			symbolProfile.ISIN.Should().Be("newISIN");
			symbolProfile.Identifiers.Should().BeEquivalentTo(identifiers);
			symbolProfile.Comment.Should().Be("Known Identifiers: [id1,id2]");
		}
	}
}
