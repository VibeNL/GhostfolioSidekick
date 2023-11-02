using GhostfolioSidekick.Model;

namespace GhostfolioSidekick
{
	internal static class CurrencyHelper
	{
		public static Currency? ParseCurrency(string currency)
		{
			if (string.IsNullOrWhiteSpace(currency))
			{
				return null;
			}

			return new Currency(currency);
		}
	}
}