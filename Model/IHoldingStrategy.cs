using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Model
{
	public interface IHoldingStrategy
	{
		int Priority { get; }

		Task Execute(Holding holding);
	}
}
