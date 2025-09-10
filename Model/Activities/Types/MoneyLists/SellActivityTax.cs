namespace GhostfolioSidekick.Model.Activities.Types.MoneyLists
{
	public record SellActivityTax
	{
		public SellActivityTax() : base()
		{
			Money = default!;
		}

		public SellActivityTax(Money money)
		{
			Money = money;
		}

		public int? Id { get; set; }

		public Money Money { get; }

		public long ActivityId { get; set; }
	}
}
