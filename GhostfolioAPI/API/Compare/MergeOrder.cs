using GhostfolioSidekick.GhostfolioAPI.Contract;

namespace GhostfolioSidekick.GhostfolioAPI.API.Compare
{
	public sealed class MergeOrder(Operation operation, SymbolProfile symbolProfile, Activity order1)
	{
		public MergeOrder(Operation operation, SymbolProfile symbolProfile, Activity order1, Activity order2) : this(operation, symbolProfile, order1)
		{
			Order2 = order2;
		}

		public Operation Operation { get; } = operation;

		public SymbolProfile SymbolProfile { get; } = symbolProfile;

		public Activity Order1 { get; } = order1;

		public Activity? Order2 { get; } = null;
	}
}
