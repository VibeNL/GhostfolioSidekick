using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
    public class DividendDisplayModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public Money Amount { get; set; } = Money.Zero(Currency.USD);
        public Money TaxAmount { get; set; } = Money.Zero(Currency.USD);
        public Money NetAmount { get; set; } = Money.Zero(Currency.USD);
        public string AssetClass { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
    }

    public class DividendAggregateDisplayModel
    {
        public string Period { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public Money TotalAmount { get; set; } = Money.Zero(Currency.USD);
        public Money TotalTaxAmount { get; set; } = Money.Zero(Currency.USD);
        public Money TotalNetAmount { get; set; } = Money.Zero(Currency.USD);
        public int DividendCount { get; set; }
        public List<DividendDisplayModel> Dividends { get; set; } = new();
    }
}