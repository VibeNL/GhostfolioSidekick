
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

		public List<AssetClass>? AllowedAssetClasses { get; private set; }

		public List<AssetSubClass>? AllowedAssetSubClasses { get; private set; }

		public static PartialSymbolIdentifier CreateCrypto(string id)
		{
			return new PartialSymbolIdentifier(id)
			{
				AllowedAssetClasses = [AssetClass.Liquidity],
				AllowedAssetSubClasses = [AssetSubClass.CryptoCurrency]
			};
		}

		public static PartialSymbolIdentifier CreateGeneric(string id)
		{
			return new PartialSymbolIdentifier(id);
		}

		public static PartialSymbolIdentifier CreateStockAndETF(string id)
		{
			return new PartialSymbolIdentifier(id)
			{
				AllowedAssetClasses = [AssetClass.Equity],
				AllowedAssetSubClasses = [AssetSubClass.Etf, AssetSubClass.Stock]
			};
		}

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Identifier} [{string.Join(",", AllowedAssetClasses ?? [])}] [{string.Join(",", AllowedAssetSubClasses ?? [])}]";
		}
	}
}