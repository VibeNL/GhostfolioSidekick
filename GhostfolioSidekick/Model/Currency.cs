namespace GhostfolioSidekick.Model
{
	public class Currency
	{
		public Currency(string symbol)
		{
			Symbol = symbol;
		}

		public string Symbol { get; set; }

		public override bool Equals(object? obj)
		{
			return obj is Currency currency &&
				   Symbol == currency.Symbol;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Symbol);
		}
	}
}