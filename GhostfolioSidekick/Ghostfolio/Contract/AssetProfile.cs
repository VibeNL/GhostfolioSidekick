namespace GhostfolioSidekick.Ghostfolio.Contract
{
    public class AssetProfile
    {
        public int ActivitiesCount { get; set; }

        public string Symbol { get; set; }

        public string DataSource { get; set; }

        public IDictionary<string, string> SymbolMapping { get; set; }
    }
}