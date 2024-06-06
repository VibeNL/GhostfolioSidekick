using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API.Mapper
{

	public class EnumMapperTests
	{
		[Theory]
		[InlineData("CRYPTOCURRENCY", AssetSubClass.CryptoCurrency)]
		[InlineData("ETF", AssetSubClass.Etf)]
		[InlineData("STOCK", AssetSubClass.Stock)]
		[InlineData("MUTUALFUND", AssetSubClass.MutualFund)]
		[InlineData("BOND", AssetSubClass.Bond)]
		[InlineData("COMMODITY", AssetSubClass.Commodity)]
		[InlineData("PRECIOUS_METAL", AssetSubClass.PreciousMetal)]
		[InlineData("PRIVATE_EQUITY", AssetSubClass.PrivateEquity)]
		[InlineData(null, null)]
		[InlineData("", null)]
		public void ParseEnum_AssetSubClass_ValidInput_ReturnsExpectedResult(string? input, AssetSubClass? expectedResult)
		{
			// Act
			var result = EnumMapper.ParseAssetSubClass(input);

			// Assert
			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void ParseAssetSubClass_InvalidInput_ThrowsNotSupportedException()
		{
			Assert.Throws<NotSupportedException>(() => EnumMapper.ParseAssetSubClass("INVALID"));
		}

		[Theory]
		[InlineData("LIQUIDITY", AssetClass.Liquidity)]
		[InlineData("COMMODITY", AssetClass.Commodity)]
		[InlineData("EQUITY", AssetClass.Equity)]
		[InlineData("FIXED_INCOME", AssetClass.FixedIncome)]
		[InlineData("REAL_ESTATE", AssetClass.RealEstate)]
		[InlineData(null, AssetClass.Undefined)]
		[InlineData("", AssetClass.Undefined)]
		public void ParseOptionalEnumAsset_AssetClass_ValidInput_ReturnsExpectedResult(string? input, AssetClass expectedResult)
		{
			// Act
			var result = EnumMapper.ParseAssetClass(input!);

			// Assert
			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void ParseAssetClass_InvalidInput_ThrowsNotSupportedException()
		{
			Assert.Throws<NotSupportedException>(() => EnumMapper.ParseAssetClass("INVALID"));
		}

		[Theory]
		[InlineData(AssetSubClass.CryptoCurrency, "CRYPTOCURRENCY")]
		[InlineData(AssetSubClass.Etf, "ETF")]
		[InlineData(AssetSubClass.Stock, "STOCK")]
		[InlineData(AssetSubClass.MutualFund, "MUTUALFUND")]
		[InlineData(AssetSubClass.Bond, "BOND")]
		[InlineData(AssetSubClass.Commodity, "COMMODITY")]
		[InlineData(AssetSubClass.PreciousMetal, "PRECIOUS_METAL")]
		[InlineData(AssetSubClass.PrivateEquity, "PRIVATE_EQUITY")]
		[InlineData(null, "")]
		public void ConvertAssetSubClassToString_ValidInput_ReturnsExpectedResult(AssetSubClass? input, string expectedResult)
		{
			// Act
			var result = EnumMapper.ConvertAssetSubClassToString(input);

			// Assert
			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void ConvertAssetSubClassToString_InvalidInput_ThrowsNotSupportedException()
		{
			// Arrange
			var invalidInput = (AssetSubClass)999;

			// Act & Assert
			Assert.Throws<NotSupportedException>(() => EnumMapper.ConvertAssetSubClassToString(invalidInput));
		}

		[Theory]
		[InlineData(AssetClass.Liquidity, "LIQUIDITY")]
		[InlineData(AssetClass.Equity, "EQUITY")]
		[InlineData(AssetClass.FixedIncome, "FIXED_INCOME")]
		[InlineData(AssetClass.RealEstate, "REAL_ESTATE")]
		[InlineData(AssetClass.Commodity, "COMMODITY")]
		[InlineData(null, "")]
		public void ConvertAssetClassToString_ValidInput_ReturnsExpectedResult(AssetClass? input, string expectedResult)
		{
			// Act
			var result = EnumMapper.ConvertAssetClassToString(input);

			// Assert
			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void ConvertAssetClassToString_InvalidInput_ThrowsNotSupportedException()
		{
			// Arrange
			var invalidInput = (AssetClass)999;

			// Act & Assert
			Assert.Throws<NotSupportedException>(() => EnumMapper.ConvertAssetClassToString(invalidInput));
		}
	}
}