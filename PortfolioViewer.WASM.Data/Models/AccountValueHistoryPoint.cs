using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
    public class AccountValueHistoryPoint
    {
        public DateOnly Date { get; set; }
        
        public int AccountId { get; set; }
        
        public Money TotalAssetValue { get; set; }
        
        public Money TotalInvested { get; set; }
        
        public Money CashBalance { get; set; }
		public Money TotalValue { get; internal set; }
	}
}