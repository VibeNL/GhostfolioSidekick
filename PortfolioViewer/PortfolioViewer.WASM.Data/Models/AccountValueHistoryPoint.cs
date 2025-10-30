using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Models
{
	public class AccountValueHistoryPoint
	{
		public DateOnly Date { get; set; }

		public int AccountId { get; set; }

		public required Money TotalAssetValue { get; set; }

		public required Money TotalInvested { get; set; }

		public required Money CashBalance { get; set; }
		public required Money TotalValue { get; set; }
	}
}