using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Models
{
	public class AccountValueDisplayModel
	{
		public DateOnly Date { get; set; }

		public string AccountName { get; set; } = string.Empty;

		public int AccountId { get; set; }

		public required Money Value { get; set; }

		public required Money Invested { get; set; }

		public required Money Balance { get; set; }

		public required Money GainLoss { get; set; }

		public decimal GainLossPercentage { get; set; }

		public string Currency { get; set; } = "USD";
	}
}