namespace GhostfolioSidekick.Model
{
	public class Currency(string symbol)
	{
		public static Currency EUR = new Currency("EUR");

		public string Symbol { get; set; } = symbol;
	}
}