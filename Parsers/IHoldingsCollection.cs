using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers
{
	public interface IHoldingsCollection
	{
		IReadOnlyList<Holding> Holdings { get; }

		Task AddPartialActivity(string accountName, IEnumerable<PartialActivity> partialActivities);
	}
}