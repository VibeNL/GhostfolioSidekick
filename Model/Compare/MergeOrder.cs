using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Compare
{
	public sealed class MergeOrder
	{
		public MergeOrder(Operation operation, SymbolProfile symbolProfile, IActivity order1)
		{
			Operation = operation;
			SymbolProfile = symbolProfile;
			Order1 = order1;
			Order2 = null;
		}

		public MergeOrder(Operation operation, SymbolProfile symbolProfile, IActivity order1, IActivity order2) : this(operation, symbolProfile, order1)
		{
			Order2 = order2;
		}

		public Operation Operation { get; }

		public SymbolProfile SymbolProfile { get; }

		public IActivity Order1 { get; }

		public IActivity? Order2 { get; }
	}
}
