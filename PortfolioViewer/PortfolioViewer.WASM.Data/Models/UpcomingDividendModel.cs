namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Models
{
    public class UpcomingDividendModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public DateTime ExDate { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal DividendPerShare { get; set; }
    }
}