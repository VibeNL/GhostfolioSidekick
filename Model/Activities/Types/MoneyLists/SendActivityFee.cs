namespace GhostfolioSidekick.Model.Activities.Types.MoneyLists
{
	public record SendActivityFee
	{
		public SendActivityFee() : base()
		{
			Money = default!;
		}

		public SendActivityFee(Money money)
		{
			Money = money;
		}

		public int? Id { get; set; }

		public Money Money { get; }

		public long ActivityId { get; set; }
	}
}
