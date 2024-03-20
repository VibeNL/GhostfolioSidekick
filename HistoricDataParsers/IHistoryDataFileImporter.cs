namespace GhostfolioSidekick.Parsers
{
	public interface IHistoryDataFileImporter
	{
		Task<bool> CanParseHistoricData(string filename);

		Task<IEnumerable<HistoricData>> ParseHistoricData(string filename);
	}
}
