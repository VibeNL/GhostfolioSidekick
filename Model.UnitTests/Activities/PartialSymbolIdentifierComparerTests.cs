using AwesomeAssertions;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Model.UnitTests.Activities
{
	public class PartialSymbolIdentifierComparerTests
	{
		[Fact]
		public void Dictionary_WithCustomComparer_ShouldFindCompatibleIdentifiers()
		{
			// Arrange
			var comparer = new PartialSymbolIdentifierComparer();
			var dictionary = new Dictionary<PartialSymbolIdentifier, string>(comparer);
			
            var identifier1 = PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Default, "AAPL", Currency.USD);
			var identifier2 = new PartialSymbolIdentifier(
				IdentifierType.Default,
				"AAPL",
				Currency.USD,
				new List<AssetClass> { AssetClass.Equity, AssetClass.Undefined }, // More permissive
				new List<AssetSubClass> { AssetSubClass.Stock } // Overlaps with identifier1
			);

			// Act
			dictionary[identifier1] = "Apple Stock";
			var found = dictionary.TryGetValue(identifier2, out var value);

			// Assert
			found.Should().BeTrue();
			value.Should().Be("Apple Stock");
		}

		[Fact]
		public void Dictionary_WithCustomComparer_ShouldConsiderNullAsCompatible()
		{
			// Arrange
			var comparer = new PartialSymbolIdentifierComparer();
			var dictionary = new Dictionary<PartialSymbolIdentifier, string>(comparer);
			
          var identifier1 = PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST", Currency.USD); // Has null asset classes
			var identifier2 = PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Default, "TEST", Currency.USD); // Has specific asset classes

			// Act
			dictionary[identifier1] = "Test Symbol";
			var found = dictionary.TryGetValue(identifier2, out var value);

			// Assert
			found.Should().BeTrue();
			value.Should().Be("Test Symbol");
		}

		[Fact]
		public void Dictionary_WithCustomComparer_ShouldConsiderUndefinedAsCompatible()
		{
			// Arrange
			var comparer = new PartialSymbolIdentifierComparer();
			var dictionary = new Dictionary<PartialSymbolIdentifier, string>(comparer);
			
           var identifier1 = new PartialSymbolIdentifier(
				IdentifierType.Default,
				"TEST",
				Currency.USD,
				new List<AssetClass> { AssetClass.Undefined },
				new List<AssetSubClass>()
			);
			var identifier2 = PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Default, "TEST", Currency.USD);

			// Act
			dictionary[identifier1] = "Test Symbol";
			var found = dictionary.TryGetValue(identifier2, out var value);

			// Assert
			found.Should().BeTrue();
			value.Should().Be("Test Symbol");
		}

		[Fact]
		public void Dictionary_WithCustomComparer_ShouldRejectIncompatibleAssetClasses()
		{
			// Arrange
			var comparer = new PartialSymbolIdentifierComparer();
			var dictionary = new Dictionary<PartialSymbolIdentifier, string>(comparer);
			
           var identifier1 = PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Default, "TEST", Currency.USD); // AssetClass.Equity
			var identifier2 = PartialSymbolIdentifier.CreateCrypto(IdentifierType.Default, "TEST", Currency.USD); // AssetClass.Liquidity

			// Act
			dictionary[identifier1] = "Test Stock";
			var found = dictionary.TryGetValue(identifier2, out var value);

			// Assert
			found.Should().BeFalse();
			value.Should().BeNull();
		}
	}
}