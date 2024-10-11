using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Activities
{
	public record PartialSymbolIdentifier
	{
		internal PartialSymbolIdentifier()
		{
			// EF Core
			Identifier = null!;
			SymbolProfiles = new List<SymbolProfile>();
			Activities = new List<Activity>();
		}

		private PartialSymbolIdentifier(string id)
		{
			Identifier = id;
			SymbolProfiles = new List<SymbolProfile>();
			Activities = new List<Activity>();
		}

		public string Identifier { get; private set; }

		public List<AssetClass>? AllowedAssetClasses { get; private set; }

		public List<AssetSubClass>? AllowedAssetSubClasses { get; private set; }
		
		public int Id { get; set; }

		public ICollection<SymbolProfile> SymbolProfiles { get; set; }

		public ICollection<Activity> Activities { get; set; }

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

		public static PartialSymbolIdentifier CreateStockBondAndETF(string id)
		{
			return new PartialSymbolIdentifier(id)
			{
				AllowedAssetClasses = [AssetClass.Equity],
				AllowedAssetSubClasses = [AssetSubClass.Etf, AssetSubClass.Stock, AssetSubClass.Bond]
			};
		}

		public override string ToString()
		{
			return $"{Identifier} [{string.Join(",", AllowedAssetClasses ?? [])}] [{string.Join(",", AllowedAssetSubClasses ?? [])}]";
		}
	}
}