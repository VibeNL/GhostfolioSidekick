using FluentAssertions;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API
{
	public class CacheKeyTests
	{
		[Fact]
		public void Equals_ShouldReturnTrue_WhenCacheKeysAreEqual()
		{
			// Arrange
			var identifiers1 = new string[] { "id1", "id2" };
			var assetClasses1 = new AssetClass[] { AssetClass.Equity, AssetClass.FixedIncome };
			var assetSubClasses1 = new AssetSubClass[] { AssetSubClass.Etf, AssetSubClass.Stock };

			var identifiers2 = new string[] { "id1", "id2" };
			var assetClasses2 = new AssetClass[] { AssetClass.Equity, AssetClass.FixedIncome };
			var assetSubClasses2 = new AssetSubClass[] { AssetSubClass.Etf, AssetSubClass.Stock };

			var cacheKey1 = new CacheKey(identifiers1, assetClasses1, assetSubClasses1);
			var cacheKey2 = new CacheKey(identifiers2, assetClasses2, assetSubClasses2);

			// Act
			var result = cacheKey1.Equals((object)cacheKey2);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void Equals_ShouldReturnFalse_WhenCacheKeysAreNotEqual()
		{
			// Arrange
			var identifiers1 = new string[] { "id1", "id2" };
			var assetClasses1 = new AssetClass[] { AssetClass.Equity, AssetClass.FixedIncome };
			var assetSubClasses1 = new AssetSubClass[] { AssetSubClass.Etf, AssetSubClass.Stock };

			var identifiers2 = new string[] { "id3", "id4" };
			var assetClasses2 = new AssetClass[] { AssetClass.Equity, AssetClass.FixedIncome };
			var assetSubClasses2 = new AssetSubClass[] { AssetSubClass.Etf, AssetSubClass.Stock };

			var cacheKey1 = new CacheKey(identifiers1, assetClasses1, assetSubClasses1);
			var cacheKey2 = new CacheKey(identifiers2, assetClasses2, assetSubClasses2);

			// Act
			var result = cacheKey1.Equals((object)cacheKey2);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void Equals_ShouldReturnFalse_WhenCacheKeysAreNotEqualByAssetSubClass()
		{
			// Arrange
			var identifiers1 = new string[] { "id1", "id2" };
			var assetClasses1 = new AssetClass[] { AssetClass.Equity, AssetClass.FixedIncome };
			var assetSubClasses1 = new AssetSubClass[] { AssetSubClass.Etf, AssetSubClass.Stock };

			var identifiers2 = new string[] { "id1", "id2" };
			var assetClasses2 = new AssetClass[] { AssetClass.Equity, AssetClass.FixedIncome };
			var assetSubClasses2 = new AssetSubClass[] { AssetSubClass.Etf, AssetSubClass.PrivateEquity };

			var cacheKey1 = new CacheKey(identifiers1, assetClasses1, assetSubClasses1);
			var cacheKey2 = new CacheKey(identifiers2, assetClasses2, assetSubClasses2);

			// Act
			var result = cacheKey1.Equals((object)cacheKey2);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void Equals_ShouldReturnFalse_WhenCacheKeysAreNotEqualByNullArrays()
		{
			// Arrange
			var identifiers1 = new string[] { "id1", "id2" };
			var assetClasses1 = new AssetClass[] { AssetClass.Equity, AssetClass.FixedIncome };
			var assetSubClasses1 = new AssetSubClass[] { AssetSubClass.Etf, AssetSubClass.Stock };

			var identifiers2 = new string[] { "id1", "id2" };

			var cacheKey1 = new CacheKey(identifiers1, assetClasses1, assetSubClasses1);
			var cacheKey2 = new CacheKey(identifiers2, null, null);

			// Act
			var result = cacheKey1.Equals((object)cacheKey2);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void GetHashCode_ShouldReturnSameHashCode_WhenCacheKeysAreEqual()
		{
			// Arrange
			var identifiers1 = new string[] { "id1", "id2" };
			var assetClasses1 = new AssetClass[] { AssetClass.Equity, AssetClass.FixedIncome };
			var assetSubClasses1 = new AssetSubClass[] { AssetSubClass.Etf, AssetSubClass.Stock };

			var identifiers2 = new string[] { "id1", "id2" };
			var assetClasses2 = new AssetClass[] { AssetClass.Equity, AssetClass.FixedIncome };
			var assetSubClasses2 = new AssetSubClass[] { AssetSubClass.Etf, AssetSubClass.Stock };

			var cacheKey1 = new CacheKey(identifiers1, assetClasses1, assetSubClasses1);
			var cacheKey2 = new CacheKey(identifiers2, assetClasses2, assetSubClasses2);

			// Act
			var hashCode1 = cacheKey1.GetHashCode();
			var hashCode2 = cacheKey2.GetHashCode();

			// Assert
			hashCode1.Should().Be(hashCode2);
		}

		[Fact]
		public void Equals_ShouldReturnFalse_WhenSecondCacheKeyIsNull()
		{
			// Arrange
			var identifiers1 = new string[] { "id1", "id2" };
			var assetClasses1 = new AssetClass[] { AssetClass.Equity, AssetClass.FixedIncome };
			var assetSubClasses1 = new AssetSubClass[] { AssetSubClass.Etf, AssetSubClass.Stock };

			CacheKey cacheKey1 = new CacheKey(identifiers1, assetClasses1, assetSubClasses1);
			CacheKey cacheKey2 = null;

			// Act
			var result = cacheKey1.Equals(cacheKey2);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void Equals_ShouldReturnFalse_IncompatibleTypes()
		{
			// Arrange
			var identifiers1 = new string[] { "id1", "id2" };
			var assetClasses1 = new AssetClass[] { AssetClass.Equity, AssetClass.FixedIncome };
			var assetSubClasses1 = new AssetSubClass[] { AssetSubClass.Etf, AssetSubClass.Stock };

			CacheKey cacheKey1 = new CacheKey(identifiers1, assetClasses1, assetSubClasses1);
			string cacheKey2 = "yo";

			// Act
			var result = cacheKey1.Equals(cacheKey2);

			// Assert
			result.Should().BeFalse();
		}
	}
}
