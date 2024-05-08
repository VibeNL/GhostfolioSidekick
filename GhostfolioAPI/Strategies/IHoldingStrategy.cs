using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI.Strategies
{
	public interface IHoldingStrategy
	{
		int Priority { get; }

		Task Execute(Holding holding);
	}
}
