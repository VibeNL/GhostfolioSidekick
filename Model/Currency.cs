using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model
{
	[SuppressMessage("Critical Code Smell", "S2223:Non-constant static fields should not be visible", Justification = "<Pending>")]
	[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "<Pending>")]
	[SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "<Pending>")]
	public record Currency
	{
		public static Currency EUR = new("EUR");
		public static Currency USD = new("USD");
		public static Currency GBP = new("GBP");
		public static Currency GBp = new("GBp");

		private static readonly List<Currency> knownCurrencies = [USD, EUR, GBP, GBp];
				public Currency() // EF Core
		{
			Symbol = default!;
		}

		public Currency(string symbol)
		{
			if (symbol == "GBX")
			{
				symbol = GBp.Symbol;
			}

			Symbol = symbol;
		}

		public string Symbol { get; set; }

		public bool IsFiat()
		{
			return knownCurrencies.Exists(x => x.Symbol == Symbol);
		}

		public override string ToString()
		{
			return Symbol;
		}

		public bool IsKnownPair(Currency key)
		{
			return Symbol.Equals(key.Symbol, StringComparison.CurrentCultureIgnoreCase); // TODO GBP == GBp
		}
	}
}