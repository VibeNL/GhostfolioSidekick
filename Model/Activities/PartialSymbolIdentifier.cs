namespace GhostfolioSidekick.Model.Activities
{
	public record PartialSymbolIdentifier
	{
		public PartialSymbolIdentifier(IdentifierType identifierType, string identifier, Currency? currency, List<AssetClass> allowedAssetClasses, List<AssetSubClass> allowedAssetSubClasses)
		{
			if (string.IsNullOrWhiteSpace(identifier))
			{
				throw new ArgumentException("Identifier cannot be null or whitespace.", nameof(identifier));
			}

			Identifier = identifier;
			IdentifierType = identifierType;
			Currency = currency;
			AllowedAssetClasses = allowedAssetClasses;
			AllowedAssetSubClasses = allowedAssetSubClasses;
		}

		public string Identifier { get; set; }

		public List<AssetClass> AllowedAssetClasses { get; set; }

		public List<AssetSubClass> AllowedAssetSubClasses { get; set; }

		public Currency? Currency { get; set; }

		public IdentifierType IdentifierType { get; set; }

		public static PartialSymbolIdentifier? CreateCrypto(IdentifierType identifierType, string? id, Currency? currency)
		{
			if (string.IsNullOrWhiteSpace(id)) return null;
			return new PartialSymbolIdentifier(identifierType, id, currency, [AssetClass.Liquidity], [AssetSubClass.CryptoCurrency]);
		}

		public static PartialSymbolIdentifier? CreateGeneric(IdentifierType identifierType, string? id, Currency? currency)
		{
			if (string.IsNullOrWhiteSpace(id)) return null;
			return new PartialSymbolIdentifier(identifierType, id, currency, [], []);
		}

		public static PartialSymbolIdentifier? CreateStockAndETF(IdentifierType identifierType, string? id, Currency? currency)
		{
			if (string.IsNullOrWhiteSpace(id)) return null;
			return new PartialSymbolIdentifier(identifierType, id, currency, [AssetClass.Equity], [AssetSubClass.Etf, AssetSubClass.Stock]);
		}

		public static PartialSymbolIdentifier? CreateStockBondAndETF(IdentifierType identifierType, string? id, Currency? currency)
		{
			if (string.IsNullOrWhiteSpace(id)) return null;
			return new PartialSymbolIdentifier(identifierType, id, currency, [AssetClass.Equity], [AssetSubClass.Etf, AssetSubClass.Stock, AssetSubClass.Bond]);
		}

		public virtual bool Equals(PartialSymbolIdentifier? other)
		{
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;

			return string.Equals(Identifier.Trim(), other.Identifier.Trim(), StringComparison.InvariantCultureIgnoreCase)
				&& IdentifierType == other.IdentifierType
				&& Currency == other.Currency
				&& ListsEqual(AllowedAssetClasses, other.AllowedAssetClasses)
				&& ListsEqual(AllowedAssetSubClasses, other.AllowedAssetSubClasses);
		}

		public override int GetHashCode()
		{
          var hash = new HashCode();
			hash.Add(StringComparer.InvariantCultureIgnoreCase.GetHashCode(Identifier.Trim()));
			hash.Add(IdentifierType);
			hash.Add(Currency);
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
			return $"{Identifier} ({IdentifierType}, {Currency}) ([{string.Join(",", AllowedAssetClasses ?? [])}][{string.Join(",", AllowedAssetSubClasses ?? [])}])";
		}
	}
}