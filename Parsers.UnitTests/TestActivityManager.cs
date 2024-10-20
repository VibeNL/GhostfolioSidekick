using GhostfolioSidekick.Model.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
