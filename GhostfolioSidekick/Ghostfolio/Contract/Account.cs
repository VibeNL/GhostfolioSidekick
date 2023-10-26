namespace GhostfolioSidekick.Ghostfolio.Contract
{
    public class Account
    {
        public string Name { get; set; }

        public string Id { get; set; }

        public decimal Balance { get; set; }

        public string Currency { get; set; }

        public string? Comment { get; set; }

        public bool IsExcluded { get; set; }

        public string PlatformId { get; set; }
    }
}