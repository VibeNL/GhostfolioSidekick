using FluentAssertions;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.UnitTests.Symbols
{
	public class SymbolProfileTests
	{
		[Fact]
		public void Equals_ShouldReturnTrue_WhenObjectsAreEqual()
		{
			// Arrange
			var symbolProfile1 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);
			var symbolProfile2 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);

			// Act
			var result = symbolProfile1.Equals((object)symbolProfile2);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void EqualsNull_ShouldReturnFalse_WhenObjectsAreEqual()
		{
			// Arrange
			var symbolProfile1 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);

			// Act
			var result = symbolProfile1.Equals(null);

			// Assert
			result.Should().BeFalse();
		}

		[Theory]
		[InlineData(null)]
		[InlineData(AssetSubClass.Stock)]
		public void Equals_ShouldReturnFalse_WhenObjectsAreNotEqual(AssetSubClass? assetSubClass)
		{
			// Arrange
			var symbolProfile1 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);
			var symbolProfile2 = new SymbolProfile("symbol2", "name2", [], new Currency("EUR"), "dataSource2", AssetClass.FixedIncome, assetSubClass, [], []);

			// Act
			var result = symbolProfile1.Equals((object)symbolProfile2);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void ParseIdentifiers_ShouldParseIdentifiersFromComment()
		{
			// Arrange
			var symbolProfile = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], [])
			{
				Comment = "Known Identifiers: [id1,id2,id3]"
			};

			// Act
			// The ParseIdentifiers method is called inside the Comment setter

			// Assert
			symbolProfile.Identifiers.Should().Contain(new[] { "id1", "id2", "id3" });
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("Known Identifiers:")]
		[InlineData("[a,b,c]")]
		public void ParseIdentifiers_EmptyComment_ShouldParseIdentifiersFromComment(string? comment)
		{
			// Arrange
			var symbolProfile = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], [])
			{
				Comment = comment
			};

			// Act
			// The ParseIdentifiers method is called inside the Comment setter

			// Assert
			symbolProfile.Identifiers.Should().BeEmpty();
		}

		[Fact]
		public void GetHashCode_ShouldReturnSameHashCode_ForEqualObjects()
		{
			// Arrange
			var symbolProfile1 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);
			var symbolProfile2 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);

			// Act
			var hashCode1 = symbolProfile1.GetHashCode();
			var hashCode2 = symbolProfile2.GetHashCode();

			// Assert
			hashCode1.Should().Be(hashCode2);
		}

		[Fact]
		public void GetHashCode_ShouldReturnDifferentHashCodes_ForDifferentObjects()
		{
			// Arrange
			var symbolProfile1 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);
			var symbolProfile2 = new SymbolProfile("symbol2", "name2", [], new Currency("EUR"), "dataSource2", AssetClass.FixedIncome, AssetSubClass.Stock, [], []);

			// Act
			var hashCode1 = symbolProfile1.GetHashCode();
			var hashCode2 = symbolProfile2.GetHashCode();

			// Assert
			hashCode1.Should().NotBe(hashCode2);
		}

		[Fact]
		public void PropertyTests()
		{
			// Arrange
			var symbolProfile = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);
			var newCurrency = new Currency("EUR");
			var newScraperConfiguration = new ScraperConfiguration();
			var newIdentifiers = new List<string> { "id1", "id2" };

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
			symbolProfile.Identifiers.Should().BeEquivalentTo(newIdentifiers);
			symbolProfile.Comment.Should().Be("Known Identifiers: [id1,id2]");
		}

		[Fact]
		public void EqualsOperator_ShouldReturnTrue_WhenObjectsAreEqual()
		{
			// Arrange
			var symbolProfile1 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);
			var symbolProfile2 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);

			// Act
			var result = symbolProfile1 == symbolProfile2;

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void EqualsOperator_ShouldReturnFalse_WhenObjectsAreNotEqual()
		{
			// Arrange
			var symbolProfile1 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);
			var symbolProfile2 = new SymbolProfile("symbol2", "name2", [], new Currency("EUR"), "dataSource2", AssetClass.FixedIncome, AssetSubClass.Stock, [], []);

			// Act
			var result = symbolProfile1 == symbolProfile2;

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void NotEqualsOperator_ShouldReturnTrue_WhenObjectsAreNotEqual()
		{
			// Arrange
			var symbolProfile1 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);
			var symbolProfile2 = new SymbolProfile("symbol2", "name2", [], new Currency("EUR"), "dataSource2", AssetClass.FixedIncome, AssetSubClass.Stock, [], []);

			// Act
			var result = symbolProfile1 != symbolProfile2;

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void NotEqualsOperator_ShouldReturnFalse_WhenObjectsAreEqual()
		{
			// Arrange
			var symbolProfile1 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);
			var symbolProfile2 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);

			// Act
			var result = symbolProfile1 != symbolProfile2;

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void GetHashCode_ShouldReturnDifferentHashCodes_ForObjectsWithDifferentSymbols()
		{
			// Arrange
			var symbolProfile1 = new SymbolProfile("symbol1", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);
			var symbolProfile2 = new SymbolProfile("symbol2", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);

			// Act
			var hashCode1 = symbolProfile1.GetHashCode();
			var hashCode2 = symbolProfile2.GetHashCode();

			// Assert
			hashCode1.Should().NotBe(hashCode2);
		}

		[Fact]
		public void GetHashCode_ShouldReturnDifferentHashCodes_ForObjectsWithDifferentNames()
		{
			// Arrange
			var symbolProfile1 = new SymbolProfile("symbol", "name1", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);
			var symbolProfile2 = new SymbolProfile("symbol", "name2", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);

			// Act
			var hashCode1 = symbolProfile1.GetHashCode();
			var hashCode2 = symbolProfile2.GetHashCode();

			// Assert
			hashCode1.Should().NotBe(hashCode2);
		}

		[Fact]
		public void GetHashCode_ShouldReturnDifferentHashCodes_ForObjectsWithDifferentCurrencies()
		{
			// Arrange
			var symbolProfile1 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);
			var symbolProfile2 = new SymbolProfile("symbol", "name", [], new Currency("EUR"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);

			// Act
			var hashCode1 = symbolProfile1.GetHashCode();
			var hashCode2 = symbolProfile2.GetHashCode();

			// Assert
			hashCode1.Should().NotBe(hashCode2);
		}

		[Fact]
		public void GetHashCode_ShouldReturnDifferentHashCodes_ForObjectsWithDifferentAssetClasses()
		{
			// Arrange
			var symbolProfile1 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);
			var symbolProfile2 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.FixedIncome, AssetSubClass.Etf, [], []);

			// Act
			var hashCode1 = symbolProfile1.GetHashCode();
			var hashCode2 = symbolProfile2.GetHashCode();

			// Assert
			hashCode1.Should().NotBe(hashCode2);
		}

		[Fact]
		public void GetHashCode_ShouldReturnDifferentHashCodes_ForObjectsWithDifferentAssetSubClasses()
		{
			// Arrange
			var symbolProfile1 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, AssetSubClass.Etf, [], []);
			var symbolProfile2 = new SymbolProfile("symbol", "name", [], new Currency("USD"), "dataSource", AssetClass.Equity, null, [], []);

			// Act
			var hashCode1 = symbolProfile1.GetHashCode();
			var hashCode2 = symbolProfile2.GetHashCode();

			// Assert
			hashCode1.Should().NotBe(hashCode2);
		}

	}
}
