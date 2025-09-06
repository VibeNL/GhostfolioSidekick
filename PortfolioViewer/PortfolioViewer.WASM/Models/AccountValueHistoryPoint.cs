using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
    public class AccountValueHistoryPoint
    {
        public DateOnly Date { get; set; }
        
        public Account Account { get; set; } = new();
        
        public Money Value { get; set; }
        
        public Money Invested { get; set; }
        
        public Money Balance { get; set; }
    }
}