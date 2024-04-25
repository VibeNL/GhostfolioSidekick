namespace GhostfolioSidekick.GhostfolioAPI.Contract
{
	public class Balance
	{
		public required Account Account { get; set; }

		public DateOnly Date { get; set; }

		public Guid Id { get; set; }

		public decimal Value { get; set; }

		public decimal ValueInBaseCurrency { get; set; }
	}
}