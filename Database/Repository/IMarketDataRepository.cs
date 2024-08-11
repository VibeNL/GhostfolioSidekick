using GhostfolioSidekick.Model.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Database.Repository
{
	public interface IMarketDataRepository
	{
		public IEnumerable<SymbolProfile> GetSymbols();
	}
}
