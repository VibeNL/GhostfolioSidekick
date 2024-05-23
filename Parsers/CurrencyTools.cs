using System.Globalization;

namespace GhostfolioSidekick.Parsers
{
	public static class CurrencyTools
	{
		private static IDictionary<string, string>? map;

		public static IDictionary<string, string> Map
		{
			get
			{
				if (map == null)
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

				return map;
			}
		}

		public static bool TryGetCurrencySymbol(
							  string ISOCurrencySymbol,
							  out string? symbol)
		{
			return Map.TryGetValue(ISOCurrencySymbol, out symbol);
		}

		public static string GetCurrencyFromSymbol(string currencySymbol)
		{
			var isoCurrencySymbol = Map
				.Where(kvp => kvp.Value == currencySymbol)
				.Select(kvp => kvp.Key)
				.FirstOrDefault();

			if (currencySymbol == "$") // Dollar is used by multiple countries
			{
				return "USD";
			}

			if (isoCurrencySymbol == null)
			{
				throw new ArgumentException($"Currency symbol not found. Searched for {currencySymbol}");
			}

			return isoCurrencySymbol;
		}
	}
}
