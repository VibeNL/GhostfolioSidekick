namespace GhostfolioSidekick.Model.Accounts
{
	public class Balance
	{
		public Balance()
		{
			// EF Core
			Date = default!;
			Money = default!;
		}

		public Balance(DateOnly date, Money money)
		{
			Date = date;
			Money = money;
		}

		public int Id { get; set; }

		public DateOnly Date { get; }

		public Money Money { get; set; }

		public int AccountId { get; set; }

		public override string ToString()
		{
			return Date.ToShortDateString() + " " + Money.ToString();
		}
	}
}