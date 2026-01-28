using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;

namespace GhostfolioSidekick.PerformanceCalculations
{
	public interface IPerformanceCalculator
	{
		Task<IEnumerable<CalculatedSnapshot>> GetCalculatedSnapshots(Holding holding);
	}
}
