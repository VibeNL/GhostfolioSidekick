using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
    public class HoldingPriceHistoryPoint
    {
        public DateOnly Date { get; set; }
        public Money Price { get; set; } = new Money(Currency.USD, 0);
    }
}