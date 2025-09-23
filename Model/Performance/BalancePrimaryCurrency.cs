namespace GhostfolioSidekick.Model.Performance
{
	public class BalancePrimaryCurrency
	{
		public BalancePrimaryCurrency()
		{
			// EF Core
			Date = default!;
			Money = default!;
		}

		public int Id { get; set; }

		public DateOnly Date { get; set; }

		public decimal Money { get; set; }

		public int AccountId { get; set; }

		public override string ToString()
		{
			return Date.ToShortDateString() + " " + Money.ToString();
		}
	}
}