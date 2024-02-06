using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Compare;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public sealed class MergeOrder
	{
		public MergeOrder(Operation operation, Activity order1)
		{
			Operation = operation;
			Order1 = order1;
			Order2 = null;
		}

		public MergeOrder(Operation operation, Activity order1, Activity order2) : this(operation, order1)
		{
			Order2 = order2;
		}

		public Operation Operation { get; }

		public Activity Order1 { get; }

		public Activity? Order2 { get; }
	}
}
