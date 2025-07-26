using GhostfolioSidekick.Model.Performance;

namespace GhostfolioSidekick.PerformanceCalculations
{
	public interface IHoldingPerformanceCalculator
	{
		Task<IEnumerable<HoldingAggregated>> GetCalculatedHoldings();
	}
}
