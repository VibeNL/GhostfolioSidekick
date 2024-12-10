using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers.UnitTests
{
	internal class TestActivityManager : IActivityManager
	{
		public List<PartialActivity> PartialActivities { get; set; } = new List<PartialActivity>();

		public void AddPartialActivity(string accountName, IEnumerable<PartialActivity> partialActivities)
		{
			PartialActivities.AddRange(partialActivities);
		}

		public Task<IEnumerable<Activity>> GenerateActivities()
		{
			throw new NotImplementedException();
		}
	}
}
