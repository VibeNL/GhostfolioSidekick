namespace GhostfolioSidekick.Model.Accounts
{
	public class Balance
	{
		public Balance(Money money)
		{
			Money = money;
		}

		public Money Money { get; set; }

		public override string ToString()
		{
			return Money.ToString();
		}
	}
}