using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Model.Activities;
using AwesomeAssertions;
using GhostfolioSidekick.Model.Activities.Types;

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
				new PartialSymbolIdentifier { Identifier = "ID1" },
				new PartialSymbolIdentifier { Identifier = "ID2" }
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
			var existingIdentifier = new PartialSymbolIdentifier { Identifier = "ID1" };
			holding.PartialSymbolIdentifiers.Add(existingIdentifier);

			var newIdentifiers = new List<PartialSymbolIdentifier>
			{
				new PartialSymbolIdentifier { Identifier = "ID1" },
				new PartialSymbolIdentifier { Identifier = "ID2" }
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
				SymbolProfiles = new List<SymbolProfile>
				{
					new SymbolProfile { Symbol = "SYM" }
				},
				Activities = new List<Activity>
				{
					new BuySellActivity(),
					new BuySellActivity()
				}
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
			var existingIdentifier = new PartialSymbolIdentifier { Identifier = "ID1" };
			holding.PartialSymbolIdentifiers.Add(existingIdentifier);

			var newIdentifier = new PartialSymbolIdentifier { Identifier = "ID1" };

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
			var existingIdentifier = new PartialSymbolIdentifier { Identifier = "ID1" };
			holding.PartialSymbolIdentifiers.Add(existingIdentifier);

			var newIdentifier = new PartialSymbolIdentifier { Identifier = "ID2" };

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
			var existingIdentifier = new PartialSymbolIdentifier
			{
				Identifier = "ID1",
				AllowedAssetClasses = [AssetClass.RealEstate]
			};
			holding.PartialSymbolIdentifiers.Add(existingIdentifier);

			var newIdentifier = new PartialSymbolIdentifier
			{
				Identifier = "ID1",
				AllowedAssetClasses = [AssetClass.Liquidity]
			};

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
			var existingIdentifier = new PartialSymbolIdentifier
			{
				Identifier = "ID1",
				AllowedAssetClasses = [AssetClass.RealEstate]
			};
			holding.PartialSymbolIdentifiers.Add(existingIdentifier);

			var newIdentifier = new PartialSymbolIdentifier
			{
				Identifier = "ID1",
				AllowedAssetClasses = []
			};

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
			var existingIdentifier = new PartialSymbolIdentifier
			{
				Identifier = "ID1",
				AllowedAssetSubClasses = [AssetSubClass.Commodity]
			};
			holding.PartialSymbolIdentifiers.Add(existingIdentifier);

			var newIdentifier = new PartialSymbolIdentifier
			{
				Identifier = "ID1",
				AllowedAssetSubClasses = []
			};

			// Act
			var result = holding.IdentifierContainsInList(newIdentifier);

			// Assert
			result.Should().BeTrue();
		}
	}
}
