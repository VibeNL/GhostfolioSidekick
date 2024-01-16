namespace GhostfolioSidekick.Model
{
	public class Balance(Currency currency)
	{
		public Currency Currency { get; set; } = currency;
	}
}