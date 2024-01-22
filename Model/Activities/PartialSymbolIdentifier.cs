using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities
{
	public class PartialSymbolIdentifier
	{
		private PartialSymbolIdentifier(string id)
		{
			Identifier = id;
		}

		public string Identifier { get; private set; }

		public List<AssetClass>? AllowedAssetClasses {  get; private set; }

		public List<AssetSubClass>? AllowedAssetSubClasses { get; private set; }

		public static PartialSymbolIdentifier CreateStockAndETF(string id)
		{
			return new PartialSymbolIdentifier(id)
			{
				AllowedAssetClasses = [AssetClass.Equity],
				AllowedAssetSubClasses = [AssetSubClass.Etf, AssetSubClass.Stock]
			};
		}
	}
}