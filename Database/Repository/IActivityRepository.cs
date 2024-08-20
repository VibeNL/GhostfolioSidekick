using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Database.Repository
{
	public interface IActivityRepository
	{
		Task StoreAll(IActivityRepository activityRepository);
	}
}
