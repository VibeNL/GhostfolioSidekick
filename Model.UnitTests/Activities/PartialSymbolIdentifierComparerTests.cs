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
			
			var identifier1 = PartialSymbolIdentifier.CreateStockAndETF("AAPL");
			var identifier2 = new PartialSymbolIdentifier 
			{ 
				Identifier = "AAPL",
				AllowedAssetClasses = [AssetClass.Equity, AssetClass.Undefined], // More permissive
				AllowedAssetSubClasses = [AssetSubClass.Stock] // Overlaps with identifier1
			};

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
			
			var identifier1 = PartialSymbolIdentifier.CreateGeneric("TEST"); // Has null asset classes
			var identifier2 = PartialSymbolIdentifier.CreateStockAndETF("TEST"); // Has specific asset classes

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
				AllowedAssetClasses = [AssetClass.Undefined]
			};
			var identifier2 = PartialSymbolIdentifier.CreateStockAndETF("TEST");

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
			
			var identifier1 = PartialSymbolIdentifier.CreateStockAndETF("TEST"); // AssetClass.Equity
			var identifier2 = PartialSymbolIdentifier.CreateCrypto("TEST"); // AssetClass.Liquidity

			// Act
			dictionary[identifier1] = "Test Stock";
			var found = dictionary.TryGetValue(identifier2, out var value);

			// Assert
			found.Should().BeFalse();
			value.Should().BeNull();
		}
	}
}