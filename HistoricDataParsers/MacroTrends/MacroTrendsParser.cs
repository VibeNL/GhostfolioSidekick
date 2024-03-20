using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Parsers.MacroTrends
{
	public class MacroTrendsParser : IHistoryDataFileImporter
	{
		public Task<bool> CanParseHistoricData(string filename)
		{
			throw new NotImplementedException();
		}

		public Task<HistoricData> ParseHistoricData(string filename)
		{
			throw new NotImplementedException();
		}
	}
}
