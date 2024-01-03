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
			return obj is Currency currency && Symbol == currency?.Symbol;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Symbol ?? string.Empty);
		}

		public static bool operator ==(Currency? obj1, Currency? obj2)
		{
			if (ReferenceEquals(obj1, obj2))
				return true;
			if (ReferenceEquals(obj1, null))
				return false;
			if (ReferenceEquals(obj2, null))
				return false;
			return obj1.Equals(obj2);
		}

		public static bool operator !=(Currency obj1, Currency obj2) => !(obj1 == obj2);

		public override string ToString()
		{
			return $"{Symbol}";
		}
	}
}