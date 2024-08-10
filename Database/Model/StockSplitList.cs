using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Database.Model
{
	public class StockSplitList
	{
		public int Id { get; set; }

		public required int SymbolProfileId { get; set; }

		public required SymbolProfile SymbolProfile { get; set; }

		public required List<StockSplit> StockSplits { get; set; }

		public DateTime LastUpdate { get; set; }
	}
}
