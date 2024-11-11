using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI.API.Mapper
{
	public static class EnumMapper
	{
		private const string CryptoCurrency = "CRYPTOCURRENCY";
		private const string Etf = "ETF";
		private const string Stock = "STOCK";
		private const string MutualFund = "MUTUALFUND";
		private const string Bond = "BOND";
		private const string Commodity = "COMMODITY";
		private const string PreciousMetal = "PRECIOUS_METAL";
		private const string PrivateEquity = "PRIVATE_EQUITY";
		private const string Liquidity = "LIQUIDITY";
		private const string Equity = "EQUITY";
		private const string FixedIncome = "FIXED_INCOME";
		private const string RealEstate = "REAL_ESTATE";

		public static AssetSubClass? ParseAssetSubClass(string? value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return default;
			}

			return value switch
			{
				CryptoCurrency => AssetSubClass.CryptoCurrency,
				Etf => AssetSubClass.Etf,
				Stock => AssetSubClass.Stock,
				MutualFund => AssetSubClass.MutualFund,
				Bond => AssetSubClass.Bond,
				Commodity => AssetSubClass.Commodity,
				PreciousMetal => AssetSubClass.PreciousMetal,
				PrivateEquity => AssetSubClass.PrivateEquity,
				_ => throw new NotSupportedException(),
			};
		}

		public static AssetClass ParseAssetClass(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return AssetClass.Undefined;
			}

			return value switch
			{
				Liquidity => AssetClass.Liquidity,
				Equity => AssetClass.Equity,
				FixedIncome => AssetClass.FixedIncome,
				RealEstate => AssetClass.RealEstate,
				Commodity => AssetClass.Commodity,
				_ => throw new NotSupportedException(),
			};
		}

		public static string ConvertAssetSubClassToString(AssetSubClass? assetSubClass)
		{
			if (assetSubClass == null)
			{
				return string.Empty;
			}

			return assetSubClass switch
			{
				AssetSubClass.CryptoCurrency => CryptoCurrency,
				AssetSubClass.Etf => Etf,
				AssetSubClass.Stock => Stock,
				AssetSubClass.MutualFund => MutualFund,
				AssetSubClass.Bond => Bond,
				AssetSubClass.Commodity => Commodity,
				AssetSubClass.PreciousMetal => PreciousMetal,
				AssetSubClass.PrivateEquity => PrivateEquity,
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
				AssetClass.Liquidity => Liquidity,
				AssetClass.Equity => Equity,
				AssetClass.FixedIncome => FixedIncome,
				AssetClass.RealEstate => RealEstate,
				AssetClass.Commodity => Commodity,
				_ => throw new NotSupportedException(),
			};
		}
	}
}
