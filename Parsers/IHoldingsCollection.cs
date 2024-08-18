using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers
{
	public interface IHoldingsCollection
	{
		void AddPartialActivity(string accountName, IEnumerable<PartialActivity> partialActivity);

		Task<IEnumerable<Holding>> GenerateActivities();
	}
}