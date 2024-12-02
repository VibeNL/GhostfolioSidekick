using GhostfolioSidekick.GhostfolioAPI.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.GhostfolioAPI.API.Compare
{
	public sealed class MergeOrder
	{
		public MergeOrder(Operation operation, SymbolProfile symbolProfile, Activity order1)
		{
			Operation = operation;
			SymbolProfile = symbolProfile;
			Order1 = order1;
			Order2 = null;
		}

		public MergeOrder(Operation operation, SymbolProfile symbolProfile, Activity order1, Activity order2) : this(operation, symbolProfile, order1)
		{
			Order2 = order2;
		}

		public Operation Operation { get; }

		public SymbolProfile SymbolProfile { get; }

		public Activity Order1 { get; }

		public Activity? Order2 { get; }
	}
}
