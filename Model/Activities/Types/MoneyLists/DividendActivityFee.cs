namespace GhostfolioSidekick.Model.Activities.Types.MoneyLists
{
	public record DividendActivityFee
	{
		public DividendActivityFee() : base()
		{
			Money = default!;
		}

		public DividendActivityFee(Money money)
		{
			Money = money;
		}

		public int? Id { get; set; }

		public Money Money { get; }

		public long ActivityId { get; set; }
	}
}
