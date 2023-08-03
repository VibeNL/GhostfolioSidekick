namespace GhostfolioSidekick.Ghostfolio.API
{
    public class Asset
    {
        public string Currency { get;set; }

        public string Symbol { get; set; }
        public string DataSource { get; internal set; }
    }
}