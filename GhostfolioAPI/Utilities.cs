using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public class Utilities
	{
		public static AssetSubClass? ParseAssetSubClass(string? value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return default;
			}

			return (value switch
			{
				"CRYPTOCURRENCY" => (AssetSubClass?)AssetSubClass.CryptoCurrency,
				"ETF" => (AssetSubClass?)AssetSubClass.Etf,
				"STOCK" => (AssetSubClass?)AssetSubClass.Stock,
				"MUTUALFUND" => (AssetSubClass?)AssetSubClass.MutualFund,
				"BOND" => (AssetSubClass?)AssetSubClass.Bond,
				"COMMODITY" => (AssetSubClass?)AssetSubClass.Commodity,
				"PRECIOUS_METAL" => (AssetSubClass?)AssetSubClass.PreciousMetal,
				"PRIVATE_EQUITY" => (AssetSubClass?)AssetSubClass.PrivateEquity,
				_ => throw new NotSupportedException(),
			})!.Value;
		}

		public static AssetClass ParseAssetClass(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return AssetClass.Undefined;
			}

			return (value switch
			{
				"CASH" => (AssetClass?)AssetClass.Cash,
				"EQUITY" => (AssetClass?)AssetClass.Equity,
				"FIXED_INCOME" => (AssetClass?)AssetClass.FixedIncome,
				"REAL_ESTATE" => (AssetClass?)AssetClass.RealEstate,
				"COMMODITY" => (AssetClass?)AssetClass.Commodity,
				_ => throw new NotSupportedException(),
			})!.Value;
		}

		public static string ConvertAssetSubClassToString(AssetSubClass? assetSubClass)
		{
			if (assetSubClass == null)
			{
				return string.Empty;
			}

			return assetSubClass switch
			{
				AssetSubClass.CryptoCurrency => "CRYPTOCURRENCY",
				AssetSubClass.Etf => "ETF",
				AssetSubClass.Stock => "STOCK",
				AssetSubClass.MutualFund => "MUTUALFUND",
				AssetSubClass.Bond => "BOND",
				AssetSubClass.Commodity => "COMMODITY",
				AssetSubClass.PreciousMetal => "PRECIOUS_METAL",
				AssetSubClass.PrivateEquity => "PRIVATE_EQUITY",
				_ => throw new NotSupportedException(),
			};
		}

		public static string ConvertAssetClassToString(AssetClass? assetClass)
		{
			if (assetClass == null)
			{
				return string.Empty;
			}

			return assetClass switch
			{
				AssetClass.Cash => "CASH",
				AssetClass.Equity => "EQUITY",
				AssetClass.FixedIncome => "FIXED_INCOME",
				AssetClass.RealEstate => "REAL_ESTATE",
				AssetClass.Commodity => "COMMODITY",
				_ => throw new NotSupportedException(),
			};
		}
	}
}
