namespace GhostfolioSidekick.Model.Accounts
{
	public class Balance(Money money)
	{
		public Money Money { get; set; } = money;

		public override string ToString()
		{
			return Money.ToString();
		}
	}
}