using AwesomeAssertions;
using GhostfolioSidekick.Activities.Comparer;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.UnitTests.Activities.Comparer
{
	public class SymbolComparerTests
	{
		private readonly SymbolComparer _symbolComparer;

		public SymbolComparerTests()
		{
			_symbolComparer = new SymbolComparer();
		}

		[Theory]
		[InlineData(null, null, true)]
		[InlineData("AAPL", "AAPL", true)]
		[InlineData("AAPL", "aapl", true)]
		[InlineData("aapl", "AAPL", true)]
		[InlineData("AAPL", "GOOGL", false)]
		[InlineData("", "", true)]
		public void Equals_WhenComparingSymbols_ShouldReturnExpectedResult(string? symbol1, string? symbol2, bool expected)
		{
			// Arrange
			var profile1 = symbol1 == null ? null : CreateSymbolProfile(symbol1, AssetClass.Equity, AssetSubClass.Stock);
			var profile2 = symbol2 == null ? null : CreateSymbolProfile(symbol2, AssetClass.Equity, AssetSubClass.Stock);

			// Act
			var result = _symbolComparer.Equals(profile1, profile2);

			// Assert
			result.Should().Be(expected);
		}

		[Fact]
		public void Equals_WhenBothProfilesNull_ShouldReturnTrue()
		{
			// Act
			var result = _symbolComparer.Equals(null, null);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void Equals_WhenFirstProfileNull_ShouldReturnFalse()
		{
			// Arrange
			var profile = CreateSymbolProfile("AAPL", AssetClass.Equity, AssetSubClass.Stock);

			// Act
			var result = _symbolComparer.Equals(null, profile);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void Equals_WhenSecondProfileNull_ShouldReturnFalse()
		{
			// Arrange
			var profile = CreateSymbolProfile("AAPL", AssetClass.Equity, AssetSubClass.Stock);

			// Act
			var result = _symbolComparer.Equals(profile, null);

			// Assert
			result.Should().BeFalse();
		}

		[Theory]
		[InlineData(AssetClass.Equity, AssetSubClass.Stock, AssetClass.Equity, AssetSubClass.Stock, true)]
		[InlineData(AssetClass.Equity, AssetSubClass.Stock, AssetClass.Equity, AssetSubClass.Etf, false)] // Different subclasses = incompatible
		[InlineData(AssetClass.Equity, AssetSubClass.Stock, AssetClass.FixedIncome, AssetSubClass.Bond, false)]
		[InlineData(AssetClass.Undefined, AssetSubClass.Stock, AssetClass.Equity, AssetSubClass.Stock, true)]
		[InlineData(AssetClass.Equity, AssetSubClass.Stock, AssetClass.Undefined, AssetSubClass.Stock, true)]
		[InlineData(AssetClass.Undefined, null, AssetClass.Undefined, null, true)]
		public void Equals_WhenComparingAssetClasses_ShouldReturnExpectedResult(
			AssetClass assetClass1, AssetSubClass? assetSubClass1,
			AssetClass assetClass2, AssetSubClass? assetSubClass2,
			bool expected)
		{
			// Arrange
			var profile1 = CreateSymbolProfile("AAPL", assetClass1, assetSubClass1);
			var profile2 = CreateSymbolProfile("AAPL", assetClass2, assetSubClass2);

			// Act
			var result = _symbolComparer.Equals(profile1, profile2);

			// Assert
			result.Should().Be(expected);
		}

		[Theory]
		[InlineData(AssetClass.Equity, null, AssetClass.Equity, null)]
		[InlineData(AssetClass.Equity, AssetSubClass.Undefined, AssetClass.Equity, AssetSubClass.Stock)]
		[InlineData(AssetClass.Equity, AssetSubClass.Stock, AssetClass.Equity, AssetSubClass.Undefined)]
		[InlineData(AssetClass.Equity, null, AssetClass.Equity, AssetSubClass.Stock)]
		[InlineData(AssetClass.Equity, AssetSubClass.Stock, AssetClass.Equity, null)]
		public void Equals_WhenComparingUndefinedOrNullAssetSubClasses_ShouldReturnTrue(
			AssetClass assetClass1, AssetSubClass? assetSubClass1,
			AssetClass assetClass2, AssetSubClass? assetSubClass2)
		{
			// Arrange
			var profile1 = CreateSymbolProfile("AAPL", assetClass1, assetSubClass1);
			var profile2 = CreateSymbolProfile("AAPL", assetClass2, assetSubClass2);

			// Act
			var result = _symbolComparer.Equals(profile1, profile2);

			// Assert
			result.Should().BeTrue();
		}

		[Theory]
		[InlineData("AAPL")]
		[InlineData("aapl")]
		[InlineData("GOOGL")]
		[InlineData("")]
		public void GetHashCode_ShouldReturnConsistentHashForSymbol(string symbol)
		{
			// Arrange
			var profile = CreateSymbolProfile(symbol, AssetClass.Equity, AssetSubClass.Stock);

			// Act
			var hashCode1 = _symbolComparer.GetHashCode(profile);
			var hashCode2 = _symbolComparer.GetHashCode(profile);

			// Assert
			hashCode1.Should().Be(hashCode2);
		}

		[Fact]
		public void GetHashCode_WhenSymbolsCaseInsensitive_ShouldReturnSameHash()
		{
			// Arrange
			var profile1 = CreateSymbolProfile("AAPL", AssetClass.Equity, AssetSubClass.Stock);
			var profile2 = CreateSymbolProfile("aapl", AssetClass.Equity, AssetSubClass.Stock);

			// Act
			var hashCode1 = _symbolComparer.GetHashCode(profile1);
			var hashCode2 = _symbolComparer.GetHashCode(profile2);

			// Assert
			hashCode1.Should().Be(hashCode2);
		}

		[Fact]
		public void GetHashCode_WhenSymbolsAreDifferent_ShouldReturnDifferentHash()
		{
			// Arrange
			var profile1 = CreateSymbolProfile("AAPL", AssetClass.Equity, AssetSubClass.Stock);
			var profile2 = CreateSymbolProfile("GOOGL", AssetClass.Equity, AssetSubClass.Stock);

			// Act
			var hashCode1 = _symbolComparer.GetHashCode(profile1);
			var hashCode2 = _symbolComparer.GetHashCode(profile2);

			// Assert
			hashCode1.Should().NotBe(hashCode2);
		}

		[Fact]
		public void GetHashCode_WhenSymbolIsNull_ShouldReturnZero()
		{
			// Arrange
			var profile = CreateSymbolProfile(null!, AssetClass.Equity, AssetSubClass.Stock);

			// Act
			var hashCode = _symbolComparer.GetHashCode(profile);

			// Assert
			hashCode.Should().Be(0);
		}

		[Theory]
		[InlineData(AssetClass.Equity, AssetClass.Equity, true)]
		[InlineData(AssetClass.Equity, AssetClass.FixedIncome, false)]
		[InlineData(AssetClass.Undefined, AssetClass.Equity, true)]
		[InlineData(AssetClass.Equity, AssetClass.Undefined, true)]
		[InlineData(AssetClass.Undefined, AssetClass.Undefined, true)]
		[InlineData(AssetClass.Liquidity, AssetClass.Commodity, false)]
		[InlineData(AssetClass.RealEstate, AssetClass.Commodity, false)]
		public void AreAssetClassesCompatible_WhenComparingAssetClasses_ShouldReturnExpectedResult(
			AssetClass assetClass1, AssetClass assetClass2, bool expected)
		{
			// Arrange
			var profile1 = CreateSymbolProfile("AAPL", assetClass1, null);
			var profile2 = CreateSymbolProfile("AAPL", assetClass2, null);

			// Act
			var result = _symbolComparer.Equals(profile1, profile2);

			// Assert
			result.Should().Be(expected);
		}

		[Theory]
		[InlineData(AssetSubClass.Stock, AssetSubClass.Stock, true)]
		[InlineData(AssetSubClass.Stock, AssetSubClass.Etf, false)] // Different subclasses are not compatible
		[InlineData(AssetSubClass.Undefined, AssetSubClass.Stock, true)]
		[InlineData(AssetSubClass.Stock, AssetSubClass.Undefined, true)]
		[InlineData(AssetSubClass.Undefined, AssetSubClass.Undefined, true)]
		[InlineData(null, null, true)]
		[InlineData(null, AssetSubClass.Stock, true)]
		[InlineData(AssetSubClass.Stock, null, true)]
		public void AreAssetClassesCompatible_WhenComparingAssetSubClasses_ShouldReturnExpectedResult(
			AssetSubClass? assetSubClass1, AssetSubClass? assetSubClass2, bool expected)
		{
			// Arrange - Use same asset class to focus on sub-class comparison
			var profile1 = CreateSymbolProfile("AAPL", AssetClass.Equity, assetSubClass1);
			var profile2 = CreateSymbolProfile("AAPL", AssetClass.Equity, assetSubClass2);

			// Act
			var result = _symbolComparer.Equals(profile1, profile2);

			// Assert
			result.Should().Be(expected);
		}

		[Fact]
		public void Equals_WhenSymbolsDifferButAssetClassesCompatible_ShouldReturnFalse()
		{
			// Arrange
			var profile1 = CreateSymbolProfile("AAPL", AssetClass.Equity, AssetSubClass.Stock);
			var profile2 = CreateSymbolProfile("GOOGL", AssetClass.Equity, AssetSubClass.Stock);

			// Act
			var result = _symbolComparer.Equals(profile1, profile2);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void Equals_WhenSymbolsSameButAssetClassesIncompatible_ShouldReturnFalse()
		{
			// Arrange
			var profile1 = CreateSymbolProfile("AAPL", AssetClass.Equity, AssetSubClass.Stock);
			var profile2 = CreateSymbolProfile("AAPL", AssetClass.FixedIncome, AssetSubClass.Bond);

			// Act
			var result = _symbolComparer.Equals(profile1, profile2);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void GetHashCode_WhenUsedInHashSet_ShouldWorkCorrectly()
		{
			// Arrange
			var profile1 = CreateSymbolProfile("AAPL", AssetClass.Equity, AssetSubClass.Stock);
			var profile2 = CreateSymbolProfile("aapl", AssetClass.Equity, AssetSubClass.Stock); // Different case and sub-class
			var profile3 = CreateSymbolProfile("GOOGL", AssetClass.Equity, AssetSubClass.Stock);
			
			var hashSet = new HashSet<SymbolProfile>(_symbolComparer)
			{
				// Act
				profile1
			};
			var addResult1 = hashSet.Add(profile2); // Should not be added as it's considered equal
			var addResult2 = hashSet.Add(profile3); // Should be added as it's different

			// Assert
			hashSet.Count.Should().Be(2);
			addResult1.Should().BeFalse(); // profile2 was not added (already exists)
			addResult2.Should().BeTrue();  // profile3 was added
		}

		[Theory]
		[InlineData("AAPL", "AAPL", AssetClass.Equity, AssetSubClass.Stock, AssetClass.Equity, AssetSubClass.Stock, true)]
		[InlineData("AAPL", "GOOGL", AssetClass.Equity, AssetSubClass.Stock, AssetClass.Equity, AssetSubClass.Stock, false)]
		[InlineData("AAPL", "AAPL", AssetClass.Equity, AssetSubClass.Stock, AssetClass.FixedIncome, AssetSubClass.Bond, false)]
		[InlineData("AAPL", "AAPL", AssetClass.Undefined, AssetSubClass.Stock, AssetClass.Equity, AssetSubClass.Stock, true)]
		public void Equals_ComplexScenarios_ShouldReturnExpectedResult(
			string symbol1, string symbol2,
			AssetClass assetClass1, AssetSubClass assetSubClass1,
			AssetClass assetClass2, AssetSubClass assetSubClass2,
			bool expected)
		{
			// Arrange
			var profile1 = CreateSymbolProfile(symbol1, assetClass1, assetSubClass1);
			var profile2 = CreateSymbolProfile(symbol2, assetClass2, assetSubClass2);

			// Act
			var result = _symbolComparer.Equals(profile1, profile2);

			// Assert
			result.Should().Be(expected);
		}

		[Fact]
		public void Equals_WhenUsedWithDictionary_ShouldWorkCorrectly()
		{
			// Arrange
			var profile1 = CreateSymbolProfile("AAPL", AssetClass.Equity, AssetSubClass.Stock);
			var profile2 = CreateSymbolProfile("aapl", AssetClass.Equity, AssetSubClass.Stock); // Different case and sub-class
			var profile3 = CreateSymbolProfile("GOOGL", AssetClass.Equity, AssetSubClass.Stock);
			
			var dictionary = new Dictionary<SymbolProfile, string>(_symbolComparer)
			{
				// Act
				[profile1] = "Apple Stock",
				[profile2] = "Apple ETF", // Should overwrite the previous entry
				[profile3] = "Google"
			};

			// Assert
			dictionary.Count.Should().Be(2);
			dictionary[profile1].Should().Be("Apple ETF"); // Should be overwritten
			dictionary.ContainsKey(profile2).Should().BeTrue();
			dictionary.ContainsKey(profile3).Should().BeTrue();
		}

		[Theory]
		[InlineData("")]
		[InlineData(" ")]
		[InlineData("\t")]
		public void Equals_WhenSymbolIsEmptyOrWhitespace_ShouldWorkCorrectly(string symbol)
		{
			// Arrange
			var profile1 = CreateSymbolProfile(symbol, AssetClass.Equity, AssetSubClass.Stock);
			var profile2 = CreateSymbolProfile(symbol, AssetClass.Equity, AssetSubClass.Stock);

			// Act
			var result = _symbolComparer.Equals(profile1, profile2);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void GetHashCode_WhenSymbolIsEmptyString_ShouldNotReturnZero()
		{
			// Arrange
			var profile = CreateSymbolProfile("", AssetClass.Equity, AssetSubClass.Stock);

			// Act
			var hashCode = _symbolComparer.GetHashCode(profile);

			// Assert
			// Empty string should not return 0 (which is reserved for null)
			hashCode.Should().NotBe(0);
		}

		[Fact]
		public void Equals_WhenDifferentAssetClassesButOneUndefined_ShouldReturnTrue()
		{
			// Arrange
			var profile1 = CreateSymbolProfile("AAPL", AssetClass.Undefined, null);
			var profile2 = CreateSymbolProfile("AAPL", AssetClass.Commodity, AssetSubClass.PreciousMetal);

			// Act
			var result = _symbolComparer.Equals(profile1, profile2);

			// Assert
			result.Should().BeTrue(); // Undefined should be compatible with any asset class
		}

		[Theory]
		[InlineData(AssetClass.Liquidity, AssetClass.Commodity)]
		[InlineData(AssetClass.Equity, AssetClass.FixedIncome)]
		[InlineData(AssetClass.RealEstate, AssetClass.Liquidity)]
		public void Equals_WhenDifferentNonUndefinedAssetClasses_ShouldReturnFalse(
			AssetClass assetClass1, AssetClass assetClass2)
		{
			// Arrange
			var profile1 = CreateSymbolProfile("AAPL", assetClass1, null);
			var profile2 = CreateSymbolProfile("AAPL", assetClass2, null);

			// Act
			var result = _symbolComparer.Equals(profile1, profile2);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void Equals_ShouldBeSymmetric()
		{
			// Arrange
			var profile1 = CreateSymbolProfile("AAPL", AssetClass.Equity, AssetSubClass.Stock);
			var profile2 = CreateSymbolProfile("aapl", AssetClass.Equity, AssetSubClass.Stock);

			// Act & Assert
			_symbolComparer.Equals(profile1, profile2).Should().Be(_symbolComparer.Equals(profile2, profile1));
		}

		[Fact]
		public void Equals_ShouldBeReflexive()
		{
			// Arrange
			var profile = CreateSymbolProfile("AAPL", AssetClass.Equity, AssetSubClass.Stock);

			// Act & Assert
			_symbolComparer.Equals(profile, profile).Should().BeTrue();
		}

		[Fact]
		public void Equals_ShouldBeTransitive()
		{
			// Arrange
			var profile1 = CreateSymbolProfile("AAPL", AssetClass.Equity, AssetSubClass.Stock);
			var profile2 = CreateSymbolProfile("aapl", AssetClass.Equity, AssetSubClass.Stock);
			var profile3 = CreateSymbolProfile("AAPL", AssetClass.Equity, AssetSubClass.MutualFund);

			// Act
			var equals12 = _symbolComparer.Equals(profile1, profile2);
			var equals23 = _symbolComparer.Equals(profile2, profile3);
			var equals13 = _symbolComparer.Equals(profile1, profile3);

			// Assert
			if (equals12 && equals23)
			{
				equals13.Should().BeTrue();
			}
		}

		[Fact]
		public void GetHashCode_ConsistentWithEquals()
		{
			// Arrange
			var profile1 = CreateSymbolProfile("AAPL", AssetClass.Equity, AssetSubClass.Stock);
			var profile2 = CreateSymbolProfile("aapl", AssetClass.Equity, AssetSubClass.Stock);

			// Act & Assert
			if (_symbolComparer.Equals(profile1, profile2))
			{
				_symbolComparer.GetHashCode(profile1).Should().Be(_symbolComparer.GetHashCode(profile2));
			}
		}

		private static SymbolProfile CreateSymbolProfile(string symbol, AssetClass assetClass, AssetSubClass? assetSubClass)
		{
			return new SymbolProfile
			{
				Symbol = symbol,
				Name = $"Test {symbol}",
				Currency = Currency.USD,
				DataSource = "TEST",
				AssetClass = assetClass,
				AssetSubClass = assetSubClass,
				Identifiers = [],
				CountryWeight = [],
				SectorWeights = []
			};
		}
	}
}
