using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Activities.Strategies
{
	public interface IHoldingStrategy
	{
		int Priority { get; }

		Task Execute(Holding holding);
	}
}
