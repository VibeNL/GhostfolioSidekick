using GhostfolioSidekick.GhostfolioAPI.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IGhostfolioMarketData
	{
		Task DeleteSymbol(SymbolProfile symbolProfile);

		Task<IEnumerable<SymbolProfile>> GetAllSymbolProfiles();

		Task<GenericInfo> GetBenchmarks();
	}
}
