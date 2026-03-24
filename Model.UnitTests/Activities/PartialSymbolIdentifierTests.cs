using AwesomeAssertions;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Model.UnitTests.Activities
{
	public class PartialSymbolIdentifierTests
	{
		[Fact]
		public void Equals_ShouldReturnTrue_WhenIdentifiersAreIdentical()
		{
			// Arrange
			var identifier1 = PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST", Currency.EUR)!;
			var identifier2 = PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST", Currency.EUR)!;

			// Act & Assert
			identifier1.Equals(identifier2).Should().BeTrue();
			(identifier1 == identifier2).Should().BeTrue();
		}

		[Fact]
		public void Equals_ShouldReturnFalse_WhenIdentifiersAreDifferent()
		{
			// Arrange
			var identifier1 = PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST1", Currency.EUR)!;
			var identifier2 = PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST2", Currency.EUR)!;

			// Act & Assert
			identifier1.Equals(identifier2).Should().BeFalse();
			(identifier1 == identifier2).Should().BeFalse();
		}

		[Fact]
		public void Equals_ShouldReturnTrue_WhenAssetClassesAreEquivalent()
		{
			// Arrange
			var identifier1 = PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Default, "TEST", Currency.EUR)!;
			var identifier2 = new PartialSymbolIdentifier(
				IdentifierType.Default,
				"TEST",
				Currency.EUR,
				new List<AssetClass> { AssetClass.Equity },
				new List<AssetSubClass> { AssetSubClass.Stock, AssetSubClass.Etf }
			);

			// Act & Assert
			identifier1.Equals(identifier2).Should().BeTrue();
			(identifier1 == identifier2).Should().BeTrue();
		}

		[Fact]
		public void Equals_ShouldReturnFalse_WhenAssetClassesAreDifferent()
		{
			// Arrange
			var identifier1 = PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Default, "TEST", Currency.EUR)!;
			var identifier2 = PartialSymbolIdentifier.CreateCrypto(IdentifierType.Default, "TEST", Currency.EUR)!;

			// Act & Assert
			identifier1.Equals(identifier2).Should().BeFalse();
			(identifier1 == identifier2).Should().BeFalse();
		}

		[Fact]
		public void Equals_ShouldReturnTrue_WhenBothHaveNullCollections()
		{
			// Arrange
			var identifier1 = new PartialSymbolIdentifier(IdentifierType.Default, "TEST", Currency.EUR, new List<AssetClass>(), new List<AssetSubClass>());
			var identifier2 = new PartialSymbolIdentifier(IdentifierType.Default, "TEST", Currency.EUR, new List<AssetClass>(), new List<AssetSubClass>());

			// Act & Assert
			identifier1.Equals(identifier2).Should().BeTrue();
			(identifier1 == identifier2).Should().BeTrue();
		}

		[Fact]
		public void Equals_ShouldReturnFalse_WhenOneHasNullAndOtherHasValues()
		{
			// Arrange
			var identifier1 = new PartialSymbolIdentifier(IdentifierType.Default, "TEST", Currency.EUR, new List<AssetClass>(), new List<AssetSubClass>());
			var identifier2 = PartialSymbolIdentifier.CreateCrypto(IdentifierType.Default, "TEST", Currency.EUR)!;

			// Act & Assert
			identifier1.Equals(identifier2).Should().BeFalse();
			(identifier1 == identifier2).Should().BeFalse();
		}

		[Fact]
		public void GetHashCode_ShouldBeEqual_WhenObjectsAreEqual()
		{
			// Arrange
			var identifier1 = PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Default, "TEST", Currency.EUR)!;
			var identifier2 = new PartialSymbolIdentifier(
				IdentifierType.Default,
				"TEST",
				Currency.EUR,
				new List<AssetClass> { AssetClass.Equity },
				new List<AssetSubClass> { AssetSubClass.Stock, AssetSubClass.Etf }
			);

			// Act & Assert
			identifier1.GetHashCode().Should().Be(identifier2.GetHashCode());
		}

		[Fact]
		public void GetHashCode_ShouldBeDifferent_WhenObjectsAreNotEqual()
		{
			// Arrange
			var identifier1 = PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Default, "TEST1", Currency.EUR)!;
			var identifier2 = PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Default, "TEST2", Currency.EUR)!;

			// Act & Assert
			identifier1.GetHashCode().Should().NotBe(identifier2.GetHashCode());
		}

		[Fact]
		public void Equals_ShouldReturnFalse_WhenComparingWithNull()
		{
			// Arrange
			var identifier = PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST", Currency.EUR)!;

			// Act & Assert
			identifier.Equals(null).Should().BeFalse();
			(identifier == null).Should().BeFalse();
			(null == identifier).Should().BeFalse();
		}

		[Fact]
		public void Equals_ShouldReturnTrue_WhenComparingSameReference()
		{
			// Arrange
			var identifier = PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST", Currency.EUR)!;

			// Act & Assert
			identifier.Equals(identifier).Should().BeTrue();
			ReferenceEquals(identifier, identifier).Should().BeTrue();
		}

		[Fact]
		public void HashSet_ShouldNotContainDuplicates_WhenUsingEqualityComparison()
		{
			// Arrange
			var hashSet = new HashSet<PartialSymbolIdentifier>();
			var identifier1 = PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Default, "TEST", Currency.EUR)!;
			var identifier2 = new PartialSymbolIdentifier(
				IdentifierType.Default,
				"TEST",
				Currency.EUR,
				new List<AssetClass> { AssetClass.Equity },
				new List<AssetSubClass> { AssetSubClass.Etf, AssetSubClass.Stock }
			);

			// Act
			hashSet.Add(identifier1);
			hashSet.Add(identifier2);

			// Assert
			hashSet.Should().HaveCount(1);
		}

		[Fact]
		public void Dictionary_ShouldUseSameKey_WhenUsingEqualityComparison()
		{
			// Arrange
			var dictionary = new Dictionary<PartialSymbolIdentifier, string>();
			var identifier1 = PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Default, "TEST", Currency.EUR)!;
			var identifier2 = new PartialSymbolIdentifier(
				IdentifierType.Default,
				"TEST",
				Currency.EUR,
				new List<AssetClass> { AssetClass.Equity },
				new List<AssetSubClass> { AssetSubClass.Etf, AssetSubClass.Stock }
			);

			// Act
			dictionary[identifier1] = "Value1";
			dictionary[identifier2] = "Value2";

			// Assert
			dictionary.Should().HaveCount(1);
			dictionary[identifier1].Should().Be("Value2");
		}
	}
}