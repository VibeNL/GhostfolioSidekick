namespace GhostfolioSidekick.Model.Activities
{
	public record PartialSymbolIdentifier
	{
		public PartialSymbolIdentifier()
		{
			// EF Core
			Identifier = null!;
		}

		private PartialSymbolIdentifier(string id)
		{
			Identifier = id;
		}

		public string Identifier { get; set; }

		public List<AssetClass>? AllowedAssetClasses { get; set; }

		public List<AssetSubClass>? AllowedAssetSubClasses { get; set; }
		
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

		public static PartialSymbolIdentifier[] CreateGeneric(params string?[] ids)
		{
			return [.. ids.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => CreateGeneric(x!))];
		}

		public static PartialSymbolIdentifier CreateStockAndETF(string id)
		{
			return new PartialSymbolIdentifier(id)
			{
				AllowedAssetClasses = [AssetClass.Equity],
				AllowedAssetSubClasses = [AssetSubClass.Etf, AssetSubClass.Stock]
			};
		}

		public static PartialSymbolIdentifier[] CreateStockAndETF(params string?[] ids)
		{
			return [.. ids.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => CreateStockAndETF(x!))];
		}

		public static PartialSymbolIdentifier CreateStockBondAndETF(string id)
		{
			return new PartialSymbolIdentifier(id)
			{
				AllowedAssetClasses = [AssetClass.Equity],
				AllowedAssetSubClasses = [AssetSubClass.Etf, AssetSubClass.Stock, AssetSubClass.Bond]
			};
		}

		public virtual bool Equals(PartialSymbolIdentifier? other)
		{
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;

			return string.Equals(Identifier.Trim(), other.Identifier.Trim(), StringComparison.InvariantCultureIgnoreCase) &&
				   ListsEqual(AllowedAssetClasses, other.AllowedAssetClasses) &&
				   ListsEqual(AllowedAssetSubClasses, other.AllowedAssetSubClasses);
		}

		public override int GetHashCode()
		{
			var hash = new HashCode();
			hash.Add(StringComparer.OrdinalIgnoreCase.GetHashCode(Identifier.Trim()));
			hash.Add(GetListHashCode(AllowedAssetClasses));
			hash.Add(GetListHashCode(AllowedAssetSubClasses));
			return hash.ToHashCode();
		}

		private static bool ListsEqual<T>(List<T>? list1, List<T>? list2)
		{
			if (list1 is null && list2 is null) return true;
			if (list1 is null || list2 is null) return false;
			if (list1.Count != list2.Count) return false;

			// Sort both lists for consistent comparison
			var sorted1 = list1.OrderBy(x => x).ToList();
			var sorted2 = list2.OrderBy(x => x).ToList();

			return sorted1.SequenceEqual(sorted2);
		}

		private static int GetListHashCode<T>(List<T>? list)
		{
			if (list is null) return 0;

			var hash = new HashCode();
			// Sort for consistent hash code regardless of order
			foreach (var item in list.OrderBy(x => x))
			{
				hash.Add(item);
			}
			return hash.ToHashCode();
		}

		public override string ToString()
		{
			return $"{Identifier}([{string.Join(",", AllowedAssetClasses ?? [])}][{string.Join(",", AllowedAssetSubClasses ?? [])}])";
		}
	}
}