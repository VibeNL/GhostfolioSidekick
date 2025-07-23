using GhostfolioSidekick.Model;
using GhostfolioSidekick.PerformanceCalculations.Models;

namespace GhostfolioSidekick.PerformanceCalculations
{
	public interface IHoldingPerformanceCalculator
	{
		Task<IEnumerable<HoldingAggregated>> GetCalculatedHoldings(Currency targetCurrency);
	}
}
