namespace GhostfolioSidekick.Model.Accounts
{
	public class Balance(Currency currency)
	{
		public Currency Currency { get; set; } = currency;
	}
}