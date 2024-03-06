using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Model.Strategies
{
	public interface IHoldingStrategy
	{
		int Priority { get; }

		Task Execute(Holding holding);
	}
}
