namespace GhostfolioSidekick.Model
{
	public static class DefaultSetsOfAssetClasses
	{
		public static AssetClass?[] StockBrokerDefaultSetAssestClasses = new AssetClass?[] { AssetClass.EQUITY };

		public static AssetSubClass?[] StockBrokerDefaultSetAssetSubClasses = new AssetSubClass?[] { AssetSubClass.STOCK, AssetSubClass.ETF };

		public static AssetClass?[] CryptoBrokerDefaultSetAssestClasses = new AssetClass?[] { AssetClass.CASH };

		public static AssetSubClass?[] CryptoBrokerDefaultSetAssetSubClasses = new AssetSubClass?[] { AssetSubClass.CRYPTOCURRENCY };
	}
}