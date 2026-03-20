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
			
           var identifier1 = PartialSymbolIdentifier.CreateStockAndETF("AAPL", Currency.EUR);
			var identifier2 = new PartialSymbolIdentifier 
			{ 
				Identifier = "AAPL",
				AllowedAssetClasses = [AssetClass.Equity, AssetClass.Undefined], // More permissive
				AllowedAssetSubClasses = [AssetSubClass.Stock], // Overlaps with identifier1
				Currency = Currency.EUR
			};

			// Act
			dictionary[identifier1] = "Apple Stock";
			var found = dictionary.Keys.Any(k => comparer.Equals(k, identifier2));
			var value = found ? dictionary.First(kvp => comparer.Equals(kvp.Key, identifier2)).Value : null;

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
			
           var identifier1 = PartialSymbolIdentifier.CreateGeneric("TEST", Currency.EUR); // Has null asset classes
           var identifier2 = PartialSymbolIdentifier.CreateStockAndETF("TEST", Currency.EUR); // Has specific asset classes

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
			
            var identifier1 = new PartialSymbolIdentifier
			{
				Identifier = "TEST",
				AllowedAssetClasses = [AssetClass.Undefined],
				Currency = Currency.EUR
			};
           var identifier2 = PartialSymbolIdentifier.CreateStockAndETF("TEST", Currency.EUR);

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
			
           var identifier1 = PartialSymbolIdentifier.CreateStockAndETF("TEST", Currency.EUR); // AssetClass.Equity
           var identifier2 = PartialSymbolIdentifier.CreateCrypto("TEST", Currency.EUR); // AssetClass.Liquidity

			// Act
			dictionary[identifier1] = "Test Stock";
			var found = dictionary.TryGetValue(identifier2, out var value);

			// Assert
			found.Should().BeFalse();
			value.Should().BeNull();
		}
	}
}