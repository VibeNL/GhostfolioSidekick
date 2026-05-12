using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Models
{
    public class TransactionRow
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public Money Amount { get; set; } = default!;
        public string Type { get; set; } = string.Empty;
    }
}
