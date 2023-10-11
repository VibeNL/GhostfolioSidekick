using GhostfolioSidekick.Ghostfolio.API.Contract;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public partial class GhostfolioAPI
	{
		private sealed class MergeOrder
		{
			public MergeOrder(Operation operation, Contract.Activity order1)
			{
				Operation = operation;
				Order1 = order1;
				Order2 = null;
			}

			public MergeOrder(Operation operation, Contract.Activity order1, RawActivity? order2) : this(operation, order1)
			{
				Order2 = order2;
			}

			public Operation Operation { get; }

			public Contract.Activity Order1 { get; }

			public RawActivity? Order2 { get; }
		}
	}
}
