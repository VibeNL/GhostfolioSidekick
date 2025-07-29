using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
	public static class CurrencyDisplay
	{
		public static string DisplaySignAndAmount(Money money)
		{
			return $"{DisplaySign(money.Currency)} {money.Amount:N2}";
		}

		public static string DisplaySign(Currency currency)
		{
			if (currency == Currency.USD)
				return "$";
			if (currency == Currency.EUR)
				return "€";
			if (currency == Currency.GBP)
				return "£";
			return currency.Symbol;
		}
	}
}
