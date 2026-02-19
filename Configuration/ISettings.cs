namespace GhostfolioSidekick.Configuration
{
	public interface ISettings
	{
		bool DeleteUnusedSymbols { get; set; }
		string DataProviderPreference { get; set; }
		string PrimaryCurrency { get; set; }
	}
}
