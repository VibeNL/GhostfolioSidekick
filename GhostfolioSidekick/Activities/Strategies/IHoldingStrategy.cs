using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.ProcessingService.Activities.Strategies
{
	public interface IHoldingStrategy
	{
		int Priority { get; }

		Task Execute(Holding holding);
	}
}
