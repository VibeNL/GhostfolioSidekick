namespace GhostfolioSidekick.Blazor.Models
{
    public class SymbolProfile
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string DataSource { get; set; }
        public string Currency { get; set; }
        public string AssetClass { get; set; }
        public string AssetSubClass { get; set; }
        public decimal MarketValue { get; set; }
        public decimal Quantity { get; set; }
    }
}
