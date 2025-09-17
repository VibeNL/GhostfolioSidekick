using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
    public class AccountValueHistoryPoint
    {
        public DateOnly Date { get; set; }
        
        public int AccountId { get; set; }
        
        public Money TotalValue { get; set; }
        
        public Money TotalInvested { get; set; }
        
        public Money Balance { get; set; }
    }
}