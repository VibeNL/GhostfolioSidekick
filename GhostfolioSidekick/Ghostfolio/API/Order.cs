namespace GhostfolioSidekick.Ghostfolio.API
{
	public class Order
	{
		public string AccountId { get; set; }

		public Asset Asset { get; set; }

		public string Comment { get; set; }

		public string Currency { get; set; }

		public DateTime Date { get; set; }

		public decimal Fee { get; set; }

		public string FeeCurrency { get; set; }

		public decimal Quantity { get; set; }

		public OrderType Type { get; set; }

		public decimal UnitPrice { get; set; }
	}
}