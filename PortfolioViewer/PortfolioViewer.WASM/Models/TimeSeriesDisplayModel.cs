using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
    public class TimeSeriesDisplayModel
    {
        public DateOnly Date { get; set; }
        public Money TotalValue { get; set; } = new Money(Currency.EUR, 0);
        public Money TotalInvested { get; set; } = new Money(Currency.EUR, 0);
        public Money GainLoss { get; set; } = new Money(Currency.EUR, 0);
        public decimal GainLossPercentage { get; set; }
    }
}