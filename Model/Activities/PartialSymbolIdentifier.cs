namespace GhostfolioSidekick.Model.Activities
{
	public record PartialSymbolIdentifier
	{
       public PartialSymbolIdentifier()
	  {
		  // EF Core
		  Identifier = null!;
		  Currency = Currency.NONE;
	  }

       private PartialSymbolIdentifier(string id, Currency currency)
	   {
		   if (string.IsNullOrWhiteSpace(id))
		   {
			   throw new ArgumentException("Identifier cannot be null or whitespace.", nameof(id));
		   }

		   Identifier = id;
		   Currency = currency;
	   }

       public string Identifier { get; set; }
	   public Currency Currency { get; set; }

		public List<AssetClass>? AllowedAssetClasses { get; set; }

		public List<AssetSubClass>? AllowedAssetSubClasses { get; set; }

       public static PartialSymbolIdentifier CreateCrypto(string id, Currency currency)
	  {
		  return new PartialSymbolIdentifier(id, currency)
		  {
			  AllowedAssetClasses = [AssetClass.Liquidity],
			  AllowedAssetSubClasses = [AssetSubClass.CryptoCurrency]
		  };
	  }

       public static PartialSymbolIdentifier CreateGeneric(string id, Currency currency)
	  {
		  return new PartialSymbolIdentifier(id, currency);
	  }

       public static PartialSymbolIdentifier[] CreateGeneric(params (string? id, Currency currency)[] items)
	  {
		  return [.. items.Where(x => !string.IsNullOrWhiteSpace(x.id)).Select(x => CreateGeneric(x.id!, x.currency))];
	  }
       // Remove the string-only overload to enforce currency requirement

       public static PartialSymbolIdentifier CreateStockAndETF(string id, Currency currency)
	  {
		  return new PartialSymbolIdentifier(id, currency)
		  {
			  AllowedAssetClasses = [AssetClass.Equity],
			  AllowedAssetSubClasses = [AssetSubClass.Etf, AssetSubClass.Stock]
		  };
	  }

       public static PartialSymbolIdentifier[] CreateStockAndETF(params (string? id, Currency preferredCurrency)[] items)
	  {
		  return [.. items.Where(x => !string.IsNullOrWhiteSpace(x.id)).Select(x => CreateStockAndETF(x.id!, x.preferredCurrency))];
	  }
       // Remove the string-only overload to enforce currency requirement

       public static PartialSymbolIdentifier CreateStockBondAndETF(string id, Currency preferredCurrency)
	  {
		  return new PartialSymbolIdentifier(id, preferredCurrency)
		  {
			  AllowedAssetClasses = [AssetClass.Equity],
			  AllowedAssetSubClasses = [AssetSubClass.Etf, AssetSubClass.Stock, AssetSubClass.Bond]
		  };
	  }

       public virtual bool Equals(PartialSymbolIdentifier? other)
       {
		   if (other is null) return false;
		   if (ReferenceEquals(this, other)) return true;

		   bool currencyEqual =
			   Equals(Currency, GhostfolioSidekick.Model.Currency.NONE)
			   || Equals(other.Currency, GhostfolioSidekick.Model.Currency.NONE)
			   || Equals(Currency, other.Currency);

		   return string.Equals(Identifier.Trim(), other.Identifier.Trim(), StringComparison.InvariantCultureIgnoreCase)
			   && ListsEqual(AllowedAssetClasses, other.AllowedAssetClasses)
			   && ListsEqual(AllowedAssetSubClasses, other.AllowedAssetSubClasses)
			   && currencyEqual;
	   }

       public override int GetHashCode()
       {
		  var hash = new HashCode();
		   hash.Add(StringComparer.InvariantCultureIgnoreCase.GetHashCode(Identifier.Trim()));
		   // Do NOT include asset class or subclass in hash code, as equality is lenient/compatible
		   // Only include Currency if not NONE, to match comparer wildcard logic
		   // If Currency is NONE, do not add it; if not, add its value
		   if (!Equals(Currency, GhostfolioSidekick.Model.Currency.NONE))
		   {
			   hash.Add(Currency);
		   }
		   // If Currency is NONE, hash is same as if any currency (wildcard)
		   return hash.ToHashCode();
	   }

	   private static int GetCompatibleAssetClassHash(List<AssetClass>? list)
       {
		   // If compatible with any (null, empty, or contains Undefined), always return 0
		   if (IsAssetClassListCompatible(list))
		   {
			   return 0;
		   }
           unchecked
		   {
			   int hash = 17;
			   foreach (var item in list!.OrderBy(x => x))
			   {
				   hash = hash * 23 + item.GetHashCode();
			   }
			   return hash;
		   }
	   }

	   private static bool IsAssetClassListCompatible(List<AssetClass>? list)
	   {
		   return list == null || list.Count == 0 || list.Contains(AssetClass.Undefined);
	   }

	   private static int GetCompatibleAssetSubClassHash(List<AssetSubClass>? list)
       {
		   // If compatible with any (null, empty, or contains Undefined), always return 0
		   if (IsAssetSubClassListCompatible(list))
		   {
			   return 0;
		   }
           unchecked
		   {
			   int hash = 17;
			   foreach (var item in list!.OrderBy(x => x))
			   {
				   hash = hash * 23 + item.GetHashCode();
			   }
			   return hash;
		   }
	   }

	   private static bool IsAssetSubClassListCompatible(List<AssetSubClass>? list)
	   {
		   return list == null || list.Count == 0 || list.Contains(AssetSubClass.Undefined);
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
		   return $"{Identifier}([{string.Join(",", AllowedAssetClasses ?? [])}][{string.Join(",", AllowedAssetSubClasses ?? [])}][{Currency}])";
	   }
	}
}