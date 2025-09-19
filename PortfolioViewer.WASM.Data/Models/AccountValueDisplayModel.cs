using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Models
{
    public class AccountValueDisplayModel
    {
        public DateOnly Date { get; set; }
        
        public string AccountName { get; set; } = string.Empty;
        
        public int AccountId { get; set; }
        
        public Money Value { get; set; }
        
        public Money Invested { get; set; }
        
        public Money Balance { get; set; }
        
        public Money GainLoss { get; set; }
        
        public decimal GainLossPercentage { get; set; }
        
        public string Currency { get; set; } = "USD";
    }
}