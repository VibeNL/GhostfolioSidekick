using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Model.Market
{
	public class StockSplit
	{
		public DateTime Date { get; set; }
		public int FromFactor { get; set; }
		public int ToFactor { get; set; }
	}
}
