namespace GhostfolioSidekick.Model
{
	public class Currency
	{
		public static Currency EUR = new Currency("EUR");
		public static Currency USD = new Currency("USD");
		public static Currency GBP = new Currency("GBP");
		public static Currency GBp = new Currency("GBp");

		public Currency(string symbol)
		{
			if (symbol == "GBX")
			{
				symbol = GBp.Symbol;
			}

			Symbol = symbol;
		}

		public string Symbol { get; set; }
	}
}