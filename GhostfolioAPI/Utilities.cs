using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI
{
	internal class Utilities
	{
		internal static T ParseEnum<T>(string value) where T : struct
		{
			if (string.IsNullOrEmpty(value))
			{
				return default;
			}

			return Enum.Parse<T>(value, true);
		}

		internal static T? ParseOptionalEnum<T>(string? value) where T : struct
		{
			if (string.IsNullOrEmpty(value))
			{
				return default;
			}

			if (new T() is AssetSubClass)
			{
				return (T?)Convert.ChangeType(ParseOptionalEnumAssetSubClass(value), typeof(T));
			}

			return Enum.Parse<T>(value, true);
		}

		private static AssetSubClass? ParseOptionalEnumAssetSubClass(string? assetSubClass)
		{
			switch (assetSubClass)
			{
				case "CRYPTOCURRENCY": return AssetSubClass.CryptoCurrency;
				case "ETF": return AssetSubClass.Etf;
				case "STOCK": return AssetSubClass.Stock;
				case "MUTUALFUND": return AssetSubClass.MutualFund;
				case "BOND": return AssetSubClass.Bond;
				case "COMMODITY": return AssetSubClass.Commodity;
				case "PRECIOUS_METAL": return AssetSubClass.PreciousMetal;
				case "PRIVATE_EQUITY": return AssetSubClass.PrivateEquity;
				default:
					throw new NotSupportedException();
			}
		}
	}
}
