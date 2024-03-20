namespace GhostfolioSidekick.Parsers
{
	public interface IHistoryDataFileImporter
	{
		Task<bool> CanParseHistoricData(string filename);

		Task<HistoricData> ParseHistoricData(string filename);
	}
}
