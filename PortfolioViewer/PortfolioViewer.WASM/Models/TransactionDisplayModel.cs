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
            return $"{Date:yyyy-MM-dd} - {Type} - {Symbol} - {Description} - {TotalValue}";
        }
    }
}