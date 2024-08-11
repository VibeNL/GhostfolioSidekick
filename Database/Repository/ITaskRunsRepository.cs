using GhostfolioSidekick.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Database.Repository
{
	public interface ITaskRunsRepository
	{
		public TaskRun GetLastTaskRun(TypeOfTaskRun typeOfTaskRun);
	}
}
