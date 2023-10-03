using GhostfolioSidekick.Ghostfolio.API;

namespace GhostfolioSidekick.FileImporter
{
	public interface IFileImporter
	{
		Task<bool> CanConvertOrders(IEnumerable<string> filenames);
		Task<IEnumerable<Activity>> ConvertToOrders(string accountName, IEnumerable<string> filenames);
	}
}
