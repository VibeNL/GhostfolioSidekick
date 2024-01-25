using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Model
{
	public interface IHoldingStrategy
	{
		Task Execute(Holding holding);
	}
}
