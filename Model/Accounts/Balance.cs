namespace GhostfolioSidekick.Model.Accounts
{
	public class Balance
	{
		public Balance()
		{
			// EF Core
			Date = default!;
			Money = default!;
			TWR = 0;
		}

		public Balance(DateOnly date, Money money, decimal twr = 0)
		{
			Date = date;
			Money = money;
			TWR = twr;
		}

		public int Id { get; set; }

		public DateOnly Date { get; }

		public Money Money { get; set; }

		public decimal TWR { get; set; }

		public override string ToString()
		{
			return Date.ToShortDateString() + " " + Money.ToString() + " TWR: " + TWR.ToString("P2");
		}
	}
}
