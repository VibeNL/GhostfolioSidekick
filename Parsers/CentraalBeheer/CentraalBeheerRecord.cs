namespace GhostfolioSidekick.Parsers.CentraalBeheer
{
	public record CentraalBeheerRecord
	{
		public DateTime? Date { get; set; }

		public string? ISIN { get; set; }

		public decimal? NumberOfShares { get; set; }

		public decimal? Price { get; set; }

		public string? Type { get; set; }
		public string? CurrencySymbol { get; set; }
	}
}
