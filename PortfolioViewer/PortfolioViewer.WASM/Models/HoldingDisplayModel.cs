namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
    public class HoldingDisplayModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal GainLoss { get; set; }
        public decimal GainLossPercentage { get; set; }
        public decimal Weight { get; set; }
        public string Sector { get; set; } = string.Empty;
        public string AssetClass { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
    }
}