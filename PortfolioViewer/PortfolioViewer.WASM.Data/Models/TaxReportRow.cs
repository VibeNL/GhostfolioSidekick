using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Models
{
    public class TaxReportRow
    {
        public int Year { get; set; }

        public DateOnly Date { get; set; }

        public int AccountId { get; set; }

        public string AccountName { get; set; } = string.Empty;

        public required Money AssetValue { get; set; }

        public required Money CashBalance { get; set; }

        public required Money TotalValue { get; set; }
    }
}
