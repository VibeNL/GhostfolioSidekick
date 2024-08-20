using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers
{
	public interface IActivityManager
	{
		void AddPartialActivity(string accountName, IEnumerable<PartialActivity> partialActivities);

		IEnumerable<Activity> GenerateActivities();
	}
}