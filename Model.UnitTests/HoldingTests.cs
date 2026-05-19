using AwesomeAssertions;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.UnitTests
{
	public class HoldingTests
	{
		[Fact]
		public void MergeIdentifiers_ShouldAddNewIdentifiers()
		{
			// Arrange
			var holding = new Holding();
          var newIdentifiers = new List<PartialSymbolIdentifier>
			{
				new PartialSymbolIdentifier(IdentifierType.Default, "ID1", Currency.EUR, [], []),
				new PartialSymbolIdentifier(IdentifierType.Default, "ID2", Currency.EUR, [], [])
			};

			// Act
			holding.MergeIdentifiers(newIdentifiers);

			// Assert
			holding.PartialSymbolIdentifiers.Should().HaveCount(2);
			holding.PartialSymbolIdentifiers.Should().Contain(id => id.Identifier == "ID1");
			holding.PartialSymbolIdentifiers.Should().Contain(id => id.Identifier == "ID2");
		}

		[Fact]
		public void MergeIdentifiers_ShouldNotAddDuplicateIdentifiers()
		{
			// Arrange
			var holding = new Holding();
            var existingIdentifier = new PartialSymbolIdentifier(IdentifierType.Default, "ID1", Currency.EUR, [], []);
			holding.PartialSymbolIdentifiers.Add(existingIdentifier);

			var newIdentifiers = new List<PartialSymbolIdentifier>
			{
				new PartialSymbolIdentifier(IdentifierType.Default, "ID1", Currency.EUR, [], []),
				new PartialSymbolIdentifier(IdentifierType.Default, "ID2", Currency.EUR, [], [])
			};

			// Act
			holding.MergeIdentifiers(newIdentifiers);

			// Assert
			holding.PartialSymbolIdentifiers.Should().HaveCount(2);
			holding.PartialSymbolIdentifiers.Should().Contain(id => id.Identifier == "ID1");
			holding.PartialSymbolIdentifiers.Should().Contain(id => id.Identifier == "ID2");
		}

		[Fact]
		public void ToString_ShouldReturnCorrectString()
		{
			// Arrange
			var holding = new Holding
			{
				SymbolProfiles =
				[
					new SymbolProfile { Symbol = "SYM" }
				],
				Activities =
				[
					new BuyActivity(),
					new SellActivity()
				]
			};

			// Act
			var result = holding.ToString();

			// Assert
			result.Should().Be("SYM - 2 activities");
		}

		[Fact]
		public void IdentifierContainsInList_ShouldReturnTrueIfIdentifierExists()
		{
			// Arrange
			var holding = new Holding();
            var existingIdentifier = new PartialSymbolIdentifier(IdentifierType.Default, "ID1", Currency.EUR, [], []);
			holding.PartialSymbolIdentifiers.Add(existingIdentifier);

			var newIdentifier = new PartialSymbolIdentifier(IdentifierType.Default, "ID1", Currency.EUR, [], []);

			// Act
			var result = holding.IdentifierContainsInList(newIdentifier);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void IdentifierContainsInList_ShouldReturnFalseIfIdentifierDoesNotExist()
		{
			// Arrange
			var holding = new Holding();
            var existingIdentifier = new PartialSymbolIdentifier(IdentifierType.Default, "ID1", Currency.EUR, [], []);
			holding.PartialSymbolIdentifiers.Add(existingIdentifier);

			var newIdentifier = new PartialSymbolIdentifier(IdentifierType.Default, "ID2", Currency.EUR, [], []);

			// Act
			var result = holding.IdentifierContainsInList(newIdentifier);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void IdentifierContainsInList_ShouldReturnFalseIfAssetClassMismatch()
		{
			// Arrange
			var holding = new Holding();
            var existingIdentifier = new PartialSymbolIdentifier(
				IdentifierType.Default,
				"ID1",
				Currency.EUR,
				[AssetClass.RealEstate],
				[]
			);
			holding.PartialSymbolIdentifiers.Add(existingIdentifier);

			var newIdentifier = new PartialSymbolIdentifier(
				IdentifierType.Default,
				"ID1",
				Currency.EUR,
				[AssetClass.Liquidity],
				[]
			);

			// Act
			var result = holding.IdentifierContainsInList(newIdentifier);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void IdentifierContainsInList_ShouldReturnTrueIfAssetClassMismatchButIsEmpty()
		{
			// Arrange
			var holding = new Holding();
            var existingIdentifier = new PartialSymbolIdentifier(
				IdentifierType.Default,
				"ID1",
				Currency.EUR,
				[AssetClass.RealEstate],
				[]
			);
			holding.PartialSymbolIdentifiers.Add(existingIdentifier);

			var newIdentifier = new PartialSymbolIdentifier(
				IdentifierType.Default,
				"ID1",
				Currency.EUR,
				[],
				[]
			);

			// Act
			var result = holding.IdentifierContainsInList(newIdentifier);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void IdentifierContainsInList_ShouldReturnTrueIfAssetSubClassMismatchButIsEmpty()
		{
			// Arrange
			var holding = new Holding();
            var existingIdentifier = new PartialSymbolIdentifier(
				IdentifierType.Default,
				"ID1",
				Currency.EUR,
				[],
				[AssetSubClass.Commodity]
			);
			holding.PartialSymbolIdentifiers.Add(existingIdentifier);

			var newIdentifier = new PartialSymbolIdentifier(
				IdentifierType.Default,
				"ID1",
				Currency.EUR,
				[],
				[]
			);

			// Act
			var result = holding.IdentifierContainsInList(newIdentifier);

			// Assert
			result.Should().BeTrue();
		}
	}
}
