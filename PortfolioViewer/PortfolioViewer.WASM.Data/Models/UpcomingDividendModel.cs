namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Models
{
    public class UpcomingDividendModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public DateTime ExDate { get; set; }
        public DateTime PaymentDate { get; set; }
        
        // Native currency (original dividend currency)
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal DividendPerShare { get; set; }
        
        // Primary currency equivalent
        public decimal AmountPrimaryCurrency { get; set; }
        public string PrimaryCurrency { get; set; } = string.Empty;
        public decimal DividendPerSharePrimaryCurrency { get; set; }
        
        public decimal Quantity { get; set; }
    }
}