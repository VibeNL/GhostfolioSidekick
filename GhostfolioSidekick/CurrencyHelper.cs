using GhostfolioSidekick.Model;

namespace GhostfolioSidekick
{
	internal static class CurrencyHelper
	{
		internal static Currency USD = new Currency("USD");
		internal static Currency EUR = new Currency("EUR");
		internal static Currency GBP = new Currency("GBP");
		internal static Currency GBp = new Currency("GBp");

		private static CurrencyPair[] currencies = new[]
		{
			new CurrencyPair(GBP, GBp, 100)
		};

		public static Currency? ParseCurrency(string currency)
		{
			if (string.IsNullOrWhiteSpace(currency))
			{
				return null;
			}

			return new Currency(currency);
		}

		public static CurrencyPair GetKnownPairOfCurrencies(Currency currency)
		{
			return currencies.FirstOrDefault(x => x.A == currency || x.B == currency) ?? new CurrencyPair(currency, currency, 1);
		}
	}
}