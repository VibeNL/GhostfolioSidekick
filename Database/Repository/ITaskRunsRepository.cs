using GhostfolioSidekick.Model.Tasks;

namespace GhostfolioSidekick.Database.Repository
{
	public interface ITaskRunsRepository
	{
		public TaskRun GetLastTaskRun(TypeOfTaskRun typeOfTaskRun);
	}
}
