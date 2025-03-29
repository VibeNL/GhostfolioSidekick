namespace GhostfolioSidekick.Blazor.Models
{
    public class Activity
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public int AccountId { get; set; }
        public Account Account { get; set; }
        public int SymbolProfileId { get; set; }
        public SymbolProfile SymbolProfile { get; set; }
    }
}
