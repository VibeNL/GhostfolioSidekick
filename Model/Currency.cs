namespace GhostfolioSidekick.Model
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S2223:Non-constant static fields should not be visible", Justification = "<Pending>")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "<Pending>")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "<Pending>")]
	public class Currency
	{
		public static Currency EUR = new("EUR");
		public static Currency USD = new("USD");
		public static Currency GBP = new("GBP");
		public static Currency GBp = new("GBp");

		private static readonly List<Currency> knownCurrencies = [USD, EUR, GBP, GBp];

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

		public override bool Equals(object? obj)
		{
			return obj is Currency currency &&
				   Symbol == currency.Symbol;
		}

		override public int GetHashCode()
		{
			return HashCode.Combine(Symbol);
		}

		public override string ToString()
		{
			return Symbol;
		}

		public static bool operator ==(Currency a, Currency? b)
		{
			if (ReferenceEquals(a, b))
			{
				return true;
			}

			if ((a is null) || (b is null))
			{
				return false;
			}

			return a.Symbol == b.Symbol;
		}


		public static bool operator !=(Currency a, Currency? b)
		{
			return !(a == b);
		}

	}
}