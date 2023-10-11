namespace GhostfolioSidekick.Ghostfolio.API.Contract
{
    public class MarketData
    {
        public DateTime Date { get; set; }

        public string Symbol { get; set; }

        public decimal MarketPrice { get; set; }
    }
}