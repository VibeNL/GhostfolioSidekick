namespace GhostfolioSidekick.Model.Accounts
{
	public class Balance(Money money)
	{
		public Money Money { get; set; } = money;
	}
}