namespace GhostfolioSidekick.Model
{
	public class Currency(string symbol)
	{
		public static Currency EUR = new Currency("EUR");
		public static Currency USD = new Currency("USD");

		public string Symbol { get; set; } = symbol;
	}
}