using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.ExternalDataProvider
{
	public interface ITargetPriceRepository
	{
		Task<PriceTarget?> GetPriceTarget(SymbolProfile symbol);
	}
}
