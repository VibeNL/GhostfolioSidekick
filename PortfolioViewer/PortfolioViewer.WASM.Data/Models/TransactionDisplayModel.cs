using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
	public class TransactionDisplayModel
	{
		public long Id { get; set; }
		public DateTime Date { get; set; }
		public string Type { get; set; } = string.Empty;
		public string? Symbol { get; set; }
		public string? Name { get; set; }
		public string Description { get; set; } = string.Empty;
		public string TransactionId { get; set; } = string.Empty;
		public string AccountName { get; set; } = string.Empty;
		public decimal? Quantity { get; set; }
		public Money? UnitPrice { get; set; }
		public Money? Amount { get; set; }
		public Money? TotalValue { get; set; }
		public string Currency { get; set; } = string.Empty;
		public Money? Fee { get; set; }
		public Money? Tax { get; set; }

		public override string ToString()
		{
			var parts = new List<string>
			{
				$"{Date:yyyy-MM-dd}",
				Type,
				Symbol ?? "",
				Description,
				TotalValue?.ToString() ?? ""
			};

			if (Fee != null && Fee.Amount > 0)
				parts.Add($"Fee: {Fee}");

			if (Tax != null && Tax.Amount > 0)
				parts.Add($"Tax: {Tax}");

			return string.Join(" - ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
		}
	}
}