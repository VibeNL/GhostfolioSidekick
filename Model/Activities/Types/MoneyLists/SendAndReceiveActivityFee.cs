namespace GhostfolioSidekick.Model.Activities.Types.MoneyLists
{
	public record SendAndReceiveActivityFee
	{
		public SendAndReceiveActivityFee() : base()
		{
			Money = default!;
		}

		public SendAndReceiveActivityFee(Money money)
		{
			Money = money;
		}

		public int? Id { get; set; }

		public Money Money { get; }
	}
}
