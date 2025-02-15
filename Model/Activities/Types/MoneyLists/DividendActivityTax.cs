namespace GhostfolioSidekick.Model.Activities.Types.MoneyLists
{
	public record DividendActivityTax
	{
		public DividendActivityTax() : base()
		{
			Money = default!;
		}

		public DividendActivityTax(Money money)
		{
			Money = money;
		}

		public int? Id { get; set; }

		public Money Money { get; }
	}
}
