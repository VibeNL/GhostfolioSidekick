namespace GhostfolioSidekick.Model.Activities.Types.MoneyLists
{
	public record ReceiveActivityFee
	{
		public ReceiveActivityFee() : base()
		{
			Money = default!;
		}

		public ReceiveActivityFee(Money money)
		{
			Money = money;
		}

		public int? Id { get; set; }

		public Money Money { get; }

		public long ActivityId { get; set; }
	}
}
