namespace GhostfolioSidekick.Model.Symbols
{
	public class ScraperConfiguration
	{
		public string? Url { get; set; }

		public string? Selector { get; set; }

		public string? Locale { get; set; }

		public bool IsValid { get { return !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(Selector); } }
	}
}