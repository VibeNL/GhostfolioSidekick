namespace GhostfolioSidekick.Model.Activities.Types.MoneyLists
{
	public record BuySellActivityTax
	{
		public BuySellActivityTax() : base()
		{
			Money = default!;
		}

		public BuySellActivityTax(Money money)
		{
			Money = money;
		}

		public int? Id { get; set; }

		public Money Money { get; }
	}
}
