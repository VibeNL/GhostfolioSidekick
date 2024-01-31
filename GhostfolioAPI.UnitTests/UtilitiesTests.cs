using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests
{

	public class UtilitiesTests
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
		[InlineData(null, AssetSubClass.Undefined)]
		[InlineData("", AssetSubClass.Undefined)]
		public void ParseEnum_AssetSubClass_ValidInput_ReturnsExpectedResult(string input, AssetSubClass expectedResult)
		{
			// Act
			var result = Utilities.ParseEnum<AssetSubClass>(input);

			// Assert
			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData("CASH", AssetClass.Cash)]
		[InlineData("COMMODITY", AssetClass.Commodity)]
		[InlineData("EQUITY", AssetClass.Equity)]
		[InlineData("FIXEDINCOME", AssetClass.FixedIncome)]
		[InlineData("REALESTATE", AssetClass.RealEstate)]
		[InlineData(null, AssetClass.Undefined)]
		[InlineData("", AssetClass.Undefined)]
		public void ParseOptionalEnumAsset_AssetClass_ValidInput_ReturnsExpectedResult(string input, AssetClass expectedResult)
		{
			// Act
			var result = Utilities.ParseEnum<AssetClass>(input);

			// Assert
			Assert.Equal(expectedResult, result);
		}

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
		public void ParseOptionalEnum_ValidInput_ReturnsExpectedResult(string? input, AssetSubClass? expectedResult)
		{
			// Act
			var result = Utilities.ParseOptionalEnum<AssetSubClass>(input);

			// Assert
			Assert.Equal(expectedResult, result);
		}
	}
}