using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;

namespace PortfolioViewer.ApiService
{
	public class PortfolioManager
	{
		internal static Portfolio LoadPorfolio(DatabaseContext databaseContext)
		{
			return new Portfolio();
		}
	}
}
