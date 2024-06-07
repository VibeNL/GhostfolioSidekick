namespace GhostfolioSidekick.Parsers
{
	public interface IFileImporter
	{
		Task<bool> CanParse(string filename);
	}
}
