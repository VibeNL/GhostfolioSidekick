namespace GhostfolioSidekick.Parsers
{
	public interface IHistoryDataFileImporter : IFileImporter
	{
		Task<IEnumerable<HistoricData>> ParseHistoricData(string filename);
	}
}
