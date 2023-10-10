namespace GhostfolioSidekick.Ghostfolio.API.Contract
{
    public class Activity
    {
        public string AccountId { get; set; }

        public Asset Asset { get; set; }

        public string Comment { get; set; }

        public string Currency { get; set; }

        public DateTime Date { get; set; }

        public decimal Fee { get; set; }

        public string FeeCurrency { get; set; }

        public decimal Quantity { get; set; }

        public ActivityType Type { get; set; }

        public decimal UnitPrice { get; set; }


        // Internal use
        public string ReferenceCode { get; set; }
    }
}