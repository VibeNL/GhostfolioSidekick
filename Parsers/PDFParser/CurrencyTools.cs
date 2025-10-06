using System.Globalization;

namespace GhostfolioSidekick.Parsers.CentraalBeheer
{
	public partial class CentraalBeheerParser
	{
		public static class CurrencyTools
		{
			private static readonly IDictionary<string, string> map;
#pragma warning disable S3963 // "static" fields should be initialized inline
			static CurrencyTools()
#pragma warning restore S3963 // "static" fields should be initialized inline
			{
				map = CultureInfo
					.GetCultures(CultureTypes.AllCultures)
					.Where(c => !c.IsNeutralCulture)
					.Select(culture =>
					{
						try
						{
							return new RegionInfo(culture.Name);
						}
						catch
						{
							return null;
						}
					})
					.Where(ri => ri != null)
					.GroupBy(ri => ri!.ISOCurrencySymbol)
					.ToDictionary(x => x.Key, x => x.First()!.CurrencySymbol);
			}

			public static bool TryGetCurrencySymbol(
								  string ISOCurrencySymbol,
								  out string? symbol)
			{
				return map.TryGetValue(ISOCurrencySymbol, out symbol);
			}

			public static string GetCurrencyFromSymbol(string currencySymbol)
			{
				var isoCurrencySymbol = map
					.Where(kvp => kvp.Value == currencySymbol)
					.Select(kvp => kvp.Key)
					.FirstOrDefault() ?? throw new ArgumentException("Currency symbol not found");
				return isoCurrencySymbol;
			}
		}
	}
}
