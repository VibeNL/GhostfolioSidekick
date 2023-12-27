using GhostfolioSidekick.Model;

namespace GhostfolioSidekick
{
	internal static class CurrencyHelper
	{
		internal static Currency USD = new("USD");
		internal static Currency EUR = new("EUR");
		internal static Currency GBP = new("GBP");
		internal static Currency GBp = new("GBp");

		private static Currency[] knownCurrencies = [USD, EUR, GBP, GBp];

		private static CurrencyPair[] currencies =
		[
			new CurrencyPair(GBP, GBp, 100)
		];

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

		public static bool IsFiat(string currency)
		{
			return knownCurrencies.Any(x => x.Symbol == currency);
		}
	}
}