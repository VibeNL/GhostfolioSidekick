namespace GhostfolioSidekick.Model.Activities.Types.MoneyLists
{
	public record SellActivityFee
	{
		public SellActivityFee() : base()
		{
			Money = default!;
		}

		public SellActivityFee(Money money)
		{
			Money = money;
		}

		public int? Id { get; set; }

		public Money Money { get; }

		public long ActivityId { get; set; }
	}
}
