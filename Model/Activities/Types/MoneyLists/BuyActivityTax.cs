namespace GhostfolioSidekick.Model.Activities.Types.MoneyLists
{
	public record BuyActivityTax
	{
		public BuyActivityTax() : base()
		{
			Money = default!;
		}

		public BuyActivityTax(Money money)
		{
			Money = money;
		}

		public int? Id { get; set; }

		public Money Money { get; }

		public long ActivityId { get; set; }
	}
}
