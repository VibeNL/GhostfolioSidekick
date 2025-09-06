namespace GhostfolioSidekick.Model.Activities.Types.MoneyLists
{
	public record BuyActivityFee
	{
		public BuyActivityFee() : base()
		{
			Money = default!;
		}

		public BuyActivityFee(Money money)
		{
			Money = money;
		}

		public int? Id { get; set; }

		public Money Money { get; }

		public long ActivityId { get; set; }
	}
}
