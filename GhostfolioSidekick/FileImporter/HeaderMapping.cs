namespace GhostfolioSidekick.FileImporter
{
    public class HeaderMapping
    {
        public string SourceName { get; set; }

        public DestinationHeader DestinationHeader { get; set; }
    }

    public enum DestinationHeader
    {
        Currency,
        Date,
        Quantity,
        Symbol,
        UnitPrice,
        Fee,
        OrderType,
        Isin,
        Reference,
        Undefined1,
        Description,
    }
}