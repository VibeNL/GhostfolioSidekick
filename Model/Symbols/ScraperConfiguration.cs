﻿namespace GhostfolioSidekick.Model.Symbols
{
	public class ScraperConfiguration
	{
		public string? Url { get; internal set; }

		public string? Selector { get; internal set; }

		public string? Locale { get; internal set; }

		public bool IsValid { get { return !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(Selector); } }
	}
}