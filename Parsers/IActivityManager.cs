using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers
{
	public interface IActivityManager
	{
		void AddPartialActivity(string accountName, IEnumerable<PartialActivity> partialActivities);

		Task<IEnumerable<Activity>> GenerateActivities();
	}
}